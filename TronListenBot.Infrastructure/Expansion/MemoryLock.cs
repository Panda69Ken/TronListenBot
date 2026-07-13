using System.Collections.Concurrent;

namespace TronListenBot.Infrastructure.Expansion
{
    public class MemoryLock
    {
        private class LockEntry
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public string Token { get; set; } = Guid.NewGuid().ToString();
            public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        }

        private readonly ConcurrentDictionary<string, LockEntry> _locks = new();

        /// <summary>
        /// 尝试获取锁（立即返回）
        /// </summary>
        public bool TryAcquire(string key, out string token)
        {
            var entry = _locks.GetOrAdd(key, _ => new LockEntry());

            if (entry.Semaphore.Wait(0))
            {
                token = Guid.NewGuid().ToString();
                entry.Token = token;
                entry.LastUsed = DateTime.UtcNow;
                return true;
            }

            token = null!;
            return false;
        }

        /// <summary>
        /// 等待获取锁（支持超时）
        /// </summary>
        public async Task<string> AcquireAsync(string key, TimeSpan timeout)
        {
            var entry = _locks.GetOrAdd(key, _ => new LockEntry());

            if (await entry.Semaphore.WaitAsync(timeout))
            {
                var token = Guid.NewGuid().ToString();
                entry.Token = token;
                entry.LastUsed = DateTime.UtcNow;
                return token;
            }

            return null;
        }

        /// <summary>
        /// 释放锁（必须提供正确 token）
        /// </summary>
        public bool Release(string key, string token)
        {
            if (!_locks.TryGetValue(key, out var entry))
                return false;

            // 防止误释放
            if (entry.Token != token)
                return false;

            entry.LastUsed = DateTime.UtcNow;
            entry.Semaphore.Release();
            return true;
        }

        /// <summary>
        /// 清理长时间未使用的锁（防止内存泄漏）
        /// </summary>
        public void Cleanup(TimeSpan maxIdle)
        {
            var now = DateTime.UtcNow;

            foreach (var kv in _locks)
            {
                if (now - kv.Value.LastUsed > maxIdle)
                {
                    if (_locks.TryRemove(kv.Key, out var entry))
                    {
                        entry.Semaphore.Dispose();
                    }
                }
            }
        }
    }
}
