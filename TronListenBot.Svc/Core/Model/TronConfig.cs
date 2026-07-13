namespace TronListenBot.Svc.Core.Model
{
    public class TronConfig
    {
        public string TronGrid { get; set; } = "";
        public string GrpcUrl { get; set; } = "";
        public int Port { get; set; }
        public int SolidityPort { get; set; }
        public required List<string> ApiKeys { get; set; }
        public string Contract { get; set; } = "";
        public string WebsiteUrl { get; set; } = "";
        public string Address { get; set; } = "";
    }
}
