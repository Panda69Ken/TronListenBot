using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Block;
using TronListenBot.Svc.Core.Model;
using TronListenBot.Svc.Core.MR.Command;
using TronListenBot.Svc.Core.Service;
using TronNet;

namespace TronListenBot.Svc.Core.MR
{
    public class MsgHandler(ILogger<MsgHandler> logger,
        IConfigService config,
        ICacheService cache,
        ITelegramBotClient botClient,
        ITronGridClient tronGridClient,
        C2CBlock marketsBlock
        ) : IRequestHandler<TgMsgCommand>
    {
        readonly ILogger<MsgHandler> _logger = logger;
        readonly IConfigService _config = config;
        readonly ICacheService _cache = cache;
        readonly ITelegramBotClient _botClient = botClient;
        readonly ITronGridClient _tronGridService = tronGridClient;
        readonly C2CBlock _marketsBlock = marketsBlock;

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
                case "/binance_c2c_sell":
                case "/binance_c2c_buy":
                case "/okx_c2c_sell":
                case "/okx_c2c_buy":
                    _marketsBlock.Post(new C2CModel { Msg = request.Update.Message, Command = command });
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
            var account = await _tronGridService.GetAccountv2(_config.TronConfig.Address);

            if (account == null) return $"地址:'{_config.TronConfig.Address}'没查到数据！";

            //交易记录
            var transactions = new List<TransactionRecord>();
            var trc10s = await _tronGridService.GetTRC10Transactions(_config.TronConfig.Address, 200);
            var trc20s = await _tronGridService.GetTRC20Transactions(_config.TronConfig.Address, 200);
            transactions.AddRange(trc10s);
            transactions.AddRange(trc20s);
            var record = transactions.OrderByDescending(o => o.TransactionTime).ToList();

            var text = $"信息\n";
            text += $"地址: {_config.TronConfig.Address}\n";
            text += $"TRX余额: {TronUnit.SunToTRX(account.Balance)}\n";
            var resultUsdt = account.WithPriceTokens.FirstOrDefault(a => a.TokenAbbr == CurrencyEnum.USDT.ToString());
            text += $"USDT余额: {TronUnit.SunToTRX(resultUsdt != null ? resultUsdt.Balance : 0)}\n\n";

            text += $"🔸资源 -----\n";
            text += $"质押总额: {TronUnit.SunToTRX(account.TotalFrozenV2)} TRX\n";
            text += $"免费带宽: {account.Bandwidth.FreeNetLimit - account.Bandwidth.FreeNetUsed} / {account.Bandwidth.FreeNetLimit}\n";
            text += $"质押带宽: {account.Bandwidth.NetLimit - account.Bandwidth.NetUsed} / {account.Bandwidth.NetLimit}\n";
            text += $"能量: {account.Bandwidth.EnergyLimit - account.Bandwidth.EnergyUsed} / {account.Bandwidth.EnergyLimit}\n";
            text += $"已投票: {TronUnit.SunToTRX(account.FrozenForEnergyV2 + account.FrozenForBandWidthV2)}/{account.VoteTotal}\n";
            text += $"待领权益:{TronUnit.SunToTRX(account.RewardNum)} TRX\n";
            text += $"创建时间: {account.Date_created.GetMilliTime().AddHours(8)}\n";
            text += $"活跃时间: {account.Latest_operation_time.GetMilliTime().AddHours(8)}\n\n";

            text += $"🔸最近交易 -----\n";
            text += $"交易笔数: {account.TotalTransactionCount}\n";
            text += $"转账笔数: {account.Transactions}次 (⬇{account.Transactions_In}| ⬆{account.Transactions_Out})\n";

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
