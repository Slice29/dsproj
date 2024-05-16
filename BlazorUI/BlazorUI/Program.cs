using Blazored.LocalStorage;
using Blazored.Toast;
using BlazorUI;
using BlazorUI.Components;
using MassTransit;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
        cfg.ConfigureSend(x => x.UseExecute(context => { context.Headers.Set("auth", "1234"); } ));
        cfg.ConfigurePublish(x => x.UseExecute(context => { context.Headers.Set("auth", "1234"); }));
        cfg.ConfigureEndpoints(context);
    });



    mt.AddRequestClient<PlaceholderPublisher>();
});


builder.Services.AddScoped<PlaceholderPublisher>();

// Set up HttpClient for different environments
if (builder.Environment.IsDevelopment())
{
    // For development, typically use a specific client configuration
    builder.Services.AddScoped(sp =>
    {
        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        client.BaseAddress = new Uri("http://localhost:5122"); // Adjust as needed
        return client;
    });
}
else
{
    // For production, you might point to a different API or have other configurations
    builder.Services.AddScoped(sp =>
    {
        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        client.BaseAddress = new Uri("https://api.productionurl.com"); // Adjust as needed
        return client;
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
