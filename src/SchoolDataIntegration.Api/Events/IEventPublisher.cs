namespace SchoolDataIntegration.Api.Events;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default);

    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
}
