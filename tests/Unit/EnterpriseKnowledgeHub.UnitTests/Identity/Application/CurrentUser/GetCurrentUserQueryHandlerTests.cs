using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using EnterpriseKnowledgeHub.UnitTests.Identity.Fakes;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Application.CurrentUser;

public class GetCurrentUserQueryHandlerTests
{
    private static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenExternalIdIsMissing_ThrowsInvalidOperationException()
    {
        using var db = CreateDbContext();
        var currentUser = new FakeCurrentUser { ExternalId = null };
        var handler = new GetCurrentUserQueryHandler(db, currentUser);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetCurrentUserQuery(), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsFoundResult()
    {
        using var db = CreateDbContext();
        var user = ApplicationUser.Create("external-id", "user@example.com", "Jane Doe");
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUser { ExternalId = "external-id" };
        var handler = new GetCurrentUserQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.DisplayName, result.Name);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundResult()
    {
        using var db = CreateDbContext();
        var currentUser = new FakeCurrentUser { ExternalId = "unknown-external-id" };
        var handler = new GetCurrentUserQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Email);
        Assert.Null(result.Name);
    }
}
