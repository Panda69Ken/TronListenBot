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
        TRX = 1,
        USDT = 2,
        Energy = 3,
        UnEnergy = 4,
        Bandwidth = 5,
        UnBandwidth = 6,
        WithdrawTRX = 7,
        DelegateEnergy = 8,
        UnDelegateEnergy = 9,
        DelegateBandwidth = 10,
        UnDelegateBandwidth = 11,
        CancelAllUnfreezeV2 = 12,
        WithdrawExpireUnfreeze = 13,
    }
}
