using Google.Protobuf;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Model;
using TronListenBot.Svc.Core.MR.Command;
using TronListenBot.Svc.Core.Service;
using TronNet;
using TronNet.Contracts;
using TronNet.Crypto;
using TronNet.Protocol;

namespace TronListenBot.Svc.Core.MR
{
    public class MsgHandler : IRequestHandler<TgMsgCommand>
    {
        readonly ILogger<MsgHandler> _logger;
        readonly IConfigService _config;
        readonly ICacheService _cache;
        readonly ITelegramBotClient _botClient;
        readonly ITronGridClient _tronGridService;
       
        readonly TronNetRecord _tron;

        public MsgHandler(ILogger<MsgHandler> logger,
            IConfigService config,
            ICacheService cache,
            ITelegramBotClient botClient,
            ITronGridClient tronGridClient,
            TronNetRecord tron
            )
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _botClient = botClient;
            _tronGridService = tronGridClient;
            _tron = tron;
        }

        public async Task Handle(TgMsgCommand request, CancellationToken cancellationToken)
        {
            if (request.Update.Message is null) return;

            var message = request.Update.Message;
            var text = message.Text ?? "";

            var tgChatId = message.Chat.Id;
            var msgId = message.MessageId;
            var fromId = message.From.Id;

            var locker = new MemoryLock();
            string keyLock = _cache.SetChatDetaileLock($"{_botClient.BotId}_{tgChatId}_{msgId}_{fromId}_{text}");
            var token = await locker.AcquireAsync(keyLock, TimeSpan.FromSeconds(60));

            try
            {
                if (token != null)
                {
                    if (text.StartsWith('/'))
                    {
                        var space = text.IndexOf(' ');
                        if (space < 0) space = text.Length;
                        var command = text[..space].ToLower();
                        if (command.LastIndexOf('@') is > 0 and var at)
                        {
                            if (command[(at + 1)..].Equals(request.UserName, StringComparison.OrdinalIgnoreCase))
                                command = command[..at];
                        }
                        await ExecCommand(command, text[space..].TrimStart(), request);
                    }
                    if (message.ReplyToMessage != null)
                    {
                        //await ReplyMessageCommand(request);
                    }

                    locker.Release(keyLock, token);
                }
            }
            catch (Exception ex)
            {
                locker.Release(keyLock, token);
                _logger.LogError($"处理指令异常,botId:{_botClient.BotId},message:{message.ToJsonEx()},error:{ex.Message}");
            }
        }

        async Task ExecCommand(string command, string args, TgMsgCommand request)
        {
            var msg = request.Update.Message;
            var tgChatId = msg.Chat.Id;
            var msgId = msg.MessageId;

            var text = "";

            switch (command)
            {
                case "/me":
                    text = Me(request);
                    break;
                case "/address_info":
                    text = await AddressInfo(request);
                    break;
            }

            if (string.IsNullOrEmpty(text)) return;

            await _botClient.SendMessage(tgChatId, text,
                parseMode: ParseMode.Html,
                linkPreviewOptions: new Telegram.Bot.Types.LinkPreviewOptions { IsDisabled = true },
                protectContent: false,
                replyParameters: msgId);
        }

        string Me(TgMsgCommand request)
        {
            var msg = request.Update.Message;
            var fromId = msg.From.Id;
            var fromName = $"{msg.From.FirstName}{msg.From.LastName}" + (string.IsNullOrEmpty(msg.From.Username) ? "" : $"(@{msg.From.Username})");

            return $"ID: {fromId}\n Name: {fromName}";
        }

        async Task<string> AddressInfo(TgMsgCommand request)
        {
            var wallet = _tron.TronClient.GetWallet();
            var contractClient = _tron.ContractClientFactory.CreateClient(ContractProtocol.TRC20);

            var resultUsdt = await contractClient.BalanceOfAsync(_config.TronConfig.Contract, _config.TronConfig.Address);

            //查询账户信息
            var accountInfo = await wallet.GetProtocol().GetAccountAsync(new Account
            {
                Address = ByteString.CopyFrom(Base58Encoder.DecodeFromBase58Check(_config.TronConfig.Address))
            }, headers: wallet.GetHeaders());

            //带宽和能量
            var accountResource = await wallet.GetProtocol().GetAccountResourceAsync(new Account
            {
                Address = ByteString.CopyFrom(Base58Encoder.DecodeFromBase58Check(_config.TronConfig.Address))
            }, headers: wallet.GetHeaders());

            //获取待领权益
            var accountReward = await wallet.GetProtocol().GetRewardInfoAsync(new BytesMessage
            {
                Value = ByteString.CopyFrom(Base58Encoder.DecodeFromBase58Check(_config.TronConfig.Address))
            }, headers: wallet.GetHeaders());

            //交易记录
            var transactions = new List<TransactionRecord>();
            var trc10s = await _tronGridService.GetTRC10Transactions(_config.TronConfig.Address);
            var trc20s = await _tronGridService.GetTRC20Transactions(_config.TronConfig.Address);
            transactions.AddRange(trc10s);
            transactions.AddRange(trc20s);
            var record = transactions.OrderByDescending(o => o.TransactionTime).ToList();

            ////质押总额
            //long totalFrozen = 0;
            //foreach (var f in accountInfo.FrozenV2)
            //    totalFrozen += f.Amount;
            //var frozen = TronUnit.SunToTRX(totalFrozen);

            ////已投票
            //long votesCounts = 0;
            //foreach (var c in accountInfo.Votes)
            //    votesCounts += c.VoteCount;

            var text = $"信息\n";
            text += $"地址: {_config.TronConfig.Address}\n";
            text += $"TRX余额: {TronUnit.SunToTRX(accountInfo.Balance)}\n";
            text += $"USDT余额: {resultUsdt}\n\n";

            text += $"🔸资源 -----\n";
            text += $"质押总额: {accountResource.TronPowerLimit} TRX\n";
            text += $"免费带宽: {accountResource.FreeNetLimit - accountResource.FreeNetUsed} / {accountResource.FreeNetLimit}\n";
            text += $"质押带宽: {accountResource.NetLimit - accountResource.NetUsed} / {accountResource.NetLimit}\n";
            text += $"能量: {accountResource.EnergyLimit - accountResource.EnergyUsed} / {accountResource.EnergyLimit}\n";
            text += $"已投票: {accountResource.TronPowerUsed}/{accountResource.TronPowerLimit}\n";
            text += $"待领权益:{TronUnit.SunToTRX(accountReward.Num)}\n";
            text += $"创建时间: {accountInfo.CreateTime.GetMilliTime().AddHours(8)}\n";
            text += $"活跃时间: {accountInfo.LatestOprationTime.GetMilliTime().AddHours(8)}\n\n";

            text += $"🔸最近交易 -----\n";
            text += $"转账笔数: {record.Count}次 (⬇{record.Count(a => a.TransactionType == 1)}| ⬆{record.Count(a => a.TransactionType == 2)})\n";

            var number = 0;
            foreach (var item in record)
            {
                if (item.Symbol == CurrencyEnum.USDT.ToString() || item.Symbol == CurrencyEnum.TRX.ToString())
                {
                    number++;

                    var typeName = item.TransactionType == 1 ? "🟢转入" : "🔴转出";
                    text += $"{item.TransactionTime.GetMilliTime().AddHours(8)} {typeName} <a href='{_config.TronConfig.WebsiteUrl}#/transaction/{item.HashId}'>{item.Amount} {item.Symbol}</a> \n";
                }

                if (number == 10) break;
            }

            text += $"最近交易 -----\n";
            return text;
        }

    }
}
