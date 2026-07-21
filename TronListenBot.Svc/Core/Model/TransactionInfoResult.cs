using TronListenBot.Infrastructure.Enums;

namespace TronListenBot.Svc.Core.Model
{
    public class Accountv2
    {
        /// <summary>
        /// TRX 余额
        /// </summary>
        public long Balance { get; set; }
        /// <summary>
        /// 投票总数
        /// </summary>
        public int VoteTotal { get; set; }
        /// <summary>
        /// Stake 2.0 冻结总额
        /// </summary>
        public long TotalFrozenV2 { get; set; }

        /// <summary>
        /// Stake 2.0 为能量冻结的数量
        /// </summary>
        public long FrozenForEnergyV2 { get; set; }
        /// <summary>
        /// Stake 2.0 为带宽冻结的数量
        /// </summary>
        public long FrozenForBandWidthV2 { get; set; }

        public Bandwidth Bandwidth { get; set; }

        /// <summary>
        /// 待提取奖励数量
        /// </summary>
        public long RewardNum { get; set; }
        /// <summary>
        /// 交易总数
        /// </summary>
        public int TotalTransactionCount { get; set; }
        /// <summary>
        /// 主交易笔数
        /// </summary>
        public int Transactions { get; set; }
        /// <summary>
        /// 转入交易笔数
        /// </summary>
        public int Transactions_In { get; set; }
        /// <summary>
        /// 转出交易笔数
        /// </summary>
        public int Transactions_Out { get; set; }
        /// <summary>
        /// 账户创建时间
        /// </summary>
        public long Date_Created { get; set; }
        /// <summary>
        /// 最近操作时间
        /// </summary>
        public long Latest_Operation_Time { get; set; }

        public List<WithPriceToken> WithPriceTokens { get; set; }
    }
    public class Bandwidth
    {
        /// <summary>
        /// 免费带宽上限
        /// </summary>
        public int FreeNetLimit { get; set; }
        /// <summary>
        /// 已使用免费带宽
        /// </summary>
        public int FreeNetUsed { get; set; }
        /// <summary>
        /// 剩余免费带宽
        /// </summary>
        public int FreeNetRemaining { get; set; }
        /// <summary>
        /// 质押兑换的带宽上限
        /// </summary>
        public int NetLimit { get; set; }
        /// <summary>
        /// 已使用质押带宽
        /// </summary>
        public int NetUsed { get; set; }
        /// <summary>
        /// 剩余质押带宽
        /// </summary>
        public int NetRemaining { get; set; }

        /// <summary>
        /// 质押兑换的能量上限
        /// </summary>
        public int EnergyLimit { get; set; }
        /// <summary>
        /// 已使用质押能量
        /// </summary>
        public int EnergyUsed { get; set; }
        /// <summary>
        /// 剩余能量
        /// </summary>
        public int EnergyRemaining { get; set; }

        /// <summary>
        /// 全网能量上限
        /// </summary>
        public long TotalEnergyLimit { get; set; }
        /// <summary>
        /// 全网能量权重
        /// </summary>
        public long TotalEnergyWeight { get; set; }
        /// <summary>
        /// 全网带宽上限
        /// </summary>
        public long TotalNetLimit { get; set; }
        /// <summary>
        /// 全网带宽权重
        /// </summary>
        public long TotalNetWeight { get; set; }
    }
    public class WithPriceToken
    {
        /// <summary>
        /// 代币合约地址（Base58）；TRX 占位符为 _
        /// </summary>
        public string TokenId { get; set; } = "";
        /// <summary>
        /// 余额（原值字符串）
        /// </summary>
        public long Balance { get; set; }
        /// <summary>
        /// 代币缩写（如 USDT / TRX）
        /// </summary>
        public string TokenAbbr { get; set; } = "";
        /// <summary>
        /// 精度位数
        /// </summary>
        public int TokenDecimal { get; set; }
    }


    public class TronTransferResult
    {
        public int Total { get; set; }
        public List<TransferTRXRecord> Data { get; set; }
        public List<TransferUSDTRecord> Token_Transfers { get; set; }
    }
    public class TransferTRXRecord
    {
        public string TransactionHash { get; set; } = "";
        public long Timestamp { get; set; }
        public string TransferFromAddress { get; set; } = "";
        public string TransferToAddress { get; set; } = "";
        public long Amount { get; set; }
    }
    public class TransferUSDTRecord
    {
        public string Transaction_Id { get; set; } = "";
        public long Block_Ts { get; set; }
        public string From_Address { get; set; } = "";
        public string To_Address { get; set; } = "";
        public long Quant { get; set; }
    }

    public class TransactionRecord
    {
        public string HashId { get; set; } = "";
        public string Symbol { get; set; } = "";
        /// <summary>
        /// 1.转入 2.转出
        /// </summary>
        public int TransactionType {  get; set; }
        public decimal Amount { get; set; }
        public long Timestamp { get; set; }
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
