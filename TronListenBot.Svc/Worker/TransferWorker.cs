using TronListenBot.Domain.DomainService;
using TronListenBot.Domain.QueryServices;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Svc.Core.Service;
using TronNet.Protocol;

namespace TronListenBot.Svc.Worker
{
    public class TransferWorker(ILogger<TransferWorker> logger,
        IServiceScopeFactory scopeFactory,
        TronQueries tronQueries,
        TronNetRecord tron) : BackgroundService
    {
        readonly ILogger<TransferWorker> _logger = logger;
        readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        readonly TronQueries _tronQueries = tronQueries;
        readonly TronNetRecord _tron = tron;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TransferWorker running at: {time}", DateTimeOffset.Now);

            var wallet = _tron.TronClient.GetWallet();
            var walletFull = wallet.GetProtocol();

            using var scope = _scopeFactory.CreateScope();
            var tronDomain = scope.ServiceProvider.GetRequiredService<TronDomainService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var list = await _tronQueries.GetTransactionRecord();

                foreach (var item in list)
                {
                    var transaction = await walletFull.GetTransactionByIdAsync(new BytesMessage
                    {
                        Value = wallet.ParseAddress(item.HashId)
                    }, cancellationToken: stoppingToken);

                    if (transaction.Ret.Count == 0) continue;

                    var contractRet = transaction.Ret[0].ContractRet;

                    var status = TransactionStatusEnum.None;

                    status = contractRet switch
                    {
                        Transaction.Types.Result.Types.contractResult.Success => TransactionStatusEnum.Confirmed,
                        Transaction.Types.Result.Types.contractResult.Default
                            or Transaction.Types.Result.Types.contractResult.Revert
                            or Transaction.Types.Result.Types.contractResult.Unknown => TransactionStatusEnum.Undetermined,
                        _ => TransactionStatusEnum.Expired,
                    };
                    if (status == TransactionStatusEnum.Undetermined) continue;

                    var result = await tronDomain.UpdateTransactionStatus(item.Id, status);

                    if (result == false) _logger.LogWarning($"更新交易记录状态失败,HashId:{item.HashId}");
                }
                await Task.Delay(2000, stoppingToken);
            }

            await Task.CompletedTask;
        }

    }
}
