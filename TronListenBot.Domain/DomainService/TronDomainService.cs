using Microsoft.Extensions.Logging;
using TronListenBot.Domain.Aggregates;
using TronListenBot.Domain.Repositories;

namespace TronListenBot.Domain.DomainService
{
    public class TronDomainService(ILogger<TronDomainService> logger,
        IFreeSql freeSql,
        Repository<BlockNumber> blockNumberRepository
        )
    {
        public readonly IFreeSql _freeSql = freeSql;
        private readonly Repository<BlockNumber> _blockNumberRepository = blockNumberRepository;

        /// <summary>
        /// 类似redis的StringIncrement函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public async Task<long> StringIncrement(string key, long step = 1)
        {
            using var uow = _freeSql.CreateUnitOfWork();

            var exists = await uow.Orm.Select<BlockNumber>().AnyAsync(a => a.Key == key);

            if (!exists)
            {
                await uow.Orm.Insert(new BlockNumber
                {
                    Key = key,
                    Value = step
                }).ExecuteAffrowsAsync();

                uow.Commit();

                return step;
            }

            await uow.Orm.Update<BlockNumber>()
                .Set(a => a.Value + step)
                .Where(a => a.Key == key)
                .ExecuteAffrowsAsync();

            var value = await uow.Orm.Select<BlockNumber>()
                .Where(a => a.Key == key)
                .FirstAsync(a => a.Value);

            uow.Commit();

            return value;
        }

        public async Task SetTronNowBlock(string key, long block)
        {
            await _blockNumberRepository.UpdateDiy
                .Set(a => a.Value == block)
                .Where(a => a.Key == key)
                .ExecuteAffrowsAsync();
        }

        public async Task InitNodeBlockNumber(string key, string node)
        {
            var k = $"{key}:{node}";

            await _blockNumberRepository.UpdateDiy
                    .Set(a => a.Value == 0)
                    .Where(a => a.Key == k)
                    .ExecuteAffrowsAsync();
        }

        public async Task SetNodeBlockNumber(string key, string node, long number)
        {
            var k = $"{key}:{node}";

            var exists = await _blockNumberRepository.Select.AnyAsync(a => a.Key == k);

            if (!exists)
            {
                await _blockNumberRepository.InsertAsync(new BlockNumber
                {
                    Key = k,
                    Value = number
                });
            }
            else
            {
                await _blockNumberRepository.UpdateDiy
                    .Set(a => a.Value == number)
                    .Where(a => a.Key == k)
                    .ExecuteAffrowsAsync();
            }
        }

    }
}
