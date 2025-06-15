using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using social_login_server.Models;

namespace social_login_server.Services;

public class TokenService(IConfiguration cfg, IConnectionMultiplexer redis)
{
    public string CreateAccessToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: cfg["Jwt:Issuer"],
            audience: cfg["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(double.Parse(cfg["Jwt:AccessTokenExpirationMinutes"]!)),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateAppToken(App app, User user)
    {
        var key = Encoding.UTF8.GetBytes(app.ClientSecret);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: cfg["Jwt:Issuer"],
            audience: app.ClientId,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(double.Parse(cfg["Jwt:AccessTokenExpirationMinutes"]!)),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateAndStoreRefreshTokenAsync(int userId)
    {
        var refreshToken = Guid.NewGuid().ToString("N");
        var db = redis.GetDatabase();
        var expiry = TimeSpan.FromDays(
            double.Parse(cfg["Jwt:RefreshTokenExpirationDays"]!));
        await db.StringSetAsync($"refresh:{refreshToken}", userId, expiry);
        return refreshToken;
    }

    public async Task<int?> ValidateRefreshTokenAsync(string token)
    {
        var db = redis.GetDatabase();
        var val = await db.StringGetAsync($"refresh:{token}");
        if (!val.HasValue) return null;
        return (int)val!;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        await redis.GetDatabase().KeyDeleteAsync($"refresh:{token}");
    }
}
