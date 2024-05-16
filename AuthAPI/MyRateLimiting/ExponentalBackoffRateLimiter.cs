using System.Collections.Concurrent;

public class ExponentialBackoffRateLimiter
{
    private readonly ConcurrentDictionary<string, (int attemptCount, DateTime lastAttemptTime, DateTime? banEndTime, int banCount)> _attempts;
    private readonly int[] _banDurations;
    private readonly int _permitLimit;
    private readonly ILogger<ExponentialBackoffRateLimiter> _logger;

    public ExponentialBackoffRateLimiter(int permitLimit, ILogger<ExponentialBackoffRateLimiter> logger)
    {
        _attempts = new ConcurrentDictionary<string, (int, DateTime, DateTime?, int)>();
        _banDurations = new[] { 5, 10, 15 }; // Ban durations in minutes for testing
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

            // Reset attempt count if the user was previously banned but the ban period has ended
            if (entry.banEndTime.HasValue && entry.banEndTime.Value <= currentTime)
            {
                _attempts.TryUpdate(key, (0, currentTime, null, entry.banCount), entry);
                entry = (0, currentTime, null, entry.banCount);
            }

            if (entry.attemptCount >= _permitLimit)
            {
                var banIndex = Math.Min(entry.banCount, _banDurations.Length - 1);
                banEndTime = currentTime.AddSeconds(_banDurations[banIndex]);
                _attempts[key] = (entry.attemptCount, entry.lastAttemptTime, banEndTime, entry.banCount + 1);
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
                return (1, currentTime, null, 0);
            },
            (k, oldValue) =>
            {
                var newAttemptCount = oldValue.attemptCount + 1;
                var banIndex = oldValue.banCount; // Use the current ban count to determine the duration
                var banEndTime = newAttemptCount > _permitLimit ? currentTime.AddSeconds(_banDurations[Math.Min(banIndex, _banDurations.Length - 1)]) : (DateTime?)null;
                _logger.LogInformation($"Updating entry for key: {key}. New attempt count: {newAttemptCount}, Ban end time: {banEndTime}, Ban count: {oldValue.banCount}");
                return (newAttemptCount, currentTime, banEndTime, oldValue.banCount + (newAttemptCount > _permitLimit ? 1 : 0));
            });
    }
}
