using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public string? ExternalId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}
