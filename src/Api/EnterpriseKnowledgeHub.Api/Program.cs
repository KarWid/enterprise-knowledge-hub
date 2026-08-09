using System.Security.Claims;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply pending migrations automatically in development.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (IdentityDbContext db) =>
{
    var dbHealthy = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status   = dbHealthy ? "healthy" : "degraded",
        database = dbHealthy ? "healthy" : "unavailable",
    });
});

app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    var id = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue("preferred_username") ?? user.FindFirstValue(ClaimTypes.Email);
    var name = user.FindFirstValue("name");
    return Results.Ok(new { id, email, name });
})
.RequireAuthorization();

app.Run();

public partial class Program { }
