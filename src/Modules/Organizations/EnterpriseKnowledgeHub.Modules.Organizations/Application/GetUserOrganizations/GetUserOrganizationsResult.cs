using EnterpriseKnowledgeHub.Modules.Organizations.Domain;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.GetUserOrganizations;

public sealed record UserOrganizationItem(Guid Id, string Name, DateTime CreatedAt, OrganizationRole Role);

public sealed record GetUserOrganizationsResult(IReadOnlyList<UserOrganizationItem> Organizations);
