namespace TronListenBot.Svc.Core.Model
{
    public class TgC2CInfo
    {
        public string Command { get; set; }
        /// <summary>
        /// 0.all 1.bank 2.aliPay 3.wxPay
        /// </summary>
        public int RotaType { get; set; }
    }
}
