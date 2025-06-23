using Orleans.Serialization;

namespace Mississippi.Core.Abstractions.Cqrs;

/// <summary>
/// Represents an unversioned reference to a query.
/// </summary>
[GenerateSerializer]
public readonly record struct QueryReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryReference"/> struct.
    /// </summary>
    /// <param name="queryType">The query type.</param>
    /// <param name="id">The query identifier.</param>
    public QueryReference(
        string queryType,
        string id
    )
    {
        QueryValidation.ValidateToken(queryType, nameof(queryType));
        QueryValidation.ValidateToken(id, nameof(id));
        QueryType = queryType;
        Id = id;
    }

    /// <summary>
    /// Gets the query type.
    /// </summary>
    public string QueryType { get; }

    /// <summary>
    /// Gets the query identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the path in <c>type/id</c> form.
    /// </summary>
    public string Path => $"{QueryType}/{Id}";
}
