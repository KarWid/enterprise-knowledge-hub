using Mediator;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakePublisher : IPublisher
{
    public List<INotification> PublishedNotifications { get; } = [];

    public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is INotification typed)
            PublishedNotifications.Add(typed);

        return ValueTask.CompletedTask;
    }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        PublishedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }
}
