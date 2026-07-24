using MediatR;
using TronListenBot.Domain.DomainService;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.MR.Command;

namespace TronListenBot.Svc.Core.MR.Transfer
{
    public class TransferRecordHandler(ILogger<TransferRecordHandler> logger,
        IServiceScopeFactory serviceScope) : INotificationHandler<TransferCommand>
    {
        readonly ILogger<TransferRecordHandler> _logger = logger;
        readonly IServiceScopeFactory _scopeFactory = serviceScope;

        public async Task Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var tronDomain = scope.ServiceProvider.GetRequiredService<TronDomainService>();

                var result = await tronDomain.AddTransactionRecord(new Domain.Aggregates.TransactionRecord
                {
                    HashId = request.Txid,
                    FromAddress = request.Parameter.FromAddress,
                    ToAddress = request.Parameter.ToAddress,
                    Amount = request.Parameter.Amount,
                    Currency = request.Parameter.Symbol,
                    Status = TransactionStatusEnum.Undetermined,
                    TransactionType = request.Parameter.Type,
                    TransactionTime = request.Parameter.TransactionTime,
                    ModifyTime = DateTime.UtcNow.GetTimeStamp()
                });

                if (result.Item1 == false)
                {
                    _logger.LogError($"添加交易记录失败,node:{request.Node},param:{request.Parameter.ToJsonEx()},msg:{result.Item2}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"添加交易记录异常,node:{request.Node},param:{request.Parameter.ToJsonEx()},error:{ex.Message}");
            }
        }
    }
}
