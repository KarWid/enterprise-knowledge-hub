using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;

public sealed class OrganizationAccessDeniedException : OrganizationsDomainException
{
    public OrganizationAccessDeniedException(string message) : base(message)
    {
    }
}
