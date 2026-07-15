using Microsoft.Extensions.Caching.Memory;

namespace TronListenBot.Svc.Core.Service
{
    public interface ICacheService
    {
        string CreateKey(params object[] args);

        string SetChatDetaileLock(string token);

        string SetMarketKeyLock(string token);
    }

    public class CacheService(IMemoryCache memory) : ICacheService
    {
        private readonly IMemoryCache _memory = memory;

        public string CreateKey(params object[] args)
        {
            return $"tron_svc:{string.Join(".", args)}".ToLower();
        }

        public string SetChatDetaileLock(string token)
        {
            return CreateKey($"chatDetaile_lock:{token}");
        }

        public string SetMarketKeyLock(string token)
        {
            return CreateKey($"market:{token}");
        }
    }

}
