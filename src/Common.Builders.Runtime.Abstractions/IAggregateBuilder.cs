namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Typed aggregate composition contract.
/// </summary>
/// <typeparam name="TSnapshot">Snapshot type.</typeparam>
public interface IAggregateBuilder<TSnapshot>
{
    /// <summary>
    ///     Adds a snapshot-state converter for aggregate state transitions.
    /// </summary>
    /// <typeparam name="TConverter">Converter type.</typeparam>
    /// <returns>The same builder instance for fluent chaining.</returns>
    IAggregateBuilder<TSnapshot> AddSnapshotStateConverter<TConverter>()
        where TConverter : class;
}