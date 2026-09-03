using EnterpriseKnowledgeHub.BuildingBlocks.Domain;

namespace EnterpriseKnowledgeHub.Application.Exceptions;

public sealed class UserNotInvitedException : DomainException
{
    public UserNotInvitedException(string message) : base(message)
    {
    }
}
