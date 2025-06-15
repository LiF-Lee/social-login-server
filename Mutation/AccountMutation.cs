using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using social_login_server.Data;
using social_login_server.Models;
using social_login_server.Services;
using social_login_server.Models.GraphQL;

namespace social_login_server.Mutation;

public class AccountMutation(
    AppDbContext db,
    TokenService tokens,
    TurnstileValidator turnstile)
{
    public async Task<AuthPayload> RegisterAsync(
        string name,
        string email,
        string password,
        string turnstileToken)
    {
        if (!await turnstile.ValidateAsync(turnstileToken))
        {
            throw new GraphQLException("Invalid turnstile token");
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            throw new GraphQLException("User already exists");
        }

        var newUser = new User
        {
            Name = name,
            Email = email,
            Password = password
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var accessToken = tokens.CreateAccessToken(newUser);
        var refreshToken = await tokens.CreateAndStoreRefreshTokenAsync(newUser.Id);

        return new AuthPayload(accessToken, refreshToken, newUser);
    }

    public async Task<AuthPayload> LoginAsync(
        string email,
        string password,
        string turnstileToken)
    {
        if (!await turnstile.ValidateAsync(turnstileToken))
        {
            throw new GraphQLException("Invalid turnstile token");
        }

        var user = await db.Users
                       .FirstOrDefaultAsync(u => u.Email == email && u.Password == password)
                   ?? throw new GraphQLException("Invalid credentials");

        var accessToken = tokens.CreateAccessToken(user);
        var refreshToken = await tokens.CreateAndStoreRefreshTokenAsync(user.Id);

        return new AuthPayload(accessToken, refreshToken, user);
    }

    public async Task<AuthPayload> RefreshTokenAsync(string refreshToken)
    {
        var userId = await tokens.ValidateRefreshTokenAsync(refreshToken)
                     ?? throw new GraphQLException("Invalid refresh token");

        await tokens.RevokeRefreshTokenAsync(refreshToken);

        var user = await db.Users
                       .FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new GraphQLException("User not found");

        var accessToken = tokens.CreateAccessToken(user);
        var newRefreshToken = await tokens.CreateAndStoreRefreshTokenAsync(userId);

        return new AuthPayload(accessToken, newRefreshToken, user);
    }

    [Authorize]
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        await tokens.RevokeRefreshTokenAsync(refreshToken);
        return true;
    }
}