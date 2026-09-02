using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;

public sealed record UserOrganizationItem(Guid Id, string Name, DateTime CreatedAt, OrganizationRole Role);

public sealed record GetUserOrganizationsResult(IReadOnlyList<UserOrganizationItem> Organizations);
