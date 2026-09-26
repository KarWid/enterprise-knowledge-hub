using EnterpriseKnowledgeHub.Api.Authentication;
using EnterpriseKnowledgeHub.Api.Middleware;
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
using Mediator;
using Scrutor;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    // The Vite development server is the only allowed browser origin locally.
    options.AddPolicy("LocalDevelopment", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());

    // Azure supplies the Static Web App URL through Cors__AllowedOrigins__0.
    // Keeping the allowed origins in configuration prevents a deployed API from
    // accepting browser calls from arbitrary websites.
    var productionOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    options.AddPolicy("Production", policy =>
    {
        if (productionOrigins.Length > 0)
        {
            policy.WithOrigins(productionOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services
    .AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddMemoryCache();

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Assemblies = [
        typeof(EnterpriseKnowledgeHubApplicationModule),
        typeof(IdentityModule),
        typeof(OrganizationsModule)
    ];
});

builder.Services.AddAuthorization();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddIdentityModule(builder.Configuration, "EnterpriseKnowledgeHubDbConnectionString");
builder.Services.AddOrganizationsModule(builder.Configuration, "EnterpriseKnowledgeHubDbConnectionString");
builder.Services.AddEnterpriseKnowledgeHubApplicationModule();

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
builder.Services.AddScoped<ICurrentOrganization, CurrentOrganization>();
builder.Services.Decorate<IUserInfoService, CachedUserInfoService>();


var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(app.Environment.IsDevelopment() ? "LocalDevelopment" : "Production");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
