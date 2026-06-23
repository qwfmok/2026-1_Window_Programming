using CardChess.Server.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace CardChess.Server.Hubs;

public sealed class GameHub : Hub
{
    private readonly RoomManager roomManager;

    public GameHub(RoomManager roomManager)
    {
        this.roomManager = roomManager;
    }

    public Task<RoomJoinResult> CreateRoom()
    {
        return roomManager.CreateRoomAsync(Context.ConnectionId);
    }

    public Task<RoomJoinResult> JoinRoom(string roomCode)
    {
        return roomManager.JoinRoomAsync(Context.ConnectionId, roomCode);
    }

    public Task<RoomJoinResult> RejoinRoom(string roomCode, string playerToken)
    {
        return roomManager.RejoinRoomAsync(Context.ConnectionId, roomCode, playerToken);
    }

    public Task SendGameMessage(string roomCode, string playerToken, string message)
    {
        return roomManager.RelayMessageAsync(Context.ConnectionId, roomCode, playerToken, message);
    }

    public Task LeaveRoom(string roomCode, string playerToken)
    {
        return roomManager.LeaveRoomAsync(Context.ConnectionId, roomCode, playerToken);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await roomManager.HandleDisconnectedAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
