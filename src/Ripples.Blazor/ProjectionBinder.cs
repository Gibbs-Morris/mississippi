using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor;

/// <summary>
///     A binding helper that manages projection subscriptions and handles entity ID changes automatically.
/// </summary>
/// <typeparam name="TProjection">The projection type.</typeparam>
internal sealed class ProjectionBinder<TProjection> : IProjectionBinder<TProjection>
    where TProjection : class
{
    private readonly IProjectionCache cache;

    private bool disposed;

    private IProjectionSubscription<TProjection>? subscription;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionBinder{TProjection}" /> class.
    /// </summary>
    /// <param name="cache">The projection cache to use for subscriptions.</param>
    public ProjectionBinder(
        IProjectionCache cache
    )
    {
        ArgumentNullException.ThrowIfNull(cache);
        this.cache = cache;
    }

    /// <inheritdoc />
    public TProjection? Current => subscription?.Current;

    /// <inheritdoc />
    public string? EntityId { get; private set; }

    /// <inheritdoc />
    public bool IsLoaded => subscription?.IsLoaded ?? false;

    /// <inheritdoc />
    public bool IsLoading => subscription?.IsLoading ?? false;

    /// <inheritdoc />
    public Exception? LastError => subscription?.LastError;

    /// <inheritdoc />
    public async Task BindAsync(
        string entityId,
        Action onChanged,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(onChanged);
        ObjectDisposedException.ThrowIf(disposed, this);

        // If already bound to same entity, no-op
        if (EntityId == entityId)
        {
            return;
        }

        // Dispose existing subscription if bound to different entity
        if (subscription is not null)
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
            subscription = null;
        }

        // Subscribe to new entity
        EntityId = entityId;
        subscription = await cache.SubscribeAsync<TProjection>(entityId, onChanged, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (subscription is not null)
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
            subscription = null;
        }

        EntityId = null;
    }

    /// <inheritdoc />
    public async Task UnbindAsync()
    {
        if (subscription is not null)
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
            subscription = null;
        }

        EntityId = null;
    }
}