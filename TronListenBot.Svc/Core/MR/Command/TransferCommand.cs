using MediatR;
using TronListenBot.Svc.Core.Model;

namespace TronListenBot.Svc.Core.MR.Command
{
    public class TransferCommand : INotification
    {
        public string Txid { get; set; } = "";
        public required TransactionParameter Parameter { get; set; }
        public string Node { get; set; } = "";
    }
}
