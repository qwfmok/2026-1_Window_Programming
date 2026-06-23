# Card Chess SignalR Server

## Local run

```powershell
$env:PORT = "5080"
dotnet run --project Server/CardChess.Server/CardChess.Server.csproj
```

The client defaults to `http://localhost:5080/gamehub`.

## Render deployment

1. Push this repository to GitHub.
2. In Render, create a Blueprint and select the repository.
3. Render reads the root `render.yaml` and builds the Docker service.
4. After deployment, copy the service URL into `App.config` before rebuilding:

```xml
<add key="SignalRServerUrl" value="https://YOUR-SERVICE.onrender.com/gamehub" />
```

For an already-built client, edit the same setting in `CardChess.exe.config`
next to the executable. You can also set the `CARDCHESS_SERVER_URL`
environment variable without rebuilding.

Free Render services sleep after inactivity. The client displays a wake-up
message and retries the initial SignalR connection automatically.
