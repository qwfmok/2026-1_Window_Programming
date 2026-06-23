using System;
using System.Configuration;

namespace CardChess.Core
{
    public static class NetworkSettings
    {
        public static string SignalRServerUrl
        {
            get
            {
                string environmentUrl = Environment.GetEnvironmentVariable("CARDCHESS_SERVER_URL");
                if (!string.IsNullOrWhiteSpace(environmentUrl))
                    return environmentUrl.Trim();

                string configuredUrl = ConfigurationManager.AppSettings["SignalRServerUrl"];
                return string.IsNullOrWhiteSpace(configuredUrl)
                    ? "http://localhost:5080/gamehub"
                    : configuredUrl.Trim();
            }
        }
    }
}
