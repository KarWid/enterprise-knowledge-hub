using EnterpriseKnowledgeHub.Modules.Identity.Application.ProvisionApplicationUser;
using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Application.ProvisionApplicationUser;

public class ProvisionApplicationUserCommandHandlerTests
{
    private static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CreatesAndReturnsNewUser()
    {
        using var db = CreateDbContext();
        var handler = new ProvisionApplicationUserCommandHandler(db);

        var result = await handler.Handle(
            new ProvisionApplicationUserCommand("external-id", "user@example.com", "Jane Doe"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Jane Doe", result.DisplayName);

        var persisted = await db.ApplicationUsers.SingleAsync();
        Assert.Equal(result.Id, persisted.Id);
        Assert.Equal("external-id", persisted.ExternalIdentityId);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsExistingUserWithoutCreatingDuplicate()
    {
        using var db = CreateDbContext();
        var existingUser = ApplicationUser.Create("external-id", "existing@example.com", "Existing User");
        db.ApplicationUsers.Add(existingUser);
        await db.SaveChangesAsync();

        var handler = new ProvisionApplicationUserCommandHandler(db);

        var result = await handler.Handle(
            new ProvisionApplicationUserCommand("external-id", "new@example.com", "New Name"),
            CancellationToken.None);

        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal("existing@example.com", result.Email);
        Assert.Equal("Existing User", result.DisplayName);
        Assert.Single(db.ApplicationUsers);
    }
}
