using MediatR;
using TronListenBot.Svc.Core.Model;
using TronNet.Protocol;

namespace TronListenBot.Svc.Core.MR.Command
{
    public class TransferCommand : IRequest
    {
        public string Txid {  get; set; }
        public TransactionExtention Transaction { get; set; }
        public TransactionParameter Parameter { get; set; }
        public string ParamJson {  get; set; }
        public string Node { get; set; } = "";
    }
}
