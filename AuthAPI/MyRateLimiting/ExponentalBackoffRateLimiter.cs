using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.MyRateLimiting
{
    using System.Collections.Concurrent;

    public class ExponentialBackoffRateLimiter
    {
        private readonly ConcurrentDictionary<string, (int attemptCount, DateTime lastAttemptTime, DateTime? banEndTime)> _attempts;
        private readonly int[] _banDurations;
        private readonly int _permitLimit;
        private readonly ILogger<ExponentialBackoffRateLimiter> _logger;

        public ExponentialBackoffRateLimiter(int permitLimit, ILogger<ExponentialBackoffRateLimiter> logger)
        {
            _attempts = new ConcurrentDictionary<string, (int, DateTime, DateTime?)>();
            _banDurations = new[] { 1, 5, 10 }; // Ban durations in minutes for testing
            _permitLimit = permitLimit;
            _logger = logger;
        }

        public bool IsRequestAllowed(string key, out DateTime? banEndTime)
        {
            _logger.LogInformation($"Checking if request is allowed for key: {key}");
            var currentTime = DateTime.UtcNow;

            if (_attempts.TryGetValue(key, out var entry))
            {
                _logger.LogInformation($"Found entry for key: {key}. Attempt count: {entry.attemptCount}, Ban end time: {entry.banEndTime}");
                if (entry.banEndTime.HasValue && entry.banEndTime.Value > currentTime)
                {
                    banEndTime = entry.banEndTime.Value;
                    _logger.LogInformation($"Request denied for key: {key}. Currently banned until: {banEndTime}");
                    return false;
                }

                if (entry.attemptCount >= _permitLimit)
                {
                    var banIndex = Math.Min(entry.attemptCount - _permitLimit, _banDurations.Length - 1);
                    banEndTime = currentTime.AddMinutes(_banDurations[banIndex]);
                    _attempts[key] = (entry.attemptCount, entry.lastAttemptTime, banEndTime);
                    _logger.LogInformation($"Request denied for key: {key}. Exceeded permit limit. New ban end time: {banEndTime}");
                    return false;
                }
            }

            banEndTime = null;
            _logger.LogInformation($"Request allowed for key: {key}");
            return true;
        }

        public void RecordAttempt(string key, bool success)
        {
            var currentTime = DateTime.UtcNow;
            _logger.LogInformation($"Recording attempt for key: {key}. Success: {success}");

            if (success)
            {
                _attempts.TryRemove(key, out _);
                _logger.LogInformation($"Removed key: {key} after successful attempt.");
                return;
            }

            _attempts.AddOrUpdate(key,
                k =>
                {
                    _logger.LogInformation($"Adding new entry for key: {key}");
                    return (1, currentTime, null);
                },
                (k, oldValue) =>
                {
                    var newAttemptCount = oldValue.attemptCount + 1;
                    var banIndex = Math.Min(newAttemptCount - _permitLimit, _banDurations.Length - 1);
                    var banEndTime = newAttemptCount >= _permitLimit ? currentTime.AddMinutes(_banDurations[banIndex]) : (DateTime?)null;
                    _logger.LogInformation($"Updating entry for key: {key}. New attempt count: {newAttemptCount}, Ban end time: {banEndTime}");
                    return (newAttemptCount, currentTime, banEndTime);
                });
        }
    }
}