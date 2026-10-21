using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseKnowledgeHub.Api.StorageEvents;

public static class StorageEventAuthentication
{
    public const string Scheme = "StorageEventMachine";
    public const string Policy = "StorageEvents.Receive";

    public static IServiceCollection AddStorageEventWebhook(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageEventOptions>().Bind(configuration.GetSection(StorageEventOptions.SectionName))
            .Validate(IsValidConfiguration,
                "Enabled StorageEvents requires an explicit tenant, audience, sender, role, and storage source.")
            .ValidateOnStart();

        // Explicit default keeps the existing browser scheme authoritative for user endpoints.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(Scheme, _ => { });
        services.AddOptions<JwtBearerOptions>(Scheme).Configure<IOptions<StorageEventOptions>>((jwt, configured) =>
        {
            var settings = configured.Value;
            jwt.MapInboundClaims = false;
            jwt.RequireHttpsMetadata = true;
            if (settings.Enabled)
            {
                jwt.Authority = $"https://login.microsoftonline.com/{settings.TenantId}/v2.0";
            }

            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuers = [
                    $"https://sts.windows.net/{settings.TenantId}/", 
                    $"https://login.microsoftonline.com/{settings.TenantId}/v2.0"
                ],
                ValidateAudience = true, 
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(60), RoleClaimType = "roles"
            };

            jwt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (!settings.Enabled) context.Fail("Storage events are disabled.");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options => options.AddPolicy(Policy, policy =>
        {
            policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                var settings = configuration.GetSection(StorageEventOptions.SectionName).Get<StorageEventOptions>() ?? new();
                var sender = context.User.FindFirst("azp")?.Value ?? context.User.FindFirst("appid")?.Value;
                
                return settings.Enabled && !context.User.HasClaim(c => c.Type == "scp") &&
                    string.Equals(sender, settings.SenderApplicationId, StringComparison.OrdinalIgnoreCase) &&
                    context.User.HasClaim("roles", settings.RequiredRole);
            });
        }));

        services.AddScoped<StorageEventParser>();

        return services;
    }

    private static bool IsValidConfiguration(StorageEventOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        return Guid.TryParse(options.TenantId, out _) &&
            Guid.TryParse(options.Audience, out _) &&
            Guid.TryParse(options.SenderApplicationId, out _) &&
            !string.IsNullOrWhiteSpace(options.RequiredRole) &&
            options.ExpectedSource.Length <= 512 &&
            options.ExpectedSource.StartsWith("/subscriptions/", StringComparison.OrdinalIgnoreCase) &&
            options.ExpectedSource.Contains(
                "/providers/Microsoft.Storage/storageAccounts/",
                StringComparison.OrdinalIgnoreCase) &&
            options.MaximumBatchSize is >= 1 and <= 100;
    }
}
