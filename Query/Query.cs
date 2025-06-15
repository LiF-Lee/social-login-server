using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using social_login_server.Data;
using social_login_server.Extensions;
using social_login_server.Models;
using social_login_server.Services;

namespace social_login_server.Query;

public class Query
{
    private int GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User.GetUserId();
        return userId ?? throw new GraphQLException("Authentication required");
    }

    private Task<User?> LoadCurrentUserAsync(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor)
    {
        var userId = GetCurrentUserId(httpContextAccessor);
        return db.Users
            .Include(u => u.Apps)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    [Authorize]
    public Task<User?> MeAsync(
        [Service] AppDbContext db,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        return LoadCurrentUserAsync(db, httpContextAccessor);
    }

    [Authorize]
    public Task<App?> AppAsync(
        string clientId,
        [Service] AppDbContext db)
    {
        return db.Apps
            .Include(a => a.OwnerUser)
            .FirstOrDefaultAsync(a => a.ClientId == clientId);
    }

    [Authorize]
    public async Task<string> AgreeAsync(
        string clientId,
        string turnstileToken,
        [Service] AppDbContext db,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] TokenService tokens,
        [Service] TurnstileValidator turnstile)
    {
        if (!await turnstile.ValidateAsync(turnstileToken))
        {
            throw new GraphQLException("Invalid turnstile token");
        }

        var app = await db.Apps
                      .Include(a => a.OwnerUser)
                      .FirstOrDefaultAsync(a => a.ClientId == clientId)
                  ?? throw new GraphQLException("App not found");

        var user = await LoadCurrentUserAsync(db, httpContextAccessor)
                   ?? throw new GraphQLException("User not found");

        return tokens.CreateAppToken(app, user);
    }
}