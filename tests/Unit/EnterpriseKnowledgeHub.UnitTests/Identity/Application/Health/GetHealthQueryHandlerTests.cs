using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using EnterpriseKnowledgeHub.Modules.Identity.Application.Health;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Application.Health;

public class GetHealthQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDatabaseIsReachable_ReturnsHealthyTrue()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(new ReachableDbConnection())
            .Options;

        using var db = new IdentityDbContext(options);
        var handler = new GetHealthQueryHandler(
            db,
            NullLogger<GetHealthQueryHandler>.Instance);

        var result = await handler.Handle(new GetHealthQuery(), CancellationToken.None);

        Assert.True(result.IsHealthy);
    }
    // TODO @KWidla: fix it later
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
