using Blazored.LocalStorage;
using Blazored.Toast;
using BlazorUI;
using BlazorUI.Components;
using dotenv.net;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Net.Http;
using System.Text;

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

builder.Services.AddAuthorizationCore();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

var config = builder.Configuration;

bool IsRunningInContainer()
{
    return bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) && inContainer;
}

builder.Services.AddMassTransit(mt =>
{
    var rabbitMqHost = config.GetValue<string>("RabbitMQHost");
    if (IsRunningInContainer()) rabbitMqHost = "rabbitmq";
    var rabbitMqPort = config.GetValue<string>("RabbitMQPort");
    var rabbitUri = new Uri($"rabbitmq://{rabbitMqHost.Trim('/')}:{rabbitMqPort}");
    string redisString = "";
    if (IsRunningInContainer())
    {
        redisString = "redis";
    }
    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitUri);
        cfg.ConfigureEndpoints(context);
    });



    mt.AddRequestClient<PlaceholderPublisher>();
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("admin", "true"));
});



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("ECT_JWT_ISSUER"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ECT_JWT_KEY")))
    };
});



builder.Services.AddScoped<PlaceholderPublisher>();


    // For development, typically use a specific client configuration
    builder.Services.AddScoped(sp =>
    {
        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        client.BaseAddress = new Uri("http://host.docker.internal:8081"); // Adjust as needed
        return client;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//app.UseHttpsRedirection();



app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//app.MapFallbackToPage("/");
app.Run();
