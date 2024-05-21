using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotenv.net;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.JWT
{
    public class JwtValidator
    {
        private readonly JwtSettings _jwtConfig;

        //* Injecting configuration from service.
        //* Maybe bad idea to have security information in services???
        public JwtValidator()
        {
            DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

            _jwtConfig = new JwtSettings
            {
                Key = Environment.GetEnvironmentVariable("ECT_JWT_KEY"),
                Issuer = Environment.GetEnvironmentVariable("ECT_JWT_ISSUER"),
                ExpireDays = int.Parse(Environment.GetEnvironmentVariable("ECT_JWT_EXPIRE_DAYS"))
            };
        }
        public bool ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true
                };


                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return claimsPrincipal?.Identity?.IsAuthenticated ?? false;
            }
            catch // Token validation failed
            {
                return false;
            }
        }
    }

}