using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using EnterpriseKnowledgeHub.Api.StorageEvents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

internal sealed class StorageWebhookFactory : WebApplicationFactory<Program>
{
    internal const string Tenant = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    internal const string Audience = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    internal const string Sender = "4962773b-9cdb-44cf-a8bf-237846a00ab7";
    internal const string Source = "/subscriptions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/teststore";
    internal const string Role = "AzureEventGridSecureWebhookSubscriber";
    internal const string Issuer = "https://login.microsoftonline.com/" + Tenant + "/v2.0";
    private readonly RSA rsa = RSA.Create(2048);
    internal UploadTestDatabase Database { get; } = new();
    internal bool Enabled { get; init; } = true;
    internal bool SimulateInboxPersistenceFailure { get; init; }
    internal string ContainerName { get; init; } = "documents";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/", 
            ["AzureAd:TenantId"] = Tenant,
            ["AzureAd:ClientId"] = "cccccccc-cccc-cccc-cccc-cccccccccccc", 
            ["AzureAd:Scopes"] = "access_as_user",
            ["StorageEvents:Enabled"] = Enabled.ToString(), 
            ["StorageEvents:TenantId"] = Tenant,
            ["StorageEvents:Audience"] = Audience, 
            ["StorageEvents:SenderApplicationId"] = Sender,
            ["StorageEvents:ExpectedSource"] = Source, 
            ["DocumentUploadWorker:Enabled"] = "false",
            ["BlobStorage:AccountUrl"] = "https://teststore.blob.core.windows.net", 
            ["BlobStorage:ContainerName"] = ContainerName
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<KnowledgeDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<KnowledgeDbContext>>();
            services.AddDbContext<KnowledgeDbContext>(o =>
            {
                if (Database.UseSqlServer)
                {
                    o.UseSqlServer(Database.ConnectionString);
                }
                else
                {
                    o.UseSqlite(Database.ConnectionString);
                }
            });

            if (SimulateInboxPersistenceFailure)
            {
                services.RemoveAll<IUploadInbox>();
                services.AddScoped<IUploadInbox, FailingUploadInbox>();
            }

            // Test-only signing key, not a test authentication handler and not production code.
            services.PostConfigureAll<JwtBearerOptions>(jwt =>
            {
                var oidc = new OpenIdConnectConfiguration { Issuer = Issuer };
                oidc.SigningKeys.Add(new RsaSecurityKey(rsa) { KeyId = "test-signing-key" });
                jwt.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidc);
            });
        });
    }

    internal string Token(
        string? sender = Sender, 
        string? role = Role, 
        string audience = Audience,
        string issuer = Issuer, 
        bool expired = false, 
        bool user = false, 
        bool wrongSignature = false)
    {
        var claims = new List<Claim>();

        if (sender is not null)
        {
            claims.Add(new("azp", sender));
        }

        if (role is not null)
        {
            claims.Add(new("roles", role));
        }

        if (user)
        {
            claims.Add(new("scp", "access_as_user"));
        }

        var rsaSecurityKey = new RsaSecurityKey(wrongSignature ? RSA.Create(2048) : rsa) { KeyId = "test-signing-key" };

        var token = new JwtSecurityToken(
            issuer, 
            audience, 
            claims, 
            DateTime.UtcNow.AddHours(-2),
            expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Database.Dispose();
            rsa.Dispose();
        }
    }

    private sealed class FailingUploadInbox : IUploadInbox
    {
        public Task EnqueueAsync(
            IEnumerable<UploadInboxItem> items,
            CancellationToken cancellationToken)
        {
            return Task.FromException(
                new DbUpdateException("Simulated inbox persistence failure."));
        }

        public Task<UploadInboxItem?> ClaimAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<int> CompleteAsync(
            UploadInboxItem item,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<int> RetryAsync(
            UploadInboxItem item,
            string errorCode,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
