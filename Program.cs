using System.Text;
using StackExchange.Redis;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using social_login_server.Data;
using social_login_server.Mutation;
using social_login_server.Query;
using social_login_server.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Database
var mySqlConn = cfg.GetConnectionString("MySql")!;
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(mySqlConn, ServerVersion.AutoDetect(mySqlConn))
);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")!)
);

// Authentication
var key = Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer     = true,
            ValidateAudience   = true,
            ValidIssuer        = cfg["Jwt:Issuer"],
            ValidAudience      = cfg["Jwt:Audience"],
            IssuerSigningKey   = new SymmetricSecurityKey(key),
            ClockSkew          = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(cfg.GetSection("AllowedOrigins").Get<string[]>()!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Application Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccountMutation>();
builder.Services.AddScoped<AppMutation>();
builder.Services.AddScoped<Mutation>();
builder.Services.AddHttpClient<TurnstileValidator>();

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(opts => opts.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

// Ensure database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// GraphQL Playground
app.UsePlayground(new PlaygroundOptions
{
    Path      = "/playground",
    QueryPath = "/graphql"
});

// GraphQL endpoint
app.MapGraphQL();

app.Run();
