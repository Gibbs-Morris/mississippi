using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     A binding helper that manages projection subscriptions and handles entity ID changes automatically.
/// </summary>
/// <remarks>
///     <para>
///         Use this for master-detail scenarios where a component parameter (entity ID) may change
///         and the subscription needs to switch to the new entity. The binder handles:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Automatic disposal of the old subscription when entity ID changes.</description>
///         </item>
///         <item>
///             <description>Creating a new subscription to the new entity ID.</description>
///         </item>
///         <item>
///             <description>No-op when entity ID hasn't changed (safe to call repeatedly).</description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="TProjection">The projection type.</typeparam>
/// <example>
///     <code>
///     public partial class UserDetails : ComponentBase, IAsyncDisposable
///     {
///         [Parameter, EditorRequired]
///         public required string UserId { get; set; }
///
///         [Inject]
///         private IProjectionCache ProjectionCache { get; set; } = null!;
///
///         private IProjectionBinder&lt;UserProfileProjection&gt;? binder;
///
///         protected override void OnInitialized()
///         {
///             binder = ProjectionCache.CreateBinder&lt;UserProfileProjection&gt;();
///         }
///
///         protected override async Task OnParametersSetAsync()
///         {
///             await binder!.BindAsync(UserId, () =&gt; InvokeAsync(StateHasChanged));
///         }
///
///         public ValueTask DisposeAsync() =&gt; binder?.DisposeAsync() ?? ValueTask.CompletedTask;
///     }
///     </code>
/// </example>
public interface IProjectionBinder<out TProjection> : IAsyncDisposable
    where TProjection : class
{
    /// <summary>
    ///     Gets the current projection data.
    /// </summary>
    /// <remarks>
    ///     Returns <c>null</c> if no entity is bound or data hasn't loaded yet.
    /// </remarks>
    TProjection? Current { get; }

    /// <summary>
    ///     Gets the currently bound entity ID.
    /// </summary>
    /// <remarks>
    ///     Returns <c>null</c> if no entity has been bound yet.
    /// </remarks>
    string? EntityId { get; }

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

    /// <summary>
    ///     Binds to an entity ID. If the ID has changed, disposes the old subscription
    ///     and creates a new one for the new entity.
    /// </summary>
    /// <param name="entityId">The entity identifier to bind to.</param>
    /// <param name="onChanged">Callback invoked when the projection data changes.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the bind operation.</returns>
    /// <remarks>
    ///     <para>
    ///         This method is safe to call repeatedly with the same entity ID - it will
    ///         no-op if the ID hasn't changed. Call this from <c>OnParametersSetAsync</c>.
    ///     </para>
    ///     <para>
    ///         The <paramref name="onChanged" /> callback is stored and reused. If you need
    ///         to change the callback, call <see cref="UnbindAsync" /> first.
    ///     </para>
    /// </remarks>
    Task BindAsync(
        string entityId,
        Action onChanged,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Unbinds from the current entity, disposing any active subscription.
    /// </summary>
    /// <returns>A task representing the unbind operation.</returns>
    /// <remarks>
    ///     After calling this, <see cref="Current" /> will be <c>null</c> and
    ///     <see cref="EntityId" /> will be <c>null</c>.
    /// </remarks>
    Task UnbindAsync();
}