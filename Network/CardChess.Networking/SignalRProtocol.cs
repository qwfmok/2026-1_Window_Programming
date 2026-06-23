using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CardChess.Networking
{
    public sealed class SignalRProtocol : IDisposable
    {
        private readonly HubConnection connection;
        private readonly SemaphoreSlim connectionLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private volatile bool manualClose;
        private volatile bool disposed;
        private string roomCode;
        private string playerToken;

        public bool IsConnected { get; private set; }
        public bool IsServerConnected => connection.State == HubConnectionState.Connected;
        public string RoomCode => roomCode;
        public int PlayerNumber { get; private set; }
        public event Action<string> OnMessage;

        public SignalRProtocol(string serverUrl)
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
                throw new ArgumentException("SignalR server URL is required.", nameof(serverUrl));

            string hubUrl = NormalizeHubUrl(serverUrl);
            connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new RenderRetryPolicy())
                .Build();

            connection.ServerTimeout = TimeSpan.FromSeconds(45);
            connection.KeepAliveInterval = TimeSpan.FromSeconds(15);

            connection.On<string>("GameMessage", message => Raise(message));
            connection.On("PeerConnected", () =>
            {
                IsConnected = true;
                Raise("CONNECTED");
            });
            connection.On("PeerReconnecting", () =>
            {
                IsConnected = false;
                Raise("PEER_RECONNECTING");
            });
            connection.On("PeerReconnected", () =>
            {
                IsConnected = true;
                Raise("PEER_RECONNECTED");
            });
            connection.On("OpponentDisconnected", () =>
            {
                IsConnected = false;
                Raise("OPPONENT_DISCONNECTED");
            });

            connection.Reconnecting += error =>
            {
                IsConnected = false;
                Raise("SERVER_RECONNECTING");
                return Task.CompletedTask;
            };
            connection.Reconnected += async connectionId =>
            {
                Raise("SERVER_RECONNECTED");
                await RejoinCurrentRoomAsync().ConfigureAwait(false);
            };
            connection.Closed += async error =>
            {
                IsConnected = false;
                if (!manualClose && !disposed)
                {
                    Raise("SERVER_DISCONNECTED");
                    await RecoverClosedConnectionAsync().ConfigureAwait(false);
                }
            };
        }

        public async Task<string> CreateRoomAsync()
        {
            if (!await EnsureServerConnectionAsync().ConfigureAwait(false))
                return null;

            try
            {
                RoomConnectionResult result = await connection
                    .InvokeAsync<RoomConnectionResult>("CreateRoom")
                    .ConfigureAwait(false);
                if (!ApplyRoomResult(result))
                    return null;

                Raise("ROOM_CREATED," + roomCode);
                return roomCode;
            }
            catch (Exception ex)
            {
                Raise("CONNECTION_REJECTED," + GetErrorMessage(ex));
                return null;
            }
        }

        public async Task<bool> JoinRoomAsync(string code)
        {
            if (!await EnsureServerConnectionAsync().ConfigureAwait(false))
                return false;

            try
            {
                RoomConnectionResult result = await connection
                    .InvokeAsync<RoomConnectionResult>("JoinRoom", code)
                    .ConfigureAwait(false);
                if (!ApplyRoomResult(result))
                    return false;

                Raise("ROOM_JOINED," + roomCode);
                return true;
            }
            catch (Exception ex)
            {
                Raise("CONNECTION_REJECTED," + GetErrorMessage(ex));
                return false;
            }
        }

        public bool Send(string message)
        {
            return SendAsync(message).GetAwaiter().GetResult();
        }

        public async Task<bool> SendAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(playerToken))
                return false;

            await sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int attempt = 0; attempt < 40 && !manualClose && !disposed; attempt++)
                {
                    if (connection.State == HubConnectionState.Connected && IsConnected)
                    {
                        try
                        {
                            await connection.InvokeAsync(
                                "SendGameMessage",
                                roomCode,
                                playerToken,
                                message).ConfigureAwait(false);
                            return true;
                        }
                        catch (Exception ex) when (IsTransient(ex))
                        {
                        }
                    }

                    await Task.Delay(500).ConfigureAwait(false);
                }

                Raise("SEND_FAILED,게임 메시지를 상대에게 전달하지 못했습니다.");
                return false;
            }
            finally
            {
                sendLock.Release();
            }
        }

        public void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
        }

        public async Task CloseAsync()
        {
            if (disposed)
                return;

            manualClose = true;
            IsConnected = false;
            try
            {
                if (connection.State == HubConnectionState.Connected &&
                    !string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(playerToken))
                {
                    await connection.InvokeAsync("LeaveRoom", roomCode, playerToken).ConfigureAwait(false);
                }
            }
            catch
            {
            }

            try { await connection.StopAsync().ConfigureAwait(false); } catch { }
            try { await connection.DisposeAsync().ConfigureAwait(false); } catch { }
            disposed = true;
            roomCode = null;
            playerToken = null;
        }

        public void Dispose()
        {
            Close();
            connectionLock.Dispose();
            sendLock.Dispose();
        }

        private async Task<bool> EnsureServerConnectionAsync()
        {
            if (disposed || manualClose)
                return false;
            if (connection.State == HubConnectionState.Connected)
                return true;

            await connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (connection.State == HubConnectionState.Connected)
                    return true;

                int[] retrySeconds = { 0, 2, 5, 10, 15, 30, 30, 30 };
                for (int attempt = 0; attempt < retrySeconds.Length && !manualClose && !disposed; attempt++)
                {
                    if (retrySeconds[attempt] > 0)
                        await Task.Delay(TimeSpan.FromSeconds(retrySeconds[attempt])).ConfigureAwait(false);

                    Raise(attempt == 0
                        ? "SERVER_CONNECTING"
                        : "SERVER_RETRYING," + (attempt + 1));

                    try
                    {
                        using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75)))
                        {
                            await connection.StartAsync(timeout.Token).ConfigureAwait(false);
                        }
                        Raise("SERVER_CONNECTED");
                        return true;
                    }
                    catch
                    {
                        if (connection.State != HubConnectionState.Disconnected)
                            await WaitUntilDisconnectedAsync().ConfigureAwait(false);
                    }
                }

                Raise("CONNECTION_REJECTED,서버에 연결하지 못했습니다. 잠시 후 다시 시도하세요.");
                return false;
            }
            finally
            {
                connectionLock.Release();
            }
        }

        private async Task RecoverClosedConnectionAsync()
        {
            if (!await EnsureServerConnectionAsync().ConfigureAwait(false))
                return;
            await RejoinCurrentRoomAsync().ConfigureAwait(false);
        }

        private async Task RejoinCurrentRoomAsync()
        {
            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(playerToken) ||
                connection.State != HubConnectionState.Connected)
                return;

            try
            {
                RoomConnectionResult result = await connection
                    .InvokeAsync<RoomConnectionResult>("RejoinRoom", roomCode, playerToken)
                    .ConfigureAwait(false);
                if (!ApplyRoomResult(result))
                {
                    ClearRoom();
                    Raise("ROOM_LOST");
                    return;
                }

                Raise("REJOINED");
            }
            catch
            {
                ClearRoom();
                Raise("ROOM_LOST");
            }
        }

        private bool ApplyRoomResult(RoomConnectionResult result)
        {
            if (result == null || !result.Success)
            {
                Raise("CONNECTION_REJECTED," + (result?.Error ?? "방 연결에 실패했습니다."));
                return false;
            }

            roomCode = result.RoomCode;
            playerToken = result.PlayerToken;
            PlayerNumber = result.PlayerNumber;
            IsConnected = result.PeerConnected;
            return true;
        }

        private void ClearRoom()
        {
            roomCode = null;
            playerToken = null;
            PlayerNumber = 0;
            IsConnected = false;
        }

        private async Task WaitUntilDisconnectedAsync()
        {
            for (int i = 0; i < 50 && connection.State != HubConnectionState.Disconnected; i++)
                await Task.Delay(100).ConfigureAwait(false);
        }

        private void Raise(string message)
        {
            try { OnMessage?.Invoke(message); } catch { }
        }

        private static bool IsTransient(Exception exception)
        {
            return exception is InvalidOperationException ||
                   exception is TimeoutException ||
                   exception.GetType().Name.Contains("HttpRequest") ||
                   exception.GetType().Name.Contains("HubException");
        }

        private static string GetErrorMessage(Exception exception)
        {
            return string.IsNullOrWhiteSpace(exception.Message)
                ? "서버 요청에 실패했습니다."
                : exception.Message.Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string NormalizeHubUrl(string url)
        {
            string normalized = url.Trim().TrimEnd('/');
            if (!normalized.EndsWith("/gamehub", StringComparison.OrdinalIgnoreCase))
                normalized += "/gamehub";
            return normalized;
        }

        private sealed class RenderRetryPolicy : IRetryPolicy
        {
            private static readonly TimeSpan[] Delays =
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
            };

            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                long count = retryContext.PreviousRetryCount;
                return count < Delays.Length ? Delays[count] : TimeSpan.FromSeconds(30);
            }
        }
    }
}
