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
            if (token != null)
                token = RemoveJsonFormatting(token);
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Unauthenticated state
            }

            if (!IsJwt(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Handle invalid JWT
            }

            // Parse the JWT for claims
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "Bearer");

            // Attempt to get and add role claims if they exist
            var userRoles = await _localStorage.GetItemAsStringAsync("userRoles");
            if (!string.IsNullOrEmpty(userRoles))
            {
                var roles = JsonSerializer.Deserialize<List<string>>(userRoles);
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            // Create an authenticated state using the identity which includes the parsed claims
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            // Log the exception, if logging is setup
            // Example: _logger.LogError("Failed to get authentication state: {Exception}", ex);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Return error state
        }
    }

    public async Task<bool> IsUserAdminAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        token = RemoveJsonFormatting(token);
        if (!IsJwt(token))
        {
            return false;
        }

        var claims = ParseClaimsFromJwt(token);
        var adminClaim = claims.FirstOrDefault(c => c.Type == "admin");

        return adminClaim != null && adminClaim.Value == "true";
    }

    private bool IsJwt(string token)
    {
        // Preprocessing to remove unwanted characters
        token = RemoveJsonFormatting(token);

        var parts = token.Split('.');
        if (parts.Length == 3)
        {
            try
            {
                var header = DecodeBase64Url(parts[0]);
                var payload = DecodeBase64Url(parts[1]);
                // Additional structural checks can be placed here if necessary
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    private string RemoveJsonFormatting(string input)
    {
        // Remove all JSON-specific characters and escaped characters that aren't part of a standard JWT
        var output = input
            .Replace("\\u0022", "") // Remove unicode escaped quotes
            .Replace("\\", "")      // Remove backslashes
            .Replace("\"", "")      // Remove quotes
            .Replace("{", "")       // Remove curly braces
            .Replace("}", "")
            .Replace("token:", "")
            .Replace("/", "");      // Remove slashes

        return output;
    }

    private byte[] DecodeBase64Url(string input)
    {
        string output = input;
        output = output.Replace('-', '+'); // 62nd char of encoding
        output = output.Replace('_', '/'); // 63rd char of encoding

        switch (output.Length % 4) // Pad with '=' chars
        {
            case 0: break; // No pad chars in this case
            case 2: output += "=="; break; // Two pad chars
            case 3: output += "="; break; // One pad char
            default: throw new Exception("Illegal base64url string!");
        }

        return Convert.FromBase64String(output); // Standard base64 decoder
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

    public async Task<string> GetUserEmailAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        token = RemoveJsonFormatting(token);
        if (!IsJwt(token))
        {
            return null;
        }

        var claims = ParseClaimsFromJwt(token);
        var emailClaim = claims.FirstOrDefault(c => c.Type == "email");

        return emailClaim?.Value;
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
