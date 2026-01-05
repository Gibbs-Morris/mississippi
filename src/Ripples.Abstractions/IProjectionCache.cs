using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Circuit-scoped projection cache that persists data across page navigations.
/// </summary>
/// <remarks>
///     <para>
///         This service is registered as scoped (one instance per Blazor circuit) and
///         maintains projection data even when components are disposed. This enables
///         fast page navigation where previously-viewed data is instantly available.
///     </para>
///     <para>
///         <b>Navigation Flow:</b>
///     </para>
///     <list type="number">
///         <item>
///             <description>Page A subscribes to Projection X - data is fetched and cached.</description>
///         </item>
///         <item>
///             <description>User navigates to Page B - Page A's subscription is disposed but data stays cached.</description>
///         </item>
///         <item>
///             <description>User navigates back to Page A - data is instantly available from cache.</description>
///         </item>
///     </list>
///     <para>
///         Cache entries are evicted based on LRU (Least Recently Used) policy when
///         the maximum cache size is reached.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     @inject IProjectionCache ProjectionCache
///     @implements IAsyncDisposable
///
///     &lt;MessageList Messages="@subscription?.Current?.Messages" IsLoading="@(subscription?.IsLoading ?? true)" /&gt;
///
///     @code {
///         private IProjectionSubscription&lt;ChannelMessagesProjection&gt;? subscription;
///
///         protected override async Task OnInitializedAsync()
///         {
///             subscription = await ProjectionCache.SubscribeAsync&lt;ChannelMessagesProjection&gt;(
///                 ChannelId,
///                 () =&gt; InvokeAsync(StateHasChanged));
///         }
///
///         public async ValueTask DisposeAsync()
///         {
///             if (subscription is not null)
///             {
///                 await subscription.DisposeAsync();
///             }
///         }
///     }
///     </code>
/// </example>
public interface IProjectionCache : IAsyncDisposable
{
    /// <summary>
    ///     Creates a binder for managing subscription to a projection type with automatic
    ///     entity ID switching support.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <returns>A new binder instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this for master-detail scenarios where the entity ID may change based on
    ///         user selection. Call <see cref="IProjectionBinder{TProjection}.BindAsync" /> from
    ///         <c>OnParametersSetAsync</c> to handle ID changes automatically.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     private IProjectionBinder&lt;UserProfileProjection&gt;? binder;
    /// 
    ///     protected override void OnInitialized()
    ///     {
    ///         binder = ProjectionCache.CreateBinder&lt;UserProfileProjection&gt;();
    ///     }
    /// 
    ///     protected override async Task OnParametersSetAsync()
    ///     {
    ///         await binder!.BindAsync(UserId, () =&gt; InvokeAsync(StateHasChanged));
    ///     }
    ///     </code>
    /// </example>
    IProjectionBinder<TProjection> CreateBinder<TProjection>()
        where TProjection : class;

    /// <summary>
    ///     Explicitly removes a projection from the cache.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A task representing the eviction operation.</returns>
    /// <remarks>
    ///     Use this to force-refresh data or to free memory. Active subscriptions
    ///     will receive a fresh copy on their next update.
    /// </remarks>
    Task EvictAsync<TProjection>(
        string entityId
    )
        where TProjection : class;

    /// <summary>
    ///     Gets the cached projection data without subscribing.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>
    ///     The cached projection data, or <c>null</c> if not in cache.
    /// </returns>
    /// <remarks>
    ///     This is useful for checking if data exists before subscribing,
    ///     or for read-only access without triggering UI updates.
    /// </remarks>
    TProjection? GetCached<TProjection>(
        string entityId
    )
        where TProjection : class;

    /// <summary>
    ///     Gets a value indicating whether a projection is currently cached.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>
    ///     <c>true</c> if the projection is in cache; otherwise, <c>false</c>.
    /// </returns>
    bool IsCached<TProjection>(
        string entityId
    )
        where TProjection : class;

    /// <summary>
    ///     Forces a refresh of a cached projection.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the refresh operation.</returns>
    Task RefreshAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;

    /// <summary>
    ///     Subscribes to a projection, returning a handle that provides access to the data.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier (e.g., channel ID, user ID).</param>
    /// <param name="onChanged">Callback invoked when the projection data changes.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A subscription handle. Dispose to remove the callback but keep data cached.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         If the projection is already cached (e.g., from a previous page visit),
    ///         the cached data is immediately available via <see cref="IProjectionSubscription{T}.Current" />.
    ///     </para>
    ///     <para>
    ///         Disposing the subscription removes the <paramref name="onChanged" /> callback
    ///         but keeps the projection data in cache for quick re-subscription.
    ///     </para>
    /// </remarks>
    Task<IProjectionSubscription<TProjection>> SubscribeAsync<TProjection>(
        string entityId,
        Action onChanged,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;
}