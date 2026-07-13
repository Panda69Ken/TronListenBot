namespace TronListenBot.Svc.Core.Model
{
    public class TgConfig
    {
        public string TgToken { get; set; } = string.Empty;
        public long TgUserId { get; set; }
        public bool Proxy { get; set; }
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;
        public string ProxyDomain { get; set; } = string.Empty;
    }
}
