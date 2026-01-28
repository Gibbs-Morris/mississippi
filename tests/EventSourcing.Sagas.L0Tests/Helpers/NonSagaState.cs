namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     A state type that does NOT implement <see cref="Mississippi.EventSourcing.Sagas.Abstractions.ISagaState" />.
///     Used for testing fallback behavior in saga effects when state type lacks ISagaState interface.
/// </summary>
/// <remarks>
///     This type intentionally has a property to avoid empty record analyzer warning (S2094).
///     The property exists solely to make the record non-empty and does not affect test behavior.
/// </remarks>
/// <param name="TestProperty">A placeholder property to avoid empty record analyzer warning.</param>
internal sealed record NonSagaState(string? TestProperty = null);