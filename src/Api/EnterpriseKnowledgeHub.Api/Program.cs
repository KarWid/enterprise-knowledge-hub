using System.Text.Json.Serialization;
using EnterpriseKnowledgeHub.Api.Authentication;
using EnterpriseKnowledgeHub.Api.Authorization;
using EnterpriseKnowledgeHub.Api.Middleware;
using EnterpriseKnowledgeHub.Application;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Infrastructure;
using EnterpriseKnowledgeHub.Modules.Identity;
using EnterpriseKnowledgeHub.Modules.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

ConfigureCors(builder);
ConfigureAuthentication(builder);
ConfigureMediator(builder);
ConfigureAuthorization(builder);
ConfigureControllers(builder);
ConfigureModules(builder);
ConfigureOpenApi(builder);
ConfigureRequestContext(builder);

var app = builder.Build();

ConfigureRequestPipeline(app);

app.Run();

static void ConfigureCors(WebApplicationBuilder builder)
{
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
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services
        .AddAuthentication()
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddMemoryCache();
}

static void ConfigureMediator(WebApplicationBuilder builder)
{
    builder.Services.AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.Assemblies = [
            typeof(EnterpriseKnowledgeHubApplicationModule),
            typeof(IdentityModule),
            typeof(OrganizationsModule),
            typeof(KnowledgeModule)
        ];
    });
}

static void ConfigureAuthorization(WebApplicationBuilder builder)
{
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(
            Policies.Documents.Read,
            policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(DocumentAccessRequirement.Read));
        options.AddPolicy(
            Policies.Documents.Upload,
            policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(DocumentAccessRequirement.Upload));
    });
    builder.Services.AddScoped<IAuthorizationHandler, DocumentAccessAuthorizationHandler>();
}

static void ConfigureControllers(WebApplicationBuilder builder)
{
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
}

static void ConfigureModules(WebApplicationBuilder builder)
{
    const string connectionStringName = "EnterpriseKnowledgeHubDbConnectionString";

    builder.Services.AddIdentityModule(builder.Configuration, connectionStringName);
    builder.Services.AddOrganizationsModule(builder.Configuration, connectionStringName);
    builder.Services.AddKnowledgeModule(builder.Configuration, connectionStringName);
    builder.Services.AddEnterpriseKnowledgeHubInfrastructureModule(builder.Configuration);
    builder.Services.AddEnterpriseKnowledgeHubApplicationModule();
}

static void ConfigureOpenApi(WebApplicationBuilder builder)
{
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
}

static void ConfigureRequestContext(WebApplicationBuilder builder)
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
    builder.Services.AddScoped<ICurrentOrganization, CurrentOrganization>();
    builder.Services.Decorate<IUserInfoService, CachedUserInfoService>();
}

static void ConfigureRequestPipeline(WebApplication app)
{
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(app.Environment.IsDevelopment() ? "LocalDevelopment" : "Production");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
}
