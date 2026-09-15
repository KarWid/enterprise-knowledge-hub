using EnterpriseKnowledgeHub.Contracts.Enums;

namespace EnterpriseKnowledgeHub.Contracts.Identity.GetMe
{
    public record MeOrganizationItem(Guid Id, string Name, OrganizationRole Role);
}
