FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CardChess.Server.csproj ./
RUN dotnet restore CardChess.Server.csproj
COPY . ./
RUN dotnet publish CardChess.Server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "CardChess.Server.dll"]
