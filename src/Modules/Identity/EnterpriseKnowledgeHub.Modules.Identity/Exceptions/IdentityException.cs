using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Identity.Exceptions
{
    public class IdentityException : DomainException
    {
        public IdentityException(string message) : base(message)
        {
        }
    }
}
