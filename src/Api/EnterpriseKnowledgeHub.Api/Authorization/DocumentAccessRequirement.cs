using Microsoft.AspNetCore.Authorization;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Api.Authorization;

public sealed class DocumentAccessRequirement(IReadOnlySet<string> allowedRoles) : IAuthorizationRequirement
{
    private static readonly IReadOnlySet<string> DocumentManagementRoles =
        new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(OrganizationRole.OrganizationOwner),
            nameof(OrganizationRole.OrganizationAdmin),
            nameof(OrganizationRole.KnowledgeManager)
        };

    public static readonly DocumentAccessRequirement Read = new(DocumentManagementRoles);

    public static readonly DocumentAccessRequirement Upload = new(DocumentManagementRoles);

    public bool Allows(string role) => allowedRoles.Count == 0 || allowedRoles.Contains(role);
}
