using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthAPI.Models;
using dotenv.net;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.JWT
{
    public class JwtHelper
    {
        private readonly TokenService _tokenService;
        private readonly UserManager<User> _userManager;

        public JwtHelper(TokenService tokenService, UserManager<User> userManager)
        {
            DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));
            _tokenService = tokenService;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(User user)
        {
            var claims = new List<Claim>
    {
       new Claim("id", user.Id.ToString()), // Simplified from NameIdentifier
        new Claim("email", user.Email)
        // Add other claims as needed
    };

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                claims.Add(new Claim("admin", "true"));
            }
            if (await _userManager.IsInRoleAsync(user, "PromoUser"))
            {
                claims.Add(new Claim("promouser", "true"));
            }
            var token = _tokenService.GenerateToken(claims);

            return token;
        }
    }
}