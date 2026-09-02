using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Events.OrganizationInvitationCreated;

internal sealed class OrganizationInvitationCreatedEventHandler()
    : INotificationHandler<OrganizationInvitationCreatedEvent>
{
    public async Task Handle(
        OrganizationInvitationCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{notification.OrganizationName}, inviter: {notification.InviterName}");
    }
}
