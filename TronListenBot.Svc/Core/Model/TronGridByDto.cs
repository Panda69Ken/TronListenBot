namespace TronListenBot.Svc.Core.Model
{
    public class TronGridTransactionInfoMeta
    {
        public TronGridTransactionInfoMetaLinks links { get; set; }
    }
    public class TronGridTransactionInfoMetaLinks
    {
        public string next { get; set; } = "";
    }

    public class TronGridTRXTransactionInfoResult
    {
        public List<TronGridTRXTransactionInfoData> data { get; set; }
        public TronGridTransactionInfoMeta meta { get; set; }
    }
    public class TronGridTRXTransactionInfoData
    {
        public string txID { get; set; }
        public long blockNumber { get; set; }
        public TronGridTRXTransactionInfoRawData raw_data { get; set; }
        public long block_timestamp { get; set; }
    }
    public class TronGridTRXTransactionInfoRawData
    {
        public List<TronGridTRXTransactionInfoRawDataContract> contract { get; set; }
        public long timestamp { get; set; }
    }
    public class TronGridTRXTransactionInfoRawDataContract
    {
        public TronGridTRXTransactionInfoRawDataContractParameter parameter { get; set; }
        public string type { get; set; }
    }
    public class TronGridTRXTransactionInfoRawDataContractParameter
    {
        public TronGridTRXTransactionInfoRawDataContractParameterValue value { get; set; }
        public string type_url { get; set; }
    }
    public class TronGridTRXTransactionInfoRawDataContractParameterValue
    {
        public long amount { get; set; }
        public string owner_address { get; set; }
        public string to_address { get; set; }
    }

    public class TronGridTUSDTransactionInfoResult
    {
        public List<TronGridUSDTTransactionInfoData> data { get; set; }
        public TronGridTransactionInfoMeta meta { get; set; }
    }
    public class TronGridUSDTTransactionInfoData
    {
        public string transaction_id { get; set; }
        public TronGridUSDTTransactionInfoDataTokenInfo token_info { get; set; }
        public long block_timestamp { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public long value { get; set; }
    }
    public class TronGridUSDTTransactionInfoDataTokenInfo
    {
        public string symbol { get; set; }
        public int decimals { get; set; }
    }

}
