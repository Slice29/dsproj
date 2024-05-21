using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using static AuthAPI.Controller.ADAuthenticationController;

namespace AuthAPI.Services
{
    public class AzureAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;

        public AzureAuthenticationService(IConfiguration configuration, HttpClient client)
        {
            _configuration = configuration;
            _client = client;
        }

        public async Task<TokenResult> GetTokenFromCode(string code)
        {
            var clientId = _configuration["EntraId:ClientId"];
            var clientSecret = _configuration["EntraId:Secret"];
            var redirectUri = _configuration["EntraId:RedirectUri"];
            var tenant = "common";

            // Prepare the request content with the required parameters.
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
                ["client_secret"] = clientSecret,
                ["scope"] = "https://graph.microsoft.com/User.Read offline_access openid"
            };

            var requestContent = new FormUrlEncodedContent(tokenRequest);
            var response = await _client.PostAsync($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine("HTTP Response: " + responseContent); // Output the raw response content

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                return new TokenResult
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken
                };
            }
            else
            {
                throw new ApplicationException($"Token request failed: {responseContent}");
            }
        }

        public async Task<TokenResult> GetNewToken(RefreshTokenModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.RefreshToken))
            {
                return null;
            }

            var clientId = _configuration["EntraId:ClientId"];
            var clientSecret = _configuration["EntraId:Secret"];
            var tenant = "common";

            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = model.RefreshToken,
                ["client_secret"] = Uri.EscapeDataString(clientSecret)
            };
            var requestContent = new FormUrlEncodedContent(tokenRequest);
            var response = await _client.PostAsync($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", requestContent);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(jsonContent);
                Debug.WriteLine("New token is: " + tokenResponse.AccessToken);
                return new TokenResult { AccessToken = tokenResponse.AccessToken, RefreshToken = tokenResponse.RefreshToken };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return null;
            }
        }

        public async Task<string> GetUserEmailFromToken(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var userProfile = JsonConvert.DeserializeObject<UserProfile>(jsonContent);
                return userProfile.Mail ?? userProfile.UserPrincipalName;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ApplicationException($"Failed to retrieve user email: {errorContent}");
            }
        }

        #region Helper Classes
        public class TokenResult
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }

        public class UserProfile
        {
            [JsonProperty("mail")]
            public string Mail { get; set; }

            [JsonProperty("userPrincipalName")]
            public string UserPrincipalName { get; set; }
        }
        #endregion
    }
}
