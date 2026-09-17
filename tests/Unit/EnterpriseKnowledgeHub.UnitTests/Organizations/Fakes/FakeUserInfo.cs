using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakeUserInfo(Guid userId, IReadOnlyCollection<Guid>? organizationIds = null) : IUserInfo
{
    public Guid UserId { get; } = userId;
    public IReadOnlyCollection<Guid>? OrganizationIds { get; } = organizationIds;
}
