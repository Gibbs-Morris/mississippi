namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Defines a factory for creating the initial state of a snapshot.
/// </summary>
/// <typeparam name="TSnapshot">The type of snapshot state to create.</typeparam>
/// <remarks>
///     <para>
///         This factory is used by snapshot cache grains to create the initial state
///         when no events have been processed yet. Implementations provide the domain-specific
///         initial state for each snapshot type.
///     </para>
///     <para>
///         Implementations should be registered in DI as singletons since initial state
///         creation is typically stateless.
///     </para>
/// </remarks>
public interface IInitialStateFactory<out TSnapshot>
{
    /// <summary>
    ///     Creates the initial state for the snapshot type.
    /// </summary>
    /// <returns>A new instance representing the initial, default state of the snapshot.</returns>
    TSnapshot Create();
}