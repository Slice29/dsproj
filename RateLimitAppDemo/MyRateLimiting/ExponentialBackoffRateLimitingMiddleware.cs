using RateLimitAppDemo.MyRateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitAppDemo.MyRateLimiting
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    public class ExponentialBackoffRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExponentialBackoffRateLimiter _rateLimiter;
        private readonly ILogger<ExponentialBackoffRateLimitingMiddleware> _logger;

        public ExponentialBackoffRateLimitingMiddleware(RequestDelegate next, ExponentialBackoffRateLimiter rateLimiter, ILogger<ExponentialBackoffRateLimitingMiddleware> logger)
        {
            _next = next;
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIp(context);
            _logger.LogInformation($"Incoming request from IP: {clientIp}");

            if (!_rateLimiter.IsRequestAllowed(clientIp, out var banEndTime))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync($"Too many attempts. Please try again after {banEndTime?.ToString("o")}.");
                _logger.LogInformation($"Request denied for IP: {clientIp}");
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
                _rateLimiter.RecordAttempt(clientIp, requestSuccessful);
                _logger.LogInformation($"Recorded attempt for IP: {clientIp}. Success: {requestSuccessful}");
            }
        }

        private string GetClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

}