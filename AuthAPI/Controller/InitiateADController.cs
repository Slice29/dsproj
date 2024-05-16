using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;

namespace AuthAPI.Controller
{
    [Route("api/initiateadflow")]
    [AllowAnonymous]
    //[EnableCors]
    public class InitiateADFlowController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public InitiateADFlowController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult RedirectToAADLogin()
        {
            Console.WriteLine("Sunt aici vere");
            var resourceId = _configuration["EntraId:ResourceId"];
            var clientId = _configuration["EntraId:ClientId"];
            var tenantId = _configuration["EntraId:TenantId"];
            var redirectUri = _configuration["EntraId:RedirectUri"];
            var scope = "https://graph.microsoft.com/User.Read offline_access openid";
            var aadLoginUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scope)}&response_mode=query";

            Console.WriteLine($"Redirecting to URL: {aadLoginUrl}");
            return Ok(aadLoginUrl); // Return the URL as a plain string
        }
    }
}
