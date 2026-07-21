using Microsoft.Extensions.Options;
using System.Net;
using Telegram.Bot;
using TronListenBot.Domain.DomainService;
using TronListenBot.Domain.QueryServices;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Block;
using TronListenBot.Svc.Core.Model;
using TronListenBot.Svc.Core.Service;
using TronListenBot.Svc.Worker;
using TronNet;
using TronNet.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.RegisterServices();

builder.Services.AddScoped<TronDomainService>();

builder.Services.AddTronNet(x =>
{
    var tronConfig = builder.Configuration.GetSection("TronConfig").Get<TronConfig>() ?? throw new InvalidOperationException("TronConfig 配置节未找到或配置无效。");

    x.Network = TronNetwork.MainNet;
    x.Channel = new GrpcChannelOption { Host = tronConfig.GrpcUrl, Port = tronConfig.Port };
    x.SolidityChannel = new GrpcChannelOption { Host = tronConfig.GrpcUrl, Port = tronConfig.SolidityPort };
    x.ApiKeys = tronConfig.ApiKeys;
});
builder.Services.AddSingleton(services =>
{
    var tronclient = services.GetService<ITronClient>();
    var contractClientFactory = services.GetService<IContractClientFactory>();
    var options = services.GetService<IOptions<TronNetOptions>>();
    return new TronNetRecord(tronclient, contractClientFactory, options);
});

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var tgConfig = builder.Configuration.GetSection("TgConfig").Get<TgConfig>() ?? throw new InvalidOperationException("TgConfig 配置节未找到或配置无效。");

    if (tgConfig.Proxy)
    {
        HttpClient socks5Client = new(new SocketsHttpHandler
        {
            Proxy = new WebProxy(tgConfig.ProxyDomain)
            {
                Credentials = new NetworkCredential(tgConfig.ProxyUsername, tgConfig.ProxyPassword)
            },
            UseProxy = true,
        });

        return new TelegramBotClient(tgConfig.TgToken, socks5Client);
    }

    return new TelegramBotClient(tgConfig.TgToken);

});

builder.Services.AddSingleton<TronQueries>();
builder.Services.AddSingleton<ITronApiClient, TronApiClient>();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<ICacheService, CacheService>();

builder.Services.AddSingleton<TransferBlock>();
builder.Services.AddSingleton<C2CBlock>();

builder.Services.AddHostedService<TGWorker>();
builder.Services.AddHostedService<ScanBlockWorker>();

var host = builder.Build();
host.Run();
