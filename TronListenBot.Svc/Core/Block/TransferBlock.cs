using Google.Protobuf.Collections;
using MediatR;
using Newtonsoft.Json;
using System.Threading.Tasks.Dataflow;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Expansion;
using TronListenBot.Svc.Core.Model;
using TronListenBot.Svc.Core.MR.Command;
using TronNet;
using TronNet.Protocol;
using static TronNet.Protocol.Transaction.Types.Contract.Types;

namespace TronListenBot.Svc.Core.Block
{
    public class TransferModel
    {
        public RepeatedField<TransactionExtention> Transactions { get; set; }
        public string Node { get; set; } = "";
    }

    public class TransferBlock
    {
        readonly ActionBlock<TransferModel> _action;

        readonly ILogger<TransferBlock> _logger;
        readonly IMediator _mediator;

        public TransferBlock(ILogger<TransferBlock> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;

            _action = new ActionBlock<TransferModel>(async (item) =>
            {
                await BlockHandler(item);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });
        }

        public bool Post(TransferModel request)
        {
            return _action.Post(request);
        }

        private async Task BlockHandler(TransferModel request)
        {
            try
            {
                foreach (var item in request.Transactions)
                {
                    var type = item.Transaction.RawData.Contract[0].Type;

                    var contractTypes = new List<ContractType> {
                        ContractType.TransferContract,
                        ContractType.TriggerSmartContract,
                        ContractType.FreezeBalanceV2Contract,
                        ContractType.UnfreezeBalanceV2Contract,
                        ContractType.WithdrawBalanceContract,
                        ContractType.DelegateResourceContract,
                        ContractType.UnDelegateResourceContract,
                        ContractType.CancelAllUnfreezeV2Contract,
                        ContractType.WithdrawExpireUnfreezeContract
                    };

                    var txid = (item.Txid != null && item.Txid.Length > 0)
                           ? Convert.ToHexString(item.Txid.ToByteArray()).ToLowerInvariant()
                           : item.Transaction?.GetTxid();

                    if (contractTypes.Contains(type))
                    {
                        string paramJson = "";

                        paramJson = item.Transaction.RawData.Contract[0].Parameter.ParameterValueToJson();

                        var Json = JsonConvert.DeserializeObject<TransactionParameter>(paramJson);

                        await _mediator.Send(new TransferCommand
                        {
                            Txid = txid,
                            Transaction = item,
                            Parameter = Json,
                            ParamJson = paramJson,
                            Node = request.Node
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"交易通知异常,node:{request.Node},param:{request.ToJsonEx()},error:{ex.Message}");
            }
        }

    }
}
