using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Registers a single event type with the event type registry.
/// </summary>
/// <typeparam name="TEvent">The event type to register.</typeparam>
internal sealed class EventTypeRegistration<TEvent> : IEventTypeRegistration
    where TEvent : class
{
    /// <inheritdoc />
    public void Register(
        IEventTypeRegistry registry
    )
    {
        string eventName = EventStorageNameHelper.GetStorageName<TEvent>();
        registry.Register(eventName, typeof(TEvent));
    }
}