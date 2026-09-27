using System.Security.Claims;
using EnterpriseKnowledgeHub.Api.Authorization;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseKnowledgeHub.IntegrationTests.Authorization;

public sealed class DocumentAccessAuthorizationHandlerTests
{
    [Fact]
    public async Task UploadPolicy_WhenMembershipIsKnowledgeManager_Succeeds()
    {
        var context = CreateAuthorizationContext(DocumentAccessRequirement.Upload);
        var handler = new DocumentAccessAuthorizationHandler(
            new FakeOrganizationAccessService("KnowledgeManager"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DocumentPolicies_WhenMembershipIsEmployee_DoNotSucceed()
    {
        var handler = new DocumentAccessAuthorizationHandler(new FakeOrganizationAccessService("Employee"));
        var uploadContext = CreateAuthorizationContext(DocumentAccessRequirement.Upload);
        var readContext = CreateAuthorizationContext(DocumentAccessRequirement.Read);

        await handler.HandleAsync(uploadContext);
        await handler.HandleAsync(readContext);

        Assert.False(uploadContext.HasSucceeded);
        Assert.False(readContext.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        IAuthorizationRequirement requirement) => new(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity("Test")),
            resource: null);

    private sealed class FakeOrganizationAccessService(string role) : IOrganizationAccessService
    {
        public Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IOrganizationMembership>(new FakeOrganizationMembership(role));

        public void ClearCache(Guid userId, Guid organizationId)
        {
        }
    }

    private sealed record FakeOrganizationMembership(string Role) : IOrganizationMembership
    {
        public Guid MembershipId { get; } = Guid.NewGuid();
        public Guid OrganizationId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
    }
}
