using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Events.OrganizationInvitationCreated;

public sealed record OrganizationInvitationCreatedEvent(
    Guid InvitationId,
    string RecipientEmail,
    string OrganizationName,
    string InviterName,
    string InvitationUrl
) : INotification;