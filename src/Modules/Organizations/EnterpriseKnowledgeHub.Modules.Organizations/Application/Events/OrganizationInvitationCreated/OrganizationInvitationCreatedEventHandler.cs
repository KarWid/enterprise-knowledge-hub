using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Events.OrganizationInvitationCreated;

internal sealed class OrganizationInvitationCreatedEventHandler()
    : INotificationHandler<OrganizationInvitationCreatedEvent>
{
    public async ValueTask Handle(
        OrganizationInvitationCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{notification.OrganizationName}, inviter: {notification.InviterName}");
    }
}
