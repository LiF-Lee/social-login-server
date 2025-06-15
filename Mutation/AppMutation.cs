using HotChocolate.Authorization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using social_login_server.Data;
using social_login_server.Extensions;
using social_login_server.Models;

namespace social_login_server.Mutation;

public class AppMutation(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db)
{
    private int CurrentUserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User.GetUserId();
            if (userId == null)
            {
                throw new GraphQLException("Authentication required");
            }
            return userId.Value;
        }
    }

    [Authorize]
    public async Task<App> CreateAsync(string name, string redirectUri)
    {
        var app = new App
        {
            Name = name,
            RedirectUri = redirectUri,
            OwnerUserId = CurrentUserId,
            ClientId = GenerateClientId(),
            ClientSecret = GenerateClientSecret(),
            Created = DateTime.UtcNow
        };

        db.Apps.Add(app);
        await db.SaveChangesAsync();

        var createdApp = await db.Apps
                             .Include(x => x.OwnerUser)
                             .ThenInclude(u => u.Apps)
                             .FirstOrDefaultAsync(x => x.Id == app.Id)
                         ?? throw new GraphQLException("Failed to load created app");

        return createdApp;
    }

    [Authorize]
    public async Task<App> EditAsync(int appId, string? name, string? redirectUri)
    {
        var app = await db.Apps
                      .Include(x => x.OwnerUser)
                      .ThenInclude(u => u.Apps)
                      .FirstOrDefaultAsync(x => x.Id == appId)
                  ?? throw new GraphQLException("App not found");

        if (app.OwnerUserId != CurrentUserId)
        {
            throw new GraphQLException("Permission denied");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            app.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            app.RedirectUri = redirectUri;
        }

        await db.SaveChangesAsync();
        return app;
    }

    [Authorize]
    public async Task<User> DeleteAsync(int appId)
    {
        var app = await db.Apps
                      .Include(x => x.OwnerUser)
                      .ThenInclude(u => u.Apps)
                      .FirstOrDefaultAsync(x => x.Id == appId)
                  ?? throw new GraphQLException("App not found");

        if (app.OwnerUserId != CurrentUserId)
        {
            throw new GraphQLException("Permission denied");
        }

        db.Apps.Remove(app);
        await db.SaveChangesAsync();

        return app.OwnerUser;
    }

    private static string GenerateClientId()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }

    private static string GenerateClientSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}