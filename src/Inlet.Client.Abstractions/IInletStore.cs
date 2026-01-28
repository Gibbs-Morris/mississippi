using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.Abstractions;

/// <summary>
///     Composite interface providing access to both local Redux-style state
///     and server-synced projection cache.
/// </summary>
/// <remarks>
///     <para>
///         Components can inject <see cref="IInletStore" /> for convenient access to
///         both <see cref="IStore" /> (for feature states and actions) and
///         <see cref="IProjectionCache" /> (for server-synced projections).
///     </para>
///     <para>
///         Alternatively, components can inject <see cref="IStore" /> and
///         <see cref="IProjectionCache" /> separately for finer-grained dependencies.
///     </para>
/// </remarks>
public interface IInletStore
    : IStore,
      IProjectionCache
{
}