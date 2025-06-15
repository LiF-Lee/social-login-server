using social_login_server.Models;
using Microsoft.EntityFrameworkCore;

namespace social_login_server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<App> Apps => Set<App>();
}