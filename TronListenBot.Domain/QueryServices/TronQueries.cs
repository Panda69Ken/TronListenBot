using TronListenBot.Domain.Aggregates;
using TronListenBot.Infrastructure.Expansion;

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

        public async Task<List<TransactionRecord>> GetTransactionRecord()
        {
            var dateTime = DateTime.UtcNow.GetTimeStamp();
            return await _freeSql.Select<TransactionRecord>()
                .Where(a => a.Status == Infrastructure.Enums.TransactionStatusEnum.Undetermined)
                .Where(a => (dateTime - a.TransactionTime) <= 3600)
                .OrderBy(o => o.Id)
                .ToListAsync();
        }

    }
}
