using TronListenBot.Infrastructure.Enums;

namespace TronListenBot.Svc.Core.Model
{
    public class TransactionRecord
    {
        public string HashId { get; set; } = "";
        public string Symbol { get; set; } = "";
        /// <summary>
        /// 1.转入 2.转出
        /// </summary>
        public int TransactionType {  get; set; }
        public decimal Amount { get; set; }
        public long TransactionTime { get; set; }
    }

    public class TransactionParameter
    {
        public string FromAddress { get; set; } = "";
        public string ToAddress { get; set; } = "";
        public long Amount { get; set; }
        public string Symbol { get; set; } = "";
        public TransactionType Type { get; set; }
    }

}
