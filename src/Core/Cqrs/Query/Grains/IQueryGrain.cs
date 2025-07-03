using Mississippi.Core.Abstractions.Cqrs.Query;

using Orleans.Concurrency;


namespace Mississippi.Core.Cqrs.Query.Grains;

public interface IQueryService<TQuery>
{
    Task<QuerySnapshot<TQuery> Get();
}

/// <summary>
///     Defines a grain for querying read models of type <typeparamref name="TQuery" />.
/// </summary>
/// <typeparam name="TQuery">The query state type.</typeparam>
[Alias("Mississippi.Core.Abstractions.Cqrs.IQueryGrain")]
public interface IQueryGrain<TQuery> : IGrainWithStringKey
{
    /// <summary>
    ///     Reads the query snapshot at the specified version or the latest if <c>null</c>.
    /// </summary>
    /// <param name="version">The optional version to read; if <c>null</c>, reads latest.</param>
    /// <returns>A task that returns the <see cref="QuerySnapshot{TQuery}" />.</returns>
    [Alias("ReadAsync")]
    [ReadOnly]
    Task<QuerySnapshot<TQuery>> ReadAsync(
        long? version = null
    );

    /// <summary>
    ///     Gets the current version number of the query.
    /// </summary>
    /// <returns>A task that returns the current version.</returns>
    [Alias("GetCurrentVersionAsync")]
    [ReadOnly]
    Task<long> GetCurrentVersionAsync();

    /// <summary>
    ///     Gets a reference to the query without version information.
    /// </summary>
    /// <returns>A task that returns the <see cref="QueryReference" />.</returns>
    [Alias("GetReferenceAsync")]
    [ReadOnly]
    Task<QueryReference> GetReferenceAsync();

    /// <summary>
    ///     Gets a versioned reference for the specified query version.
    /// </summary>
    /// <param name="version">The version number for the reference.</param>
    /// <returns>A task that returns the <see cref="VersionedQueryReference" />.</returns>
    [Alias("GetVersionedReferenceAsync")]
    [ReadOnly]
    Task<VersionedQueryReference> GetVersionedReferenceAsync(
        long version
    );
}