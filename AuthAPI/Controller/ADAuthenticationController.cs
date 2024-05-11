using System.Diagnostics;
using System.Net.Http.Headers;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AuthAPI.Controller
{
    [Route("api/authentication")]
    [AllowAnonymous]
    [EnableCors]
    public class ADAuthenticationController : ControllerBase
    {

        private readonly AzureAuthenticationService _authService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ADAuthenticationController(AzureAuthenticationService authService, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _httpClientFactory = httpClientFactory;
        }

        // GET api/authentication/receivetoken
        [HttpGet]
        [Route("receivetoken")]
        public async Task<IActionResult> GetToken([FromQuery] string code)
        {
            Console.WriteLine("Authorization Code Received: " + code);
            var tokenResult = await _authService.GetTokenFromCode(code);
            Console.WriteLine("Token Received: " + tokenResult.AccessToken);
            Console.WriteLine("Refresh Token: " + tokenResult.RefreshToken);
            return Ok(tokenResult.AccessToken);
         //   var vpnStartResult = await SendGetRequestToStartVpn(tokenResult.AccessToken);
           // return vpnStartResult;
        }

        private async Task<IActionResult> SendGetRequestToStartVpn(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5240/api/test/startvpn", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error sending POST request: " + response.StatusCode);
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            else
            {
                Console.WriteLine("POST request sent successfully.");
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(responseContent);
            }
        }


        [HttpPost]
        [Route("refreshtoken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }
            var tokenResult = await _authService.GetNewToken(model);
            Console.WriteLine("Token Received: " + tokenResult.AccessToken);
            Console.WriteLine("Refresh Token: " + tokenResult.RefreshToken);
            return Ok(tokenResult);
        }

        #region Helper Classes
        public class RefreshTokenModel
        {
            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }
        }

        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("scope")]
            public string Scope { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

        }

    }
    #endregion

}