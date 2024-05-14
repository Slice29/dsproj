using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitAppDemo.MyRateLimiting
{
    public class ExponentialBackoffRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExponentialBackoffRateLimiter _rateLimiter;

        public ExponentialBackoffRateLimitingMiddleware(RequestDelegate next, ExponentialBackoffRateLimiter rateLimiter)
        {
            _next = next;
            _rateLimiter = rateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientKey = GetClientKey(context);

            if (!_rateLimiter.IsRequestAllowed(clientKey, out var banEndTime))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync($"Too many attempts. Please try again after {banEndTime?.ToString("o")}.");
                return;
            }

            var requestSuccessful = true;

            try
            {
                await _next(context);
            }
            catch
            {
                requestSuccessful = false;
                throw;
            }
            finally
            {
                _rateLimiter.RecordAttempt(clientKey, requestSuccessful);
            }
        }

        private string GetClientKey(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}