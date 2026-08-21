using EnterpriseKnowledgeHub.Modules.Identity;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services
    .AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddIdentityModule();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

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
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
