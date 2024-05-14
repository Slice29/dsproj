using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitAppDemo.MyRateLimiting
{
    using System.Collections.Concurrent;

    public class ExponentialBackoffRateLimiter
    {
        private readonly ConcurrentDictionary<string, (int attemptCount, DateTime lastAttemptTime, DateTime? banEndTime)> _attempts;
        private readonly int[] _banDurations;

        public ExponentialBackoffRateLimiter()
        {
            _attempts = new ConcurrentDictionary<string, (int, DateTime, DateTime?)>();
            _banDurations = new[] { 10, 15, 20 }; // Ban durations in hours: 1 hour, 1 day, 7 days
        }

        public bool IsRequestAllowed(string key, out DateTime? banEndTime)
        {
            var currentTime = DateTime.UtcNow;

            if (_attempts.TryGetValue(key, out var entry))
            {
                if (entry.banEndTime.HasValue && entry.banEndTime.Value > currentTime)
                {
                    banEndTime = entry.banEndTime.Value;
                    return false;
                }
            }

            banEndTime = null;
            return true;
        }

        public void RecordAttempt(string key, bool success)
        {
            var currentTime = DateTime.UtcNow;

            if (success)
            {
                _attempts.TryRemove(key, out _);
                return;
            }

            _attempts.AddOrUpdate(key,
                k => (1, currentTime, null),
                (k, oldValue) =>
                {
                    var newAttemptCount = oldValue.attemptCount + 1;
                    var banIndex = Math.Min(newAttemptCount - 1, _banDurations.Length - 1);
                    var banEndTime = currentTime.AddSeconds(_banDurations[banIndex]);
                    return (newAttemptCount, currentTime, banEndTime);
                });
        }
    }
}