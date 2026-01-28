using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;


namespace Mississippi.Inlet.Client.L0Tests.Helpers;

/// <summary>
///     Test implementation of IProjectionFetcher for testing.
/// </summary>
internal sealed class TestProjectionFetcher : IProjectionFetcher
{
    /// <inheritdoc />
    public Task<ProjectionFetchResult?> FetchAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult<ProjectionFetchResult?>(null);
}