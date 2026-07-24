using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using TronListenBot.Infrastructure.Enums;

namespace TronListenBot.Domain.Aggregates
{
    /// <summary>
    /// 交易流水表
    /// </summary>
    [JsonObject(MemberSerialization.OptIn), Table(Name = "transaction_record")]
    public class TransactionRecord
    {

        [JsonProperty, Column(Name = "id", IsPrimary = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 交易哈希ID
        /// </summary>
        [JsonProperty, Column(Name = "hash_id")]
        public string HashId { get; set; } = "";

        /// <summary>
        /// 发送人地址
        /// </summary>
        [JsonProperty, Column(Name = "from_address")]
        public string FromAddress { get; set; } = "";

        /// <summary>
        /// 接收人地址
        /// </summary>
        [JsonProperty, Column(Name = "to_address")]
        public string ToAddress { get; set; } = "";

        /// <summary>
        /// 金额
        /// </summary>
        [JsonProperty, Column(Name = "amount", DbType = "decimal(32,18)")]
        public decimal Amount { get; set; } = 0.000000000000000000M;

        /// <summary>
        /// 币种
        /// </summary>
        [JsonProperty, Column(Name = "currency")]
        public CurrencyEnum Currency { get; set; }

        /// <summary>
        /// 状态 1.未确定 2.已确定 3.过期
        /// </summary>
        [JsonProperty, Column(Name = "status")]
        public TransactionStatusEnum Status { get; set; } = 0;

        /// <summary>
        /// 交易类型
        /// </summary>
        [JsonProperty, Column(Name = "transaction_type")]
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// 交易时间
        /// </summary>
        [JsonProperty, Column(Name = "transaction_time")]
        public long TransactionTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonProperty, Column(Name = "modify_time")]
        public long ModifyTime { get; set; }

    }
}
