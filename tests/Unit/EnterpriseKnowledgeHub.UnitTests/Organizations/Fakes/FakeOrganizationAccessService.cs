using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakeOrganizationAccessService(IOrganizationMembership? membership) : IOrganizationAccessService
{
    public int CallCount { get; private set; }
    public int ClearCacheCallCount { get; private set; }

    public Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken)
    {
        CallCount++;

        if (membership is null)
            throw new InvalidOperationException("No organization was specified for this request.");

        return Task.FromResult(membership);
    }

    public void ClearCache(Guid userId, Guid organizationId)
    {
        ClearCacheCallCount++;
    }
}
