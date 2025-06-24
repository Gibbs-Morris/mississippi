namespace Mississippi.Core.Abstractions.Cqrs;

/// <summary>
///     Represents a versioned reference to a query.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Core.Abstractions.Cqrs.VersionedQueryReference")]
public readonly record struct VersionedQueryReference
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionedQueryReference" /> struct.
    /// </summary>
    /// <param name="queryType">The query type.</param>
    /// <param name="id">The query identifier.</param>
    /// <param name="version">The query version.</param>
    public VersionedQueryReference(
        string queryType,
        string id,
        long version
    )
    {
        QueryValidation.ValidateToken(queryType, nameof(queryType));
        QueryValidation.ValidateToken(id, nameof(id));
        QueryValidation.ValidateVersion(version, nameof(version));
        QueryType = queryType;
        Id = id;
        Version = version;
    }

    /// <summary>
    ///     Gets the query type.
    /// </summary>
    [Id(0)]
    public string QueryType { get; }

    /// <summary>
    ///     Gets the query identifier.
    /// </summary>
    [Id(1)]
    public string Id { get; }

    /// <summary>
    ///     Gets the query version.
    /// </summary>
    [Id(2)]
    public long Version { get; }

    /// <summary>
    ///     Gets the versioned path in <c>type/id/version</c> form.
    /// </summary>
    public string VersionedPath => $"{QueryType}/{Id}/{Version}";
}