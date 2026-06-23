using CardChess.Server.Hubs;
using CardChess.Server.Rooms;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(45);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 16 * 1024;
});
builder.Services.AddSingleton<RoomManager>();

string port = Environment.GetEnvironmentVariable("PORT") ?? "5080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

WebApplication app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Card Chess SignalR Relay",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));
app.MapGet("/health", (RoomManager rooms) => Results.Ok(rooms.GetStatus()));
app.MapHub<GameHub>("/gamehub");

app.Run();
