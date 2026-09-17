using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;

internal sealed class OrganizationAccessDeniedException : OrganizationsDomainException
{
    internal OrganizationAccessDeniedException(string message) : base(message)
    {
    }
}
