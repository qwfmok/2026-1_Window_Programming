namespace CardChess.Server.Rooms;

public sealed class RoomJoinResult
{
    public bool Success { get; init; }
    public string? RoomCode { get; init; }
    public string? PlayerToken { get; init; }
    public int PlayerNumber { get; init; }
    public bool PeerConnected { get; init; }
    public string? Error { get; init; }

    public static RoomJoinResult Failed(string error) => new()
    {
        Success = false,
        Error = error
    };
}

internal sealed class PlayerSlot
{
    public required int PlayerNumber { get; init; }
    public required string Token { get; init; }
    public string? ConnectionId { get; set; }
    public CancellationTokenSource? DisconnectCancellation { get; set; }
}

internal sealed class GameRoom
{
    public required string Code { get; init; }
    public required PlayerSlot Host { get; init; }
    public PlayerSlot? Guest { get; set; }

    public IEnumerable<PlayerSlot> Players
    {
        get
        {
            yield return Host;
            if (Guest != null)
                yield return Guest;
        }
    }
}

public sealed class RoomServerStatus
{
    public required string Status { get; init; }
    public int RoomCount { get; init; }
    public int PlayerCount { get; init; }
    public int MaxRooms { get; init; }
    public int MaxPlayers { get; init; }
    public DateTimeOffset Utc { get; init; }
}
