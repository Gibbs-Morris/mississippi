using System;
using System.Threading.Tasks;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor;

/// <summary>
///     Represents an active subscription to a cached projection.
/// </summary>
/// <typeparam name="TProjection">The projection type.</typeparam>
internal sealed class ProjectionSubscription<TProjection> : IProjectionSubscription<TProjection>
    where TProjection : class
{
    private readonly Action onChanged;

    private readonly Action<ProjectionSubscription<TProjection>> onDispose;

    private readonly IRipple<TProjection> ripple;

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionSubscription{TProjection}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="ripple">The underlying ripple providing data.</param>
    /// <param name="onChanged">Callback when data changes.</param>
    /// <param name="onDispose">Callback when this subscription is disposed.</param>
    public ProjectionSubscription(
        string entityId,
        IRipple<TProjection> ripple,
        Action onChanged,
        Action<ProjectionSubscription<TProjection>> onDispose
    )
    {
        EntityId = entityId;
        this.ripple = ripple;
        this.onChanged = onChanged;
        this.onDispose = onDispose;

        // Wire up the change handler
        this.ripple.Changed += HandleChanged;
    }

    /// <inheritdoc />
    public TProjection? Current => ripple.Current;

    /// <inheritdoc />
    public string EntityId { get; }

    /// <inheritdoc />
    public bool IsLoaded => ripple.IsLoaded;

    /// <inheritdoc />
    public bool IsLoading => ripple.IsLoading;

    /// <inheritdoc />
    public Exception? LastError => ripple.LastError;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return ValueTask.CompletedTask;
        }

        disposed = true;

        // Unhook our change handler
        ripple.Changed -= HandleChanged;

        // Notify the cache that this subscription is done (for ref counting)
        onDispose(this);
        return ValueTask.CompletedTask;
    }

    private void HandleChanged(
        object? sender,
        EventArgs e
    )
    {
        if (!disposed)
        {
            onChanged();
        }
    }
}