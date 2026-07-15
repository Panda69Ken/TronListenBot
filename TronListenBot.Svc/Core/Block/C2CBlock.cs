using System.Threading.Tasks.Dataflow;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Model;
using TronListenBot.Svc.Core.Service;

namespace TronListenBot.Svc.Core.Block
{
    public class C2CModel
    {
        public Message Msg { get; set; }
        public string Command { get; set; }
        public bool isReplyParameters { get; set; }
        /// <summary>
        /// 0.all 1.bank 2.aliPay 3.wxPay 4.QQWallet
        /// </summary>
        public int RotaType { get; set; }
    }

    public class C2CBlock
    {
        readonly ActionBlock<C2CModel> _action;
        readonly ILogger<C2CBlock> _logger;
        readonly IConfigService _config;
        readonly ICacheService _cache;
        readonly ITelegramBotClient _botClient;
        readonly IHttpClientFactory _clientFactory;

        public C2CBlock(ILogger<C2CBlock> logger,
            IConfigService config,
            ICacheService cache,
            ITelegramBotClient botClient,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _botClient = botClient;
            _clientFactory = clientFactory;

            _action = new ActionBlock<C2CModel>(async (item) => { await MarketsBlockHandler(item); });
        }

        public bool Post(C2CModel request)
        {
            return _action.Post(request);
        }

        private async Task MarketsBlockHandler(C2CModel request)
        {
            var userId = request.Msg.Chat.Id;
            var msgId = request.Msg.MessageId;

            var locker = new MemoryLock();
            string keyLock = _cache.SetMarketKeyLock($"{_botClient.BotId}_{userId}_{msgId}");
            var token = await locker.AcquireAsync(keyLock, TimeSpan.FromSeconds(60));

            try
            {
                if (token != null)
                {
                    var text = "";
                    var replyMarkup = new InlineKeyboardMarkup();

                    switch (request.Command)
                    {
                        case "/binance_c2c_buy":
                        case "/binance_c2c_sell":
                            var tradeType = request.Command == "/binance_c2c_buy" ? "BUY" : "SELL";
                            (text, replyMarkup) = await GetBinanceMarket(request, tradeType);
                            break;
                        case "/okx_c2c_sell":
                        case "/okx_c2c_buy":
                            var side = request.Command == "/okx_c2c_sell" ? "sell" : "buy";
                            (text, replyMarkup) = await GetOKXMarket(request, side);
                            break;
                    }

                    if (string.IsNullOrEmpty(text)) return;

                    if (request.isReplyParameters == false)
                    {
                        await _botClient.SendMessage(request.Msg.Chat, text,
                            parseMode: ParseMode.Html,
                            protectContent: false,
                            replyMarkup: replyMarkup,
                            replyParameters: msgId);
                    }
                    else
                    {
                        await _botClient.EditMessageText(request.Msg.Chat, request.Msg.MessageId, text,
                            parseMode: ParseMode.Html,
                            replyMarkup: replyMarkup);
                    }

                    locker.Release(keyLock, token);
                }
            }
            catch (Exception ex)
            {
                locker.Release(keyLock, token);
                _logger.LogError($"执行商家查询异常,MarketsBlock-->request:{request.ToJsonEx()},error:{ex.Message}");
            }
        }

