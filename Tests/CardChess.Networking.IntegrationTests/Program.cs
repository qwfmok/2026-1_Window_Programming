using CardChess.Networking;
using System.Collections.Concurrent;

string serverUrl = args.Length > 0 ? args[0] : "http://localhost:5080/gamehub";
if (args.Length > 2 && args[1] == "--restart-probe")
{
    await RunRestartProbeAsync(serverUrl, args[2]);
    return;
}

using SignalRProtocol host = new SignalRProtocol(serverUrl);
using SignalRProtocol guest = new SignalRProtocol(serverUrl);

ConcurrentQueue<string> hostMessages = new ConcurrentQueue<string>();
ConcurrentQueue<string> guestMessages = new ConcurrentQueue<string>();
host.OnMessage += hostMessages.Enqueue;
guest.OnMessage += guestMessages.Enqueue;

string? roomCode = await host.CreateRoomAsync();
Require(roomCode != null && roomCode.Length == 6 && roomCode.All(char.IsDigit), "server-issued six-digit room code");
Require(await guest.JoinRoomAsync(roomCode), "guest joins room");
await WaitUntilAsync(() => host.IsConnected && guest.IsConnected, "both peers connected");

using (SignalRProtocol thirdPlayer = new SignalRProtocol(serverUrl))
{
    Require(!await thirdPlayer.JoinRoomAsync(roomCode), "third player is rejected from a full room");
}

string[] hostPackets =
{
    "START,24680",
    "MOVE,6,0,5,0,17,4",
    "CARD,랜덤 진화,4,4,2,8",
    "PASS",
    "CHAT,host,message",
    "HANDORDER,Player1,0,1",
    "SURRENDER"
};

foreach (string packet in hostPackets)
{
    Require(await host.SendAsync(packet), "host sends " + packet.Split(',')[0]);
    await WaitUntilAsync(() => guestMessages.Contains(packet), "guest receives " + packet);
}

string[] guestPackets =
{
    "MOVE,1,0,2,0,3",
    "CARD,두장 뽑기,3,3,5,6",
    "PASS",
    "CHAT,guest reply",
    "HANDORDER,Player2,1,0",
    "SURRENDER"
};

foreach (string packet in guestPackets)
{
    Require(await guest.SendAsync(packet), "guest sends " + packet.Split(',')[0]);
    await WaitUntilAsync(() => hostMessages.Contains(packet), "host receives " + packet);
}

guest.Close();
await WaitUntilAsync(() => hostMessages.Contains("OPPONENT_DISCONNECTED"), "host receives opponent disconnect");
host.Close();

List<SignalRProtocol> roomHosts = new List<SignalRProtocol>();
try
{
    HashSet<string> issuedCodes = new HashSet<string>();
    for (int index = 0; index < 4; index++)
    {
        SignalRProtocol roomHost = new SignalRProtocol(serverUrl);
        roomHosts.Add(roomHost);
        string? issuedCode = await roomHost.CreateRoomAsync();
        Require(issuedCode != null, "create room " + (index + 1));
        Require(issuedCodes.Add(issuedCode!), "room codes are unique");
    }

    using SignalRProtocol rejectedHost = new SignalRProtocol(serverUrl);
    Require(await rejectedHost.CreateRoomAsync() == null, "fifth room is rejected");
}
finally
{
    foreach (SignalRProtocol roomHost in roomHosts)
        roomHost.Dispose();
}

Console.WriteLine($"PASS room={roomCode} hostPackets={hostPackets.Length} guestPackets={guestPackets.Length} limits=ok disconnect=ok");

static void Require(bool condition, string operation)
{
    if (!condition)
        throw new InvalidOperationException("FAILED: " + operation);
}

static async Task WaitUntilAsync(Func<bool> condition, string operation)
{
    DateTime timeout = DateTime.UtcNow.AddSeconds(10);
    while (!condition())
    {
        if (DateTime.UtcNow >= timeout)
            throw new TimeoutException("TIMEOUT: " + operation);
        await Task.Delay(50);
    }
}

static async Task RunRestartProbeAsync(string serverUrl, string continueFile)
{
    using SignalRProtocol host = new SignalRProtocol(serverUrl);
    using SignalRProtocol guest = new SignalRProtocol(serverUrl);
    ConcurrentQueue<string> hostMessages = new ConcurrentQueue<string>();
    ConcurrentQueue<string> guestMessages = new ConcurrentQueue<string>();
    host.OnMessage += hostMessages.Enqueue;
    guest.OnMessage += guestMessages.Enqueue;

    string? roomCode = await host.CreateRoomAsync();
    Require(roomCode != null, "restart probe room creation");
    Require(await guest.JoinRoomAsync(roomCode), "restart probe room join");
    await WaitUntilAsync(() => host.IsConnected && guest.IsConnected, "restart probe peers connected");
    Console.WriteLine("READY " + roomCode);

    DateTime signalTimeout = DateTime.UtcNow.AddSeconds(30);
    while (!File.Exists(continueFile))
    {
        if (DateTime.UtcNow >= signalTimeout)
            throw new TimeoutException("TIMEOUT: restart signal");
        await Task.Delay(100);
    }

    DateTime reconnectTimeout = DateTime.UtcNow.AddSeconds(90);
    while (!hostMessages.Contains("ROOM_LOST") || !guestMessages.Contains("ROOM_LOST"))
    {
        if (DateTime.UtcNow >= reconnectTimeout)
            throw new TimeoutException("TIMEOUT: clients detect room loss after server restart");
        await Task.Delay(100);
    }

    Require(hostMessages.Contains("SERVER_RECONNECTING"), "host enters automatic reconnect");
    Require(guestMessages.Contains("SERVER_RECONNECTING"), "guest enters automatic reconnect");
    Console.WriteLine("PASS automatic-reconnect=ok room-loss=ok");
}
