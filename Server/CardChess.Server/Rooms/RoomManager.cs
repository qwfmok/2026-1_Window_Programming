using System.Security.Cryptography;
using CardChess.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CardChess.Server.Rooms;

public sealed class RoomManager
{
    public const int MaxRooms = 4;
    public const int MaxPlayers = 8;
    private static readonly TimeSpan ReconnectGracePeriod = TimeSpan.FromSeconds(20);
    private static readonly HashSet<string> AllowedMessageTypes = new(StringComparer.Ordinal)
    {
        "MOVE", "CARD", "PASS", "CHAT", "SURRENDER", "START", "HANDORDER"
    };

    private readonly object sync = new();
    private readonly Dictionary<string, GameRoom> rooms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string RoomCode, string PlayerToken)> connections = new(StringComparer.Ordinal);
    private readonly IHubContext<GameHub> hubContext;
    private readonly ILogger<RoomManager> logger;

    public RoomManager(IHubContext<GameHub> hubContext, ILogger<RoomManager> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public async Task<RoomJoinResult> CreateRoomAsync(string connectionId)
    {
        string roomCode;
        PlayerSlot host;

        lock (sync)
        {
            RemoveConnectionMapping(connectionId);
            if (rooms.Count >= MaxRooms || GetPlayerCountUnsafe() >= MaxPlayers)
                return RoomJoinResult.Failed("현재 생성 가능한 방이 모두 사용 중입니다.");

            roomCode = CreateUniqueRoomCodeUnsafe();
            host = new PlayerSlot
            {
                PlayerNumber = 1,
                Token = CreatePlayerToken(),
                ConnectionId = connectionId
            };
            rooms.Add(roomCode, new GameRoom { Code = roomCode, Host = host });
            connections[connectionId] = (roomCode, host.Token);
        }

        await hubContext.Groups.AddToGroupAsync(connectionId, roomCode);
        logger.LogInformation("Room {RoomCode} created", roomCode);
        return Success(roomCode, host, false);
    }

    public async Task<RoomJoinResult> JoinRoomAsync(string connectionId, string roomCode)
    {
        roomCode = NormalizeRoomCode(roomCode);
        if (!IsValidRoomCode(roomCode))
            return RoomJoinResult.Failed("방 코드는 숫자 6자리여야 합니다.");

        PlayerSlot guest;
        lock (sync)
        {
            RemoveConnectionMapping(connectionId);
            if (!rooms.TryGetValue(roomCode, out GameRoom? room))
                return RoomJoinResult.Failed("존재하지 않거나 종료된 방입니다.");
            if (room.Guest != null)
                return RoomJoinResult.Failed("이미 두 명이 참가한 방입니다.");
            if (GetPlayerCountUnsafe() >= MaxPlayers)
                return RoomJoinResult.Failed("서버의 최대 접속 인원에 도달했습니다.");

            guest = new PlayerSlot
            {
                PlayerNumber = 2,
                Token = CreatePlayerToken(),
                ConnectionId = connectionId
            };
            room.Guest = guest;
            connections[connectionId] = (roomCode, guest.Token);
        }

        await hubContext.Groups.AddToGroupAsync(connectionId, roomCode);
        await hubContext.Clients.Group(roomCode).SendAsync("PeerConnected");
        logger.LogInformation("Player 2 joined room {RoomCode}", roomCode);
        return Success(roomCode, guest, true);
    }

    public async Task<RoomJoinResult> RejoinRoomAsync(string connectionId, string roomCode, string playerToken)
    {
        roomCode = NormalizeRoomCode(roomCode);
        PlayerSlot? player;
        bool peerConnected;

        lock (sync)
        {
            if (!rooms.TryGetValue(roomCode, out GameRoom? room))
                return RoomJoinResult.Failed("서버가 재시작되어 기존 방이 사라졌습니다.");

            player = room.Players.FirstOrDefault(slot => FixedEquals(slot.Token, playerToken));
            if (player == null)
                return RoomJoinResult.Failed("재접속 토큰이 올바르지 않습니다.");

            if (!string.IsNullOrEmpty(player.ConnectionId))
                connections.Remove(player.ConnectionId);

            player.DisconnectCancellation?.Cancel();
            player.DisconnectCancellation?.Dispose();
            player.DisconnectCancellation = null;
            player.ConnectionId = connectionId;
            connections[connectionId] = (roomCode, player.Token);
            peerConnected = room.Players.Any(slot => slot != player && !string.IsNullOrEmpty(slot.ConnectionId));
        }

        await hubContext.Groups.AddToGroupAsync(connectionId, roomCode);
        await hubContext.Clients.GroupExcept(roomCode, new[] { connectionId }).SendAsync("PeerReconnected");
        logger.LogInformation("Player {PlayerNumber} rejoined room {RoomCode}", player.PlayerNumber, roomCode);
        return Success(roomCode, player, peerConnected);
    }

    public async Task RelayMessageAsync(string connectionId, string roomCode, string playerToken, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > 8192)
            throw new HubException("메시지 길이가 올바르지 않습니다.");

        string messageType = message.Split(',')[0];
        if (!AllowedMessageTypes.Contains(messageType))
            throw new HubException("허용되지 않은 게임 메시지입니다.");

        string? peerConnection;
        lock (sync)
        {
            PlayerSlot player = GetAuthenticatedPlayerUnsafe(connectionId, roomCode, playerToken);
            GameRoom room = rooms[roomCode];
            peerConnection = room.Players
                .Where(slot => slot != player)
                .Select(slot => slot.ConnectionId)
                .FirstOrDefault(id => !string.IsNullOrEmpty(id));
        }

        if (string.IsNullOrEmpty(peerConnection))
            throw new HubException("상대방의 재접속을 기다리고 있습니다.");

        await hubContext.Clients.Client(peerConnection).SendAsync("GameMessage", message);
    }

    public async Task LeaveRoomAsync(string connectionId, string roomCode, string playerToken)
    {
        string? peerConnection = null;
        lock (sync)
        {
            if (!rooms.TryGetValue(roomCode, out GameRoom? room))
                return;

            PlayerSlot? player = room.Players.FirstOrDefault(slot =>
                slot.ConnectionId == connectionId && FixedEquals(slot.Token, playerToken));
            if (player == null)
                return;

            peerConnection = room.Players.Where(slot => slot != player).Select(slot => slot.ConnectionId).FirstOrDefault();
            RemoveRoomUnsafe(roomCode);
        }

        await hubContext.Groups.RemoveFromGroupAsync(connectionId, roomCode);
        if (!string.IsNullOrEmpty(peerConnection))
            await hubContext.Clients.Client(peerConnection).SendAsync("OpponentDisconnected");
    }

    public Task HandleDisconnectedAsync(string connectionId)
    {
        string? roomCode = null;
        string? playerToken = null;
        string? peerConnection = null;
        CancellationTokenSource? cancellation = null;

        lock (sync)
        {
            if (!connections.TryGetValue(connectionId, out var membership) ||
                !rooms.TryGetValue(membership.RoomCode, out GameRoom? room))
                return Task.CompletedTask;

            PlayerSlot? player = room.Players.FirstOrDefault(slot => slot.ConnectionId == connectionId);
            if (player == null)
                return Task.CompletedTask;

            connections.Remove(connectionId);
            player.ConnectionId = null;
            player.DisconnectCancellation?.Cancel();
            player.DisconnectCancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            player.DisconnectCancellation = cancellation;
            roomCode = room.Code;
            playerToken = player.Token;
            peerConnection = room.Players.Where(slot => slot != player).Select(slot => slot.ConnectionId).FirstOrDefault();
        }

        if (!string.IsNullOrEmpty(peerConnection))
            _ = hubContext.Clients.Client(peerConnection).SendAsync("PeerReconnecting");

        _ = FinalizeDisconnectAfterGraceAsync(roomCode!, playerToken!, cancellation!);
        return Task.CompletedTask;
    }

    public RoomServerStatus GetStatus()
    {
        lock (sync)
        {
            return new RoomServerStatus
            {
                Status = "running",
                RoomCount = rooms.Count,
                PlayerCount = GetPlayerCountUnsafe(),
                MaxRooms = MaxRooms,
                MaxPlayers = MaxPlayers,
                Utc = DateTimeOffset.UtcNow
            };
        }
    }

    private async Task FinalizeDisconnectAfterGraceAsync(
        string roomCode,
        string playerToken,
        CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(ReconnectGracePeriod, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string? peerConnection = null;
        lock (sync)
        {
            if (!rooms.TryGetValue(roomCode, out GameRoom? room))
                return;

            PlayerSlot? player = room.Players.FirstOrDefault(slot => FixedEquals(slot.Token, playerToken));
            if (player == null || player.ConnectionId != null || player.DisconnectCancellation != cancellation)
                return;

            peerConnection = room.Players.Where(slot => slot != player).Select(slot => slot.ConnectionId).FirstOrDefault();
            RemoveRoomUnsafe(roomCode);
        }

        if (!string.IsNullOrEmpty(peerConnection))
            await hubContext.Clients.Client(peerConnection).SendAsync("OpponentDisconnected");
        logger.LogInformation("Room {RoomCode} removed after reconnect timeout", roomCode);
    }

    private PlayerSlot GetAuthenticatedPlayerUnsafe(string connectionId, string roomCode, string playerToken)
    {
        roomCode = NormalizeRoomCode(roomCode);
        if (!rooms.TryGetValue(roomCode, out GameRoom? room))
            throw new HubException("존재하지 않는 방입니다.");

        PlayerSlot? player = room.Players.FirstOrDefault(slot =>
            slot.ConnectionId == connectionId && FixedEquals(slot.Token, playerToken));
        return player ?? throw new HubException("방 참가 인증에 실패했습니다.");
    }

    private void RemoveRoomUnsafe(string roomCode)
    {
        if (!rooms.Remove(roomCode, out GameRoom? room))
            return;

        foreach (PlayerSlot player in room.Players)
        {
            if (!string.IsNullOrEmpty(player.ConnectionId))
                connections.Remove(player.ConnectionId);
            player.DisconnectCancellation?.Cancel();
            player.DisconnectCancellation?.Dispose();
        }
    }

    private void RemoveConnectionMapping(string connectionId)
    {
        if (connections.TryGetValue(connectionId, out var membership))
            RemoveRoomUnsafe(membership.RoomCode);
    }

    private int GetPlayerCountUnsafe() => rooms.Values.Sum(room => room.Players.Count());

    private string CreateUniqueRoomCodeUnsafe()
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        } while (rooms.ContainsKey(code));
        return code;
    }

    private static string CreatePlayerToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    private static string NormalizeRoomCode(string roomCode) => (roomCode ?? string.Empty).Trim();
    private static bool IsValidRoomCode(string roomCode) => roomCode.Length == 6 && roomCode.All(char.IsDigit);
    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(left ?? string.Empty),
            System.Text.Encoding.UTF8.GetBytes(right ?? string.Empty));

    private static RoomJoinResult Success(string roomCode, PlayerSlot player, bool peerConnected) => new()
    {
        Success = true,
        RoomCode = roomCode,
        PlayerToken = player.Token,
        PlayerNumber = player.PlayerNumber,
        PeerConnected = peerConnected
    };
}
