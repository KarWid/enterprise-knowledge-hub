using EnterpriseKnowledgeHub.Api.Authentication;
using EnterpriseKnowledgeHub.Application;
using EnterpriseKnowledgeHub.BuildingBlocks.Application;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Identity;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using EnterpriseKnowledgeHub.Modules.Organizations;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

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

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddIdentityModule();
builder.Services.AddOrganizationsModule();
builder.Services.AddEnterpriseKnowledgeHubApplicationModule();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EnterpriseKnowledgeHubDbConnectionString")));

builder.Services.AddDbContext<OrganizationsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EnterpriseKnowledgeHubDbConnectionString")));

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

    options.EnableAnnotations();

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IUserContext, UserContext>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    // Apply pending migrations automatically in development.
//    using var scope = app.Services.CreateScope();

//    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
//    if (identityDb.Database.IsRelational())
//        await identityDb.Database.MigrateAsync();

//    var organizationsDb = scope.ServiceProvider.GetRequiredService<OrganizationsDbContext>();
//    if (organizationsDb.Database.IsRelational())
//        await organizationsDb.Database.MigrateAsync();
//}

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
