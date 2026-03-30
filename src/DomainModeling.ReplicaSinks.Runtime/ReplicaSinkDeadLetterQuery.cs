using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Describes a bounded dead-letter page request.
/// </summary>
public sealed class ReplicaSinkDeadLetterQuery
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeadLetterQuery" /> class.
    /// </summary>
    /// <param name="context">The operator context for the request.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="continuationToken">The opaque continuation token for the next page, when present.</param>
    /// <param name="includeFailureSummary">
    ///     A value indicating whether the caller requested detailed failure summaries when authorized.
    /// </param>
    public ReplicaSinkDeadLetterQuery(
        ReplicaSinkOperatorContext context,
        int pageSize,
        string? continuationToken = null,
        bool includeFailureSummary = false
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        Context = context;
        PageSize = pageSize;
        ContinuationToken = continuationToken;
        IncludeFailureSummary = includeFailureSummary;
    }

    /// <summary>
    ///     Gets the opaque continuation token for the next page, when present.
    /// </summary>
    public string? ContinuationToken { get; }

    /// <summary>
    ///     Gets the operator context for the request.
    /// </summary>
    public ReplicaSinkOperatorContext Context { get; }

    /// <summary>
    ///     Gets a value indicating whether the caller requested detailed failure summaries when authorized.
    /// </summary>
    public bool IncludeFailureSummary { get; }

    /// <summary>
    ///     Gets the requested page size.
    /// </summary>
    public int PageSize { get; }
}
