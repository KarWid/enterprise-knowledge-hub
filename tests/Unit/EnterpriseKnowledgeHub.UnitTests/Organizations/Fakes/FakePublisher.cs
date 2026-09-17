using MediatR;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakePublisher : IPublisher
{
    public List<INotification> PublishedNotifications { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is INotification typed)
            PublishedNotifications.Add(typed);

        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        PublishedNotifications.Add(notification);
        return Task.CompletedTask;
    }
}
