using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Exceptions
{
    internal class OrganizationsDomainException : DomainException
    {
        internal OrganizationsDomainException(string message) : base(message)
        {
        }
    }
}
