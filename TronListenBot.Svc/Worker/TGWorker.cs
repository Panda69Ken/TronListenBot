using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TronListenBot.Svc.Core.MR.Command;
using TronListenBot.Svc.Core.Service;

namespace TronListenBot.Svc.Worker
{
    public class TGWorker(ILogger<TGWorker> logger, 
        IMediator mediator, 
        ITelegramBotClient tgBotClient, 
        IConfigService config) : BackgroundService
    {
        private readonly ILogger<TGWorker> _logger = logger;
        private readonly IMediator _mediator = mediator;
        private readonly ITelegramBotClient _tgBotClient = tgBotClient;
        private readonly IConfigService _config = config;

        string _userName = "";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TGWorker running at: {time}", DateTimeOffset.Now);

            var me = await _tgBotClient.GetMe(cancellationToken: stoppingToken);

            _userName = me.Username ?? "";

            await AddCommand(_tgBotClient, stoppingToken);

            await HandleUpdate(_tgBotClient, stoppingToken);

            await Task.CompletedTask;
        }

        async Task AddCommand(ITelegramBotClient bot, CancellationToken ct)
        {
            _ = bot.DeleteMyCommands(cancellationToken: ct);

            var commands = new List<BotCommand>
            {
                new() { Command = "/me", Description = "我的信息" },
                new() { Command = "/address_info", Description = "地址信息" },
            };

            await bot.SetMyCommands(commands: commands, cancellationToken: ct);
        }

        async Task HandleUpdate(ITelegramBotClient bot, CancellationToken ct)
        {
            int? offset = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var updates = await bot.GetUpdates(offset, timeout: 2, cancellationToken: ct);

                    foreach (var update in updates)
                    {
                        offset = update.Id + 1;

                        switch (update.Type)
                        {
                            case UpdateType.Message:
                            case UpdateType.EditedMessage:
                                var message = update.Message ?? update.EditedMessage;
                                await _mediator.Send(new TgMsgCommand
                                {
                                    Update = update,
                                    UserName = _userName
                                }, ct);
                                break;
                            case UpdateType.CallbackQuery:
                                if (update.CallbackQuery is null) return;
                                HandleButton(bot, update.CallbackQuery);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"获取TG消息异常,TgToken:{_config.TgConfig.TgToken},error:{ex.Message}");
                }

                await Task.Delay(5);
            }
        }

        void HandleButton(ITelegramBotClient bot, CallbackQuery query)
        {

        }

    }
}
