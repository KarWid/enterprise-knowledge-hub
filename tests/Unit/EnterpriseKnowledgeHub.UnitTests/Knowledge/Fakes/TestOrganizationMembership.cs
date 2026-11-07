using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;

internal sealed record TestOrganizationMembership(Guid OrganizationId, Guid UserId) : IOrganizationMembership
{
    public Guid MembershipId { get; } = Guid.NewGuid();
    public string Role => "KnowledgeManager";
}
