using Blazored.LocalStorage;
using Blazored.Toast;
using BlazorUI.Components;
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
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

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
