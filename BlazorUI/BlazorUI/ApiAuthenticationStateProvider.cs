using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop; // Import the JS interop namespace


public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime; // Inject the JSRuntime

    public ApiAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Unauthenticated state
                
            }

            if (!IsJwt(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Handle 2FA state or invalid JWT
            }

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "Bearer");
            var userRoles = await _localStorage.GetItemAsStringAsync("userRoles");
            if (!string.IsNullOrEmpty(userRoles))
            {
                var roles = JsonSerializer.Deserialize<List<string>>(userRoles);
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Error state
        }
    }
    private bool IsJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length == 3)
        {
            try
            {
                var header = Convert.FromBase64String(parts[0]);
                var payload = Convert.FromBase64String(parts[1]);
                // further checks could be added here to validate structure
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
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
