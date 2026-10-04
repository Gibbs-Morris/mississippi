using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Returns a configured latest result while relying on the interface's versioned-read fallback.
/// </summary>
internal sealed class LatestOnlyProjectionFetcher : IProjectionFetcher
{
    /// <summary>
    ///     Gets the number of latest reads.
    /// </summary>
    public int FetchCount { get; private set; }

    /// <summary>
    ///     Gets the last requested entity identifier.
    /// </summary>
    public string? LastEntityId { get; private set; }

    /// <summary>
    ///     Gets the last requested projection type.
    /// </summary>
    public Type? LastProjectionType { get; private set; }

    /// <summary>
    ///     Gets the result returned by the latest read.
    /// </summary>
    public required ProjectionFetchResult Result { get; init; }

    /// <inheritdoc />
    public Task<ProjectionFetchResult?> FetchAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        FetchCount++;
        LastProjectionType = projectionType;
        LastEntityId = entityId;
        return Task.FromResult<ProjectionFetchResult?>(Result);
    }
}