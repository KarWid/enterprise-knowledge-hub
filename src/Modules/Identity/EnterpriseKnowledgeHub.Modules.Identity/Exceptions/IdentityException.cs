using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Modules.Identity.Exceptions
{
    internal class IdentityException : DomainException
    {
        internal IdentityException(string message) : base(message)
        {
        }
    }
}