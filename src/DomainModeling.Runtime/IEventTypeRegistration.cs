using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Registers an event type with the event type registry during service activation.
/// </summary>
internal interface IEventTypeRegistration
{
    /// <summary>
    ///     Registers the event type with the registry.
    /// </summary>
    /// <param name="registry">The event type registry.</param>
    void Register(
        IEventTypeRegistry registry
    );
}