namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Typed saga composition contract.
/// </summary>
/// <typeparam name="TSagaState">Saga state type.</typeparam>
public interface ISagaBuilder<out TSagaState>
{
    /// <summary>
    ///     Gets the saga state type marker.
    /// </summary>
    TSagaState? SagaStateMarker { get; }
}