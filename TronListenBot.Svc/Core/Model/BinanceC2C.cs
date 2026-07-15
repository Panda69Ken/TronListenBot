namespace TronListenBot.Svc.Core.Model
{
    public class BinanceC2C
    {
        public int Code { get; set; }
        public List<BinanceC2CData> Data { get; set; }
    }

    public class BinanceC2CData
    {
        public BinanceAdv Adv { get; set; }
        public BinanceAdvertiser Advertiser { get; set; }
    }

    public class BinanceAdvertiser
    {
        public string NickName { get; set; }
        public int MonthOrderCount { get; set; }
        public double MonthFinishRate { get; set; }

    }
    public class BinanceAdv
    {
        public List<BinanceTradeMethods> TradeMethods { get; set; }
        public double Price { get; set; }
        public int PayTimeLimit { get; set; }
        public double TradableQuantity { get; set; }
        public double MinSingleTransAmount { get; set; }
        public double MaxSingleTransAmount { get; set; }
    }

    public class BinanceTradeMethods
    {
        public string PayType { get; set; }
    }

}
