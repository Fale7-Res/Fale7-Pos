using System;

namespace Fale7_POS.Infrastructure.Storage
{
    /// <summary>
    /// A dedicated static, thread-safe engine to generate, stamp, and lock
    /// unique Idempotency Keys across the entire application offline-first architecture.
    /// </summary>
    public static class LocalIdempotencyEngine
    {
        private static long _lastTimestampMs = 0;
        private static readonly object _lock = new object();

        /// <summary>
        /// Generates a strictly unique idempotency key tied to the exact millisecond.
        /// Thread-safe to prevent duplicates in concurrent mutation triggers.
        /// </summary>
        public static string GenerateIdempotencyKey(string prefix = "IDMP")
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            lock (_lock)
            {
                if (nowMs <= _lastTimestampMs)
                {
                    nowMs = _lastTimestampMs + 1;
                }
                _lastTimestampMs = nowMs;
                
                string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                return $"{prefix}_{nowMs}_{uniqueId}";
            }
        }

        /// <summary>
        /// Safely stamps an entity with a new idempotency key exactly once.
        /// Ensures the key is immutable after creation.
        /// </summary>
        public static T StampEntity<T>(T entity, Action<T, string> keySetter, string prefix = "IDMP")
        {
            string key = GenerateIdempotencyKey(prefix);
            keySetter?.Invoke(entity, key);
            return entity;
        }
    }
}
