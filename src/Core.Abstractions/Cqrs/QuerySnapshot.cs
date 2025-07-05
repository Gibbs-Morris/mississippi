namespace Mississippi.Core.Abstractions.Cqrs;

/// <summary>
///     A snapshot of Projection query at Projection particular version.
/// </summary>
/// <typeparam name="TState">The type of the query state.</typeparam>
[GenerateSerializer]
[Alias("Mississippi.Core.Abstractions.Cqrs.QuerySnapshot`1")]
public record QuerySnapshot<TState>
{
    /// <summary>
    ///     Initializes Projection new instance of the <see cref="QuerySnapshot{TState}" /> class.
    /// </summary>
    /// <param name="reference">The versioned query reference.</param>
    /// <param name="state">The query state.</param>
    public QuerySnapshot(
        VersionedQueryReference reference,
        TState? state
    )
    {
        if (reference == default)
        {
            throw new ArgumentException("Reference must be supplied.", nameof(reference));
        }

        ArgumentNullException.ThrowIfNull(state);
        Reference = reference;
        State = state!;
    }

    /// <summary>
    ///     Gets the versioned query reference associated with this snapshot.
    /// </summary>
    [Id(0)]
    public VersionedQueryReference Reference { get; }

    /// <summary>
    ///     Gets the query state held by this snapshot.
    /// </summary>
    [Id(1)]
    public TState State { get; init; }
}