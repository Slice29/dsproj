using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication; // Ensures support for AuthorizationMessageHandler
using Blazored.LocalStorage; // For local storage management
using System.Net.Http;
using System;
using BlazorApp;
using Blazored.Toast;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Register the AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

// Add local storage service for token storage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();
// Register authorization services
builder.Services.AddAuthorizationCore();


// Set up HttpClient for general API access with Authorization capabilities
// Correcting the setup of HttpClient with AuthorizationMessageHandler
builder.Services.AddScoped<AuthorizationMessageHandler>(sp =>
{
    return new AuthorizationMessageHandler(sp.GetRequiredService<IAccessTokenProvider>(), sp.GetRequiredService<NavigationManager>())
        .ConfigureHandler(
            authorizedUrls: new[] { "http://localhost:5122" },
            scopes: new[] { "api-access" }
        );
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    client.BaseAddress = new Uri("http://localhost:5122");
    return client;
});

builder.Services.AddHttpClient("ServerAPI", client => client.BaseAddress = new Uri("http://localhost:5122"))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();


await builder.Build().RunAsync();
