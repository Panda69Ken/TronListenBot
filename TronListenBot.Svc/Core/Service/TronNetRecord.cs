using Microsoft.Extensions.Options;
using TronNet;
using TronNet.Contracts;

namespace TronListenBot.Svc.Core.Service
{
    public class TronNetRecord(ITronClient tronClient,
        IContractClientFactory contractClientFactory,
        IOptions<TronNetOptions> options)
    {
        public ITronClient TronClient { get; set; } = tronClient;

        public IContractClientFactory ContractClientFactory { get; set; } = contractClientFactory;
    }
}
