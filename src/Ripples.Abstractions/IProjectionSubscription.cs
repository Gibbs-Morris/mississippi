using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Represents an active subscription to a projection for a specific entity.
/// </summary>
/// <remarks>
///     <para>
///         Disposing this subscription removes the UI callback but does NOT dispose
///         the underlying cached data. The data remains available for quick re-subscription
///         when navigating back to the same page.
///     </para>
///     <para>
///         This enables efficient page navigation where users can quickly move between
///         pages without losing cached projection data.
///     </para>
/// </remarks>
/// <typeparam name="TProjection">The projection type.</typeparam>
public interface IProjectionSubscription<out TProjection> : IAsyncDisposable
    where TProjection : class
{
    /// <summary>
    ///     Gets the current projection data.
    /// </summary>
    /// <remarks>
    ///     Returns <c>null</c> if not yet loaded.
    /// </remarks>
    TProjection? Current { get; }

    /// <summary>
    ///     Gets the entity ID this subscription is for.
    /// </summary>
    string EntityId { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection has been loaded at least once.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is currently loading.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    ///     Gets the last error that occurred, if any.
    /// </summary>
    Exception? LastError { get; }
}