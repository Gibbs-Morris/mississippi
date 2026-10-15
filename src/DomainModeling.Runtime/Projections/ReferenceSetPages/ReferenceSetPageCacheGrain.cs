using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

/// <summary>Prepares one ordinal index per immutable versioned activation and derives pages on demand.</summary>
/// <typeparam name="TProjection">The named membership projection.</typeparam>
/// <typeparam name="TSource">The referenced source projection type.</typeparam>
[StatelessWorker]
internal sealed class ReferenceSetPageCacheGrain<TProjection, TSource>
    : IReferenceSetPageCacheGrain<TProjection, TSource>,
      IGrainBase
    where TProjection : class
{
    private ImmutableArray<ReferenceSetMember> index = [];

    private UxProjectionVersionedCacheKey key;

    /// <summary>Initializes a new instance of the <see cref="ReferenceSetPageCacheGrain{TProjection, TSource}" /> class.</summary>
    /// <param name="grainContext">The Orleans activation context.</param>
    /// <param name="projections">The existing immutable projection cache factory.</param>
    /// <param name="membership">The existing pure membership accessor.</param>
    /// <param name="logger">The typed logger.</param>
    public ReferenceSetPageCacheGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory projections,
        ReferenceSetStateAccess<TProjection, TSource> membership,
        ILogger<ReferenceSetPageCacheGrain<TProjection, TSource>> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Projections = projections ?? throw new ArgumentNullException(nameof(projections));
        Membership = membership ?? throw new ArgumentNullException(nameof(membership));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger Logger { get; }

    private ReferenceSetStateAccess<TProjection, TSource> Membership { get; }

    private IUxProjectionGrainFactory Projections { get; }

    /// <summary>Loads the existing exact-version cache and prepares this activation's ordinal index.</summary>
    /// <param name="token">Cancels activation.</param>
    /// <returns>The asynchronous activation.</returns>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        Stopwatch timer = Stopwatch.StartNew();
        key = UxProjectionVersionedCacheKey.Parse(this.GetPrimaryKeyString());
        Logger.PageCacheActivating(typeof(TProjection).Name, key.Version.Value);
        TProjection projection =
            await Projections.GetUxProjectionVersionedCacheGrain<TProjection>(key).GetAsync(token) ??
            throw new InvalidOperationException("The requested membership snapshot is unavailable.");
        index = Membership.Read(projection)
            .Members.OrderBy(member => member.Key, StringComparer.Ordinal)
            .Select(member => new ReferenceSetMember(member.Key, member.Value))
            .ToImmutableArray();
        Logger.PageIndexPrepared(key.Version.Value, index.Length, timer.Elapsed.TotalMilliseconds);
    }

    /// <summary>Records the end of this activation's immutable cache lifetime.</summary>
    /// <param name="reason">The Orleans deactivation reason.</param>
    /// <param name="token">The deactivation token.</param>
    /// <returns>A completed task.</returns>
    public Task OnDeactivateAsync(
        DeactivationReason reason,
        CancellationToken token
    )
    {
        Logger.PageCacheDeactivated(key.Version.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<ReferenceSetPage<TProjection, TSource>> ReadAsync(
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        ReferenceSetPageSelection.Validate(offset, limit);
        cancellationToken.ThrowIfCancellationRequested();
        Stopwatch timer = Stopwatch.StartNew();
        Logger.PageReadStarted(typeof(TProjection).Name, offset, limit, key.Version.Value);
        ReferenceSetPage<TProjection, TSource> page = new()
        {
            Offset = offset,
            Limit = limit,
            TotalCount = index.Length,
            Version = key.Version,
            References = offset >= index.Length ? [] : index.Slice(offset, Math.Min(limit, index.Length - offset)),
        };
        Logger.PageReadCompleted(page.Version.Value, page.References.Length, timer.Elapsed.TotalMilliseconds);
        return ValueTask.FromResult(page);
    }
}