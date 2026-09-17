using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakeCurrentOrganization : ICurrentOrganization
{
    public Guid? OrganizationId { get; set; }
}
