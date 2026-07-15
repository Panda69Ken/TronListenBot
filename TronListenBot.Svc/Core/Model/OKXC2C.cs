namespace TronListenBot.Svc.Core.Model
{
    public class OKXC2C
    {
        public int Code { get; set; }
        public OKXC2CData Data { get; set; }
    }

    public class OKXC2CData
    {
        public List<OKXC2CInfo> Sell { get; set; }
        public List<OKXC2CInfo> Buy { get; set; }
    }

    public class OKXC2CInfo
    {
        public string NickName { get; set; }
        public List<string> PaymentMethods { get; set; }
        public double Price { get; set; }
        public int CompletedOrderQuantity { get; set; }
        public double AvailableAmount { get; set; }
        public int AvgCompletedTime { get; set; }
        public double CompletedRate {  get; set; }
        public double QuoteMaxAmountPerOrder { get; set; }
        public double QuoteMinAmountPerOrder { get; set; }
    }
}
