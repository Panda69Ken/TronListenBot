using TronListenBot.Domain.Aggregates;

namespace TronListenBot.Domain.QueryServices
{
    public class TronQueries(IFreeSql freeSql)
    {
        private readonly IFreeSql _freeSql = freeSql;


        public async Task<bool> BlockNumberKeyExist(string key)
        {
            return await _freeSql.Select<BlockNumber>().AnyAsync(a => a.Key == key);
        }

        public async Task<long> GetNodeBlockNumber(string key, string node)
        {
            var value = await _freeSql.Select<BlockNumber>()
               .Where(a => a.Key == $"{key}:{node}")
               .FirstAsync(a => a.Value);

            return value;
        }

    }
}
