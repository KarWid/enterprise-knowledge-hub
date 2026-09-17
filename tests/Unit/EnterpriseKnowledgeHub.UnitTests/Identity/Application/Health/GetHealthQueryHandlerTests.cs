using EnterpriseKnowledgeHub.Modules.Identity.Application.Health;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Application.Health;

public class GetHealthQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDatabaseIsReachable_ReturnsHealthyTrue()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new IdentityDbContext(options);
        var handler = new GetHealthQueryHandler(db);

        var result = await handler.Handle(new GetHealthQuery(), CancellationToken.None);

        Assert.True(result.IsHealthy);
    }
}
