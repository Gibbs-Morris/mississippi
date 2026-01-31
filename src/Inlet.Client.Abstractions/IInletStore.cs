using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.Abstractions;

/// <summary>
///     Composite interface providing access to Redux-style state management
///     with convenience methods for server-synced projections.
/// </summary>
/// <remarks>
///     <para>
///         Components can inject <see cref="IInletStore" /> for convenient access to
///         <see cref="IStore" /> functionality plus projection-specific helper methods.
///     </para>
///     <para>
///         Projection state is stored in <see cref="State.ProjectionsFeatureState" />
///         and follows the Redux pattern: actions → reducers → state.
///         Use <c>GetState&lt;ProjectionsFeatureState&gt;()</c> for direct access.
///     </para>
/// </remarks>
public interface IInletStore : IStore
{
}