using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;

using System.Threading.Tasks;

namespace RateLimitAppDemo.MyRateLimiting
{

    public class CustomRateLimiter : RateLimiter
    {
        private readonly ExponentialBackoffRateLimiter _rateLimiter;
        private readonly string _clientKey;

        public override TimeSpan? IdleDuration => throw new NotImplementedException();

        public CustomRateLimiter(ExponentialBackoffRateLimiter rateLimiter, string clientKey)
        {
            _rateLimiter = rateLimiter;
            _clientKey = clientKey;
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            var lease = _rateLimiter.IsRequestAllowed(_clientKey, out var banEndTime)
                ? new CustomRateLimitLease(true, new Dictionary<string, object>())
                : new CustomRateLimitLease(false, new Dictionary<string, object> { { "BanEndTime", banEndTime } });

            return lease;
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            var lease = _rateLimiter.IsRequestAllowed(_clientKey, out var banEndTime)
                ? new CustomRateLimitLease(true, new Dictionary<string, object>())
                : new CustomRateLimitLease(false, new Dictionary<string, object> { { "BanEndTime", banEndTime } });

            return new ValueTask<RateLimitLease>(lease);
        }

        public override RateLimiterStatistics? GetStatistics()
        {
            return null;
        }

        protected TimeSpan? GetRetryAfter()
        {
            return null;
        }
    }
}