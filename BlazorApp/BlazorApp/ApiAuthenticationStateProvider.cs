using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.IdentityModel.Tokens.Jwt;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public ApiAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Unauthenticated state
        }
        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "Bearer");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private AuthenticationState BuildAuthenticationState(string token)
    {
        var identity = new ClaimsIdentity();
        if (!string.IsNullOrEmpty(token))
        {
            identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        }
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(jwt) as JwtSecurityToken;
        return jsonToken.Claims;
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsStringAsync("authToken", token);
        var authState = BuildAuthenticationState(token);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("authToken");
        var authState = BuildAuthenticationState(null);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }
}
