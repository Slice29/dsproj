using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using dotenv.net;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.JWT
{
   public class TokenService
{
    //* Token generation class

    private readonly JwtSettings _jwtConfig;

    public TokenService()
    {
        DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

        _jwtConfig = new JwtSettings
        {
            Key = Environment.GetEnvironmentVariable("ECT_JWT_KEY"),
            Issuer = Environment.GetEnvironmentVariable("ECT_JWT_ISSUER"),
            ExpireDays = int.Parse(Environment.GetEnvironmentVariable("ECT_JWT_EXPIRE_DAYS") ?? "7")
        };
    }

    public string GenerateToken(IEnumerable<Claim> claims, double? expireDays = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        Console.WriteLine("IA UITE AICI " + _jwtConfig.Key);
        var keyString = Environment.GetEnvironmentVariable("ECT_JWT_KEY");
        Console.WriteLine($"Key used for signing: {keyString}");  // For debugging only; remove in production!
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(expireDays ?? _jwtConfig.ExpireDays);

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
}