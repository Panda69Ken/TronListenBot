namespace TronListenBot.Infrastructure.Enums
{
    public enum CurrencyEnum
    {
        USDT,
        TRX,
        //BTC,
        //ETH,
        //BNB,
    }

    public enum TransactionType
    {
        TransferContract = 1,
        TriggerSmartContract = 2,
        FreezeBalanceV2Energy = 3,
        UnfreezeBalanceV2Energy = 4,
        FreezeBalanceV2Bandwidth = 5,
        UnfreezeBalanceV2Bandwidth = 6,
        WithdrawBalanceContract = 7,
        DelegateResourceEnergy = 8,
        UnDelegateResourceEnergy = 9,
        DelegateResourceBandwidth = 10,
        UnDelegateResourceBandwidth = 11,
        CancelAllUnfreezeV2Contract = 12,
        WithdrawExpireUnfreezeContract = 13,
    }

    public enum TransactionStatusEnum
    {
        None = 0,
        Undetermined = 1,
        Confirmed = 2,
        Expired = 3,
    }
}