        async Task<(string, InlineKeyboardMarkup)> GetOKXMarket(C2CModel request, string side)
        {
            var httpClient = _clientFactory.CreateTryClient();
            var url = $"{_config.OKXC2CUrl}?quoteCurrency=CNY&baseCurrency=USDT&side={side}&userType=all&t={DateTime.UtcNow.GetTimeStamp()}";
            var result = await httpClient.GetAsync<OKXC2C>(url, new { });
            var text = $"<b>OKX 商家 {side} 实时交易汇率top10</b>\n" + "\n";
            var i = 1;
            var list = new List<OKXC2CInfo>();

            switch (request.RotaType)
            {
                case 0:
                    list = side == "sell" ? [.. result.Data.Sell.Skip(0).Take(10)] 
                        : [.. result.Data.Buy.Skip(0).Take(10)];
                    break;
                case 1:
                    list = side == "sell" ? [.. result.Data.Sell.Where(a => a.PaymentMethods.Contains("bank")).Skip(0).Take(10)]
                        : [.. result.Data.Buy.Where(a => a.PaymentMethods.Contains("bank")).Skip(0).Take(10)];
                    break;
                case 2:
                    list = side == "sell" ? [.. result.Data.Sell.Where(a => a.PaymentMethods.Contains("aliPay")).Skip(0).Take(10)]
                        : [.. result.Data.Buy.Where(a => a.PaymentMethods.Contains("aliPay")).Skip(0).Take(10)];
                    break;
                case 3:
                    list = side == "sell" ? [.. result.Data.Sell.Where(a => a.PaymentMethods.Contains("wxPay")).Skip(0).Take(10)]
                        : [.. result.Data.Buy.Where(a => a.PaymentMethods.Contains("wxPay")).Skip(0).Take(10)];
                    break;
            }

            foreach (var item in list)
            {
                var payments = new List<string>();
                if (item.PaymentMethods.Contains("bank")) payments.Add("银行卡");
                if (item.PaymentMethods.Contains("aliPay")) payments.Add("支付宝");
                if (item.PaymentMethods.Contains("wxPay")) payments.Add("微信");

                var completedRate = Math.Round(item.CompletedRate * 100, 2, MidpointRounding.AwayFromZero).ToString("0.##");

                text += $"<b>{i}) {item.Price}CNY | {item.NickName} | {item.CompletedOrderQuantity} 单 | {completedRate}% 成交率</b>\n";
                text += $"<b>数量:{item.AvailableAmount} USDT | 限额: {item.QuoteMinAmountPerOrder} - {item.QuoteMaxAmountPerOrder} CNY</b>\n";
                text += $"\n";
                i++;
            }

            var callbackData = new { request.Command, request.RotaType }.ToJsonEx();

            var replyMarkup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData($"全部{(request.RotaType==0?"✅":"")}", new { request.Command, RotaType = 0 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"银行卡{(request.RotaType==1?"✅":"")}", new { request.Command, RotaType = 1 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"支付宝{(request.RotaType==2?"✅":"")}", new { request.Command, RotaType = 2 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"微信{(request.RotaType==3?"✅":"")}", new { request.Command, RotaType = 3 }.ToJsonEx()),
            });

            return (text, replyMarkup);
        }

        async Task<(string, InlineKeyboardMarkup)> GetBinanceMarket(C2CModel request, string tradeType)
        {
            var httpClient = _clientFactory.CreateTryClient();

            var payTypes = new List<string>();
            switch (request.RotaType)
            {
                case 1:
                    payTypes.Add("BANK");
                    break;
                case 2:
                    payTypes.Add("ALIPAY");
                    break;
                case 3:
                    payTypes.Add("WECHAT");
                    break;
                case 4:
                    payTypes.Add("QQWallet");
                    break;
            }

            var result = await httpClient.PostAsync<BinanceC2C>(_config.BinanceC2CUrl, new
            {
                fiat = "CNY",
                page = 1,
                rows = 10,
                tradeType,
                asset = "USDT",
                payTypes,
                classifies = new List<string> { "mass", "profession", "fiat_trade" }
            });
            var text = $"<b>Binance 商家 {tradeType} 实时交易汇率top10</b>\n" + "\n";
            var i = 1;

            foreach (var item in result.Data)
            {
                var payments = new List<string>();
                if (item.Adv.TradeMethods.Any(a => a.PayType == "BANK")) payments.Add("银行卡");
                if (item.Adv.TradeMethods.Any(a => a.PayType == "ALIPAY")) payments.Add("支付宝");
                if (item.Adv.TradeMethods.Any(a => a.PayType == "WECHAT")) payments.Add("微信");
                if (item.Adv.TradeMethods.Any(a => a.PayType == "QQWallet")) payments.Add("QQ钱包");

                var monthFinishRate = Math.Round(item.Advertiser.MonthFinishRate * 100, 2, MidpointRounding.AwayFromZero).ToString("0.##");

                text += $"<b>{i}) {item.Adv.Price}CNY | {item.Advertiser.NickName} | {item.Advertiser.MonthOrderCount} 单 | {monthFinishRate}% 成交率</b>\n";
                text += $"<b>数量:{item.Adv.TradableQuantity} USDT | 限额: {item.Adv.MinSingleTransAmount} - {item.Adv.MaxSingleTransAmount} CNY</b>\n";
                text += $"\n";
                i++;
            }

            var replyMarkup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData($"全部{(request.RotaType==0?"✅":"")}", new { request.Command, RotaType = 0 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"银行卡{(request.RotaType==1?"✅":"")}", new { request.Command, RotaType = 1 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"支付宝{(request.RotaType==2?"✅":"")}", new { request.Command, RotaType = 2 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"微信{(request.RotaType==3?"✅":"")}", new { request.Command, RotaType = 3 }.ToJsonEx()),
                InlineKeyboardButton.WithCallbackData($"QQ钱包{(request.RotaType==4?"✅":"")}", new { request.Command, RotaType = 4 }.ToJsonEx()),
            });

            return (text, replyMarkup);
        }

    }
}
