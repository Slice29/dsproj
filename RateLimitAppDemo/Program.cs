using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using RateLimitAppDemo.MyRateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rate Limiting API", Version = "v1" });
});

builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    //options.AddPolicy("fixed", httpContext =>
    //    RateLimitPartition.GetFixedWindowLimiter(
    //        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
    //        factory: partition => new FixedWindowRateLimiterOptions
    //        {
    //            PermitLimit = 10,
    //            Window = TimeSpan.FromSeconds(10)
    //        }));
});

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ExponentialBackoffRateLimiter>>();
    return new ExponentialBackoffRateLimiter(permitLimit: 5, logger);
});
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<ExponentialBackoffRateLimitingMiddleware>>();
    appBuilder.UseMiddleware<ExponentialBackoffRateLimitingMiddleware>(logger);
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
