using TronListenBot.Domain.DomainService;
using TronListenBot.Domain.QueryServices;
using TronListenBot.Svc.Core.Block;
using TronListenBot.Svc.Core.Service;
using TronNet.Protocol;

namespace TronListenBot.Svc.Worker
{
    public class ScanBlockWorker(ILogger<ScanBlockWorker> logger, IServiceScopeFactory scopeFactory,
        TronQueries tronQueries, TronNetRecord tron, TransferBlock block) : BackgroundService
    {
        readonly ILogger<ScanBlockWorker> _logger = logger;
        readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        readonly TronQueries _tronQueries = tronQueries;
        readonly TronNetRecord _tron = tron;
        readonly TransferBlock _block = block;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScanBlockWorker running at: {time}", DateTimeOffset.Now);

            var wallet = _tron.TronClient.GetWallet();
            var walletFull = wallet.GetProtocol();

            var nodeList = new List<string> { "node1", "node2" };
            var key = "tron_block";
            var tasks = new Task[nodeList.Count];
            var num = 0;

            using var scope = _scopeFactory.CreateScope();

            var tronDomain = scope.ServiceProvider.GetRequiredService<TronDomainService>();

            var exist = await _tronQueries.BlockNumberKeyExist(key);

            var nowBlockExt = await walletFull.GetNowBlock2Async(new EmptyMessage(), headers: wallet.GetHeaders(), cancellationToken: stoppingToken);
            var number = nowBlockExt.BlockHeader.RawData.Number;
            number--;   //类似redis自增每次请求读取都会自增加1，区块从上一个开始计算

            if (exist == false)
            {
                await tronDomain.StringIncrement(key, number);
            }
            else
            {
                await tronDomain.SetTronNowBlock(key, number);
            }

            foreach (var node in nodeList)
            {
                await tronDomain.InitNodeBlockNumber(key, node);

                tasks[num] = Task.Run(async () =>
                {
                    var interval = 1000;

                    //获取任务节点最近处理的区块
                    var lastNumber = await _tronQueries.GetNodeBlockNumber(key, node);

                    if (lastNumber == 0)
                    {
                        lastNumber = await tronDomain.StringIncrement(key);

                        //记录当前节点处理的区块
                        await tronDomain.SetNodeBlockNumber(key, node, lastNumber);
                    }

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            //获取链上最新区块
                            var nowBlockExt = await walletFull.GetNowBlock2Async(new EmptyMessage(), headers: wallet.GetHeaders(), cancellationToken: stoppingToken);
                            if (nowBlockExt == null || nowBlockExt.BlockHeader == null)
                            {
                                _logger.LogWarning("GetNowBlock2Async 返回了null或空值; 正在重试......");
                                await Task.Delay(interval * 2, stoppingToken);
                                continue;
                            }
                            var latestHeight = nowBlockExt.BlockHeader.RawData.Number;

                            //当前区块比链上区块大 或者 还没产生最新区块，轮空等待
                            if (lastNumber >= latestHeight)
                            {
                                //_logger.LogWarning($"{node} 当前区块比链上区块大或者还没产生最新区块,轮空等待--Now:{lastNumber}--BlockHeight:{latestHeight}");
                                await Task.Delay(interval * 2, stoppingToken);
                                continue;
                            }

                            //以 GetBlockByNum2 接口的交易为准
                            var blockExt = await walletFull.GetBlockByNum2Async(new NumberMessage { Num = lastNumber },
                                headers: wallet.GetHeaders(), cancellationToken: stoppingToken);

                            if (blockExt.Transactions != null || blockExt.Transactions!.Count > 0)
                            {
                                //_logger.LogWarning($"Task:{node}, 当前区块:{lastNumber},交易笔数:{blockExt.Transactions.Count}");

                                _block.Post(new TransferModel
                                {
                                    Node = $"{node}--{lastNumber}",
                                    Transactions = blockExt.Transactions
                                });
                            }

                            //处理完成取得最新的区块
                            lastNumber = await tronDomain.StringIncrement(key);

                            //记录当前节点处理的区块
                            await tronDomain.SetNodeBlockNumber(key, node, lastNumber);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"{node} 获取区块数据异常,error:{ex.Message}");
                        }

                        await Task.Delay(interval * 2, stoppingToken);
                    }
                }, stoppingToken);

                num++;
            }

            Task.WaitAny(tasks, stoppingToken);
        }

    }
}
