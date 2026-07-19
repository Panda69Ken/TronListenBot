using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.MR.Command;
using TronListenBot.Svc.Core.Service;
using TronNet;
using TronNet.Protocol;
using static TronNet.Protocol.Transaction.Types.Contract.Types;

namespace TronListenBot.Svc.Core.MR.Transfer
{
    public class TransferNotifyHandler(ILogger<TransferNotifyHandler> logger,
        IConfigService config,
        ITelegramBotClient botClient,
        ITronGridClient tronGridClient,
        TronNetRecord tron) : IRequestHandler<TransferCommand>
    {
        readonly ILogger<TransferNotifyHandler> _logger = logger;
        readonly IConfigService _config = config;
        readonly ITelegramBotClient _botClient = botClient;
        readonly ITronGridClient _tronGridService = tronGridClient;
        readonly TronNetRecord _tron = tron;

        public async Task Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var transaction = request.Transaction;
            var type = transaction.Transaction.RawData.Contract[0].Type;

            var unType = new List<ContractType> {
                ContractType.WithdrawBalanceContract,
                ContractType.CancelAllUnfreezeV2Contract,
                ContractType.WithdrawExpireUnfreezeContract
            };

            if (request.Parameter.Amount == 0 && unType.Contains(type) == false) return;

            if (request.ParamJson.IndexOf(_config.TronConfig.Address) > -1)
            {
                var wallet = _tron.TronClient.GetWallet();

                try
                {
                    var timeDate = DateTime.UtcNow;

                    var time = transaction.Transaction.RawData.Timestamp;

                    if (time > 0 && Utils.IsPlausibleUnixMilliseconds(time))
                    {
                        timeDate = time.GetMilliTime();
                    }
                    timeDate = timeDate.AddHours(8);

                    var text = "";
                    var relatedAddress = request.Parameter.FromAddress == _config.TronConfig.Address ? request.Parameter.ToAddress : request.Parameter.FromAddress;
                    var symbol = request.Parameter.Symbol;

                    var account = await _tronGridService.GetAccountv2(_config.TronConfig.Address);
                    if (account == null)
                    {
                        _logger.LogWarning($"没查到数据，可能查询超时！");
                        return;
                    }

                    switch (type)
                    {
                        case ContractType.TransferContract:
                        case ContractType.TriggerSmartContract:
                            var resultUsdt = account.WithPriceTokens.FirstOrDefault(a => a.TokenAbbr == CurrencyEnum.USDT.ToString());

                            var transactionType = request.Parameter.FromAddress == _config.TronConfig.Address ? "转出" : "转入";
                            var sym = transactionType == "转出" ? "-" : "";

                            text = $"📣余额变化 {sym}{TronUnit.SunToTRX(request.Parameter.Amount)} {symbol}\n";
                            text += $"⏰交易时间: {timeDate}\n";
                            text += $"💰监听地址: <code>{_config.TronConfig.Address}</code>\n";
                            text += $"💰关联地址: <code>{relatedAddress}</code>\n";
                            text += $"🟢交易类型:  {transactionType}\n";
                            text += $"💵交易金额: {TronUnit.SunToTRX(request.Parameter.Amount)} {symbol}\n";
                            text += $"💵TRX余额: {TronUnit.SunToTRX(account.Balance)} TRX\n";
                            text += $"USDT余额 {TronUnit.SunToTRX(resultUsdt != null ? resultUsdt.Balance : 0)} USDT\n";
                            break;

                        case ContractType.FreezeBalanceV2Contract:
                        case ContractType.UnfreezeBalanceV2Contract:
                            var desFreeze = "质押";
                            var desFreeze1 = "获取";
                            var desFreeze2 = "能量";

                            if (request.Parameter.Type == TransactionType.Bandwidth || request.Parameter.Type == TransactionType.UnBandwidth)
                                desFreeze2 = "带宽";

                            if (request.Parameter.Type == TransactionType.UnEnergy || request.Parameter.Type == TransactionType.UnBandwidth)
                            {
                                desFreeze = "解除";
                                desFreeze1 = "失去";
                            }

                            text = $"📣{desFreeze}(2.0) {TronUnit.SunToTRX(request.Parameter.Amount)} {symbol},{desFreeze1}{desFreeze2}&投票权\n";
                            text += $"⏰交易时间: {timeDate}\n";
                            text += $"💰监听地址: <code>{_config.TronConfig.Address}</code>\n";
                            text += $"💰关联地址: <code>{relatedAddress}</code>\n";
                            text += $"🟢交易类型:  {desFreeze}{symbol}\n";
                            text += $"💵质押金额: {TronUnit.SunToTRX(request.Parameter.Amount)} {symbol}\n";
                            text += $"💵质押总额: {TronUnit.SunToTRX(account.TotalFrozenV2)} TRX\n";
                            text += $"💵免费带宽: {account.Bandwidth.FreeNetRemaining} / {account.Bandwidth.FreeNetLimit}\n";
                            text += $"💵质押带宽: {account.Bandwidth.NetRemaining} / {account.Bandwidth.NetLimit}\n";
                            text += $"💵能量: {account.Bandwidth.EnergyRemaining} / {account.Bandwidth.EnergyLimit}\n";
                            text += $"💵已投票: {TronUnit.SunToTRX(account.FrozenForEnergyV2 + account.FrozenForBandWidthV2)}/{account.VoteTotal}\n";
                            text += $"💵待领权益:{TronUnit.SunToTRX(account.RewardNum)} TRX\n";
                            break;

                        case ContractType.WithdrawBalanceContract:
                        case ContractType.CancelAllUnfreezeV2Contract:
                        case ContractType.WithdrawExpireUnfreezeContract:
                            var transactionInfo = await wallet.GetSolidityProtocol().GetTransactionInfoByIdAsync(new BytesMessage
                            {
                                Value = wallet.ParseAddress(request.Txid)
                            });
                            var typeName = "";
                            var text1 = "";

                            if (type == ContractType.WithdrawBalanceContract)
                            {
                                text = $"📣领取 {TronUnit.SunToTRX(transactionInfo.WithdrawAmount)} {symbol} 投票/出块奖励\n";
                                typeName = "领取投票/出块奖励";
                            }
                            if (type == ContractType.CancelAllUnfreezeV2Contract)
                            {
                                long totalFrozen = 0;
                                foreach (var f in transactionInfo.CancelUnfreezeV2Amount)
                                    totalFrozen += f.Value;

                                text = $"📣取消解锁 {TronUnit.SunToTRX(totalFrozen)} {symbol}, 重新获得资源&投票权\n";
                                typeName = "取消解锁";

                                text1 += $"💵质押总额: {TronUnit.SunToTRX(account.TotalFrozenV2)} TRX\n";
                                text1 += $"💵免费带宽: {account.Bandwidth.FreeNetRemaining} / {account.Bandwidth.FreeNetLimit}\n";
                                text1 += $"💵质押带宽: {account.Bandwidth.NetRemaining} / {account.Bandwidth.NetLimit}\n";
                                text1 += $"💵能量: {account.Bandwidth.EnergyRemaining} / {account.Bandwidth.EnergyLimit}\n";
                                text1 += $"💵已投票: {TronUnit.SunToTRX(account.FrozenForEnergyV2 + account.FrozenForBandWidthV2)}/{account.VoteTotal}\n";
                            }
                            if (type == ContractType.WithdrawExpireUnfreezeContract)
                            {
                                text = $"📣提取 {TronUnit.SunToTRX(transactionInfo.WithdrawExpireAmount)} {symbol} 质押本金\n";
                                typeName = "提取质押本金";
                            }

                            text += $"⏰交易时间: {timeDate}\n";
                            text += $"💰监听地址: <code>{_config.TronConfig.Address}</code>\n";
                            text += $"🟢交易类型:  {typeName}\n";
                            text += text1;
                            text += $"💵TRX余额: {TronUnit.SunToTRX(account.Balance)} TRX\n";
                            break;

                        case ContractType.DelegateResourceContract:
                        case ContractType.UnDelegateResourceContract:
                            var desDelegate1 = "代理";
                            var desDelegate2 = "能量";
                            var desDelegate3 = "";
                            var delegateValue = 0M;

                            if (request.Parameter.Type == TransactionType.UnDelegateEnergy || request.Parameter.Type == TransactionType.UnDelegateBandwidth)
                            {
                                desDelegate1 = "回收";
                                desDelegate3 = "解除";
                            }
                            if (request.Parameter.Type == TransactionType.DelegateEnergy || request.Parameter.Type == TransactionType.UnDelegateEnergy)
                            {
                                delegateValue = TronUnit.SunToTRX(request.Parameter.Amount) / account.Bandwidth.TotalEnergyWeight * account.Bandwidth.TotalEnergyLimit;
                            }
                            if (request.Parameter.Type == TransactionType.DelegateBandwidth || request.Parameter.Type == TransactionType.UnDelegateBandwidth)
                            {
                                delegateValue = TronUnit.SunToTRX(request.Parameter.Amount) / account.Bandwidth.TotalNetWeight * account.Bandwidth.TotalNetLimit;
                                desDelegate2 = "带宽";
                            }

                            text = $"📣{desDelegate1} {Math.Ceiling(delegateValue)} {desDelegate2}\n";
                            text += $"⏰交易时间: {timeDate}\n";
                            text += $"💰监听地址: <code>{_config.TronConfig.Address}</code>\n";
                            text += $"💰关联地址: <code>{relatedAddress}</code>\n";
                            text += $"🟢交易类型:  {desDelegate1}{desDelegate2}\n";
                            text += $"💵{desDelegate3}占用质押数量: {TronUnit.SunToTRX(request.Parameter.Amount)} TRX\n";
                            text += $"💵免费带宽: {account.Bandwidth.FreeNetRemaining} / {account.Bandwidth.FreeNetLimit}\n";
                            text += $"💵质押带宽: {account.Bandwidth.NetRemaining} / {account.Bandwidth.NetLimit}\n";
                            text += $"💵能量: {account.Bandwidth.EnergyRemaining} / {account.Bandwidth.EnergyLimit}\n";
                            break;
                    }

                    var btnVerify = InlineKeyboardButton.WithUrl("Tron官网验证", $"{_config.TronConfig.WebsiteUrl}#/transaction/{request.Txid}");
                    var markup = new InlineKeyboardMarkup(btnVerify);

                    await _botClient.SendMessage(_config.TgConfig.TgUserId, text, parseMode: ParseMode.Html, protectContent: false, replyMarkup: markup);

                }
                catch (Exception ex)
                {
                    _logger.LogError($"交易通知异常,node:{request.Node},param:{request.ParamJson},error:{ex.Message}");
                }
            }
        }

    }
}
