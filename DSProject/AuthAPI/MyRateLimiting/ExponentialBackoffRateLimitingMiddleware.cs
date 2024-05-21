namespace AuthAPI.MyRateLimiting
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.IO;
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
                var lockoutInfo = new
                {
                    Message = "Too many attempts. Please try again later.",
                    BanEndTime = banEndTime?.ToString("o") // ISO 8601 format
                };
                await context.Response.WriteAsJsonAsync(lockoutInfo);
                _logger.LogInformation($"Request denied for IP: {clientIp}");
                return;
            }

            var originalResponseBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var requestSuccessful = true;

            try
            {
                await _next(context);

                // Determine if the request was successful based on the status code
                if (context.Response.StatusCode >= 400)
                {
                    requestSuccessful = false;
                }
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

                // Copy the contents of the new memory stream (which contains the response) to the original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalResponseBodyStream);
            }
        }

        private string GetClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
