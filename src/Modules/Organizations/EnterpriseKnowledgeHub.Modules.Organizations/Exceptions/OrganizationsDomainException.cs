using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Exceptions
{
    public class OrganizationsDomainException : DomainException
    {
        public OrganizationsDomainException(string message) : base(message)
        {
        }
    }
}
