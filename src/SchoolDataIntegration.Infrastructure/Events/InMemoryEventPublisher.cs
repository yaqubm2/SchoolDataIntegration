using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SchoolDataIntegration.Application;
namespace SchoolDataIntegration.Infrastructure;

public class InMemoryEventPublisher : IEventPublisher
{
    #region private fields
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<InMemoryEventPublisher> _logger;

    #endregion

    #region constructor
    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    #endregion

    #region public Methods

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<object>());
        lock (list)
        {
            list.Add(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            return;
        }

        List<object> snapshot;
        lock (list)
        {
            snapshot = new List<object>(list);
        }

        foreach (var handlerObj in snapshot)
        {
            var handler = (Func<TEvent, CancellationToken, Task>)handlerObj;
            try
            {
                await handler(@event, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Event handler for {EventType} threw an exception; continuing with remaining handlers",
                    typeof(TEvent).Name);
            }
        }
    }

    #endregion
}
