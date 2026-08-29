using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Identity.Domain.Exceptions
{
    internal class IdentityException : DomainException
    {
        internal IdentityException(string message) : base(message)
        {
        }
    }
}