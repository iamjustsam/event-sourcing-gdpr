using Newtonsoft.Json;

namespace EventSourcingGdpr.ForgettablePayloads.Domain;

public abstract class Aggregate
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly IDictionary<Type, List<Action<IDomainEvent>>> _eventHandlers = new Dictionary<Type, List<Action<IDomainEvent>>>();

    [JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void On<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        void ActionToRegister(IDomainEvent domainEvent)
        {
            var typedDomainEvent = (TEvent)domainEvent;
            handler(typedDomainEvent);
        }

        if (_eventHandlers.ContainsKey(typeof(TEvent)))
        {
            _eventHandlers[typeof(TEvent)].Add(ActionToRegister);
        }
        else
        {
            _eventHandlers.Add(typeof(TEvent), new List<Action<IDomainEvent>> { ActionToRegister });
        }
    }

    protected void Handle<TDomainEvent>(TDomainEvent @event) where TDomainEvent : IDomainEvent
    {
        _domainEvents.Add(@event);

        var eventType = @event.GetType();
        if (!_eventHandlers.ContainsKey(eventType))
            throw new InvalidOperationException($"There are no actions registered to handle {eventType}");

        foreach (var action in _eventHandlers[eventType])
            action(@event);
    }
}
