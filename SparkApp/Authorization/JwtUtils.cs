namespace WebASparkApppi.Authorization;

using SparkService.Models;
using SparkService.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;



public class JwtUtils
{
    private readonly UsersService _usersService;
    private readonly AppSettings _config;

    public JwtUtils(UsersService usersService, IOptions<AppSettings> config)
    {
        _usersService = usersService;
        _config = config.Value;
    }


    public string GenerateJwtToken(User user, Claim[] additionalClaims = null)
    {
        var claims = new[]
        {
          new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
          new Claim("id", user.Id!.ToString())
        };

        if (additionalClaims is object)
        {
            var claimList = new List<Claim>(claims);
            claimList.AddRange(additionalClaims);
            claims = claimList.ToArray();
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JWTSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_config.JWTTokenValidityInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _config.JWTValidIssuer,
            Audience = _config.JWTValidAudience,
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string? ValidateJwtToken(string token)
    {
        if (token == null)
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JWTSecret);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = _config.JWTValidIssuer,
                ValidAudience = _config.JWTValidAudience,
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "id").Value.ToString();

            // return user id from JWT token if validation successful
            return userId;
        }
        catch (Exception ex)
        {
            // return null if validation fails
            return null;
        }
    }

    public RefreshToken GenerateRefreshToken(User user, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            token = getUniqueToken(),
            // token is valid for 7 days
            expires = DateTime.UtcNow.AddDays(_config.RefreshTokenExpiryTimeDays),
            created_at = DateTime.UtcNow,
            created_by_ip = ipAddress,
            UserId = user.Id
        };

        return refreshToken;

        string getUniqueToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            // ensure token is unique by checking against db
            var tokenIsUnique = !_usersService.IsRefreshTokenUnique(token);

            if (!tokenIsUnique)
                return getUniqueToken();

            return token;
        }
    }
}