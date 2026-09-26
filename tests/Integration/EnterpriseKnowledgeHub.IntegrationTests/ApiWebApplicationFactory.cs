using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace EnterpriseKnowledgeHub.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Supply placeholder AzureAd values so JWT middleware registers without error.
            // Tests for the health endpoint are unauthenticated; no real token is validated.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "test-tenant-id",
                ["AzureAd:ClientId"] = "test-client-id",
                ["AzureAd:Scopes"] = "access_as_user"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // The production health check opens a relational database connection.
            // Use a reachable relational test connection so the test exercises that behavior.
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(new ReachableDbConnection()));
        });
    }

    private sealed class ReachableDbConnection : DbConnection
    {
        private ConnectionState state;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "EnterpriseKnowledgeHub";

        public override string DataSource => "test";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close() => state = ConnectionState.Closed;

        public override void Open() => state = ConnectionState.Open;

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
