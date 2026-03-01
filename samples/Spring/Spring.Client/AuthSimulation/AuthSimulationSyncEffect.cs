using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;

using Spring.Client.Features.AuthSimulation;


namespace Spring.Client.AuthSimulation;

/// <summary>
///     Action effect that synchronizes <see cref="AuthSimulationState" /> to the
///     <see cref="AuthSimulationHeadersHandler" /> whenever the auth simulation
///     profile changes.
/// </summary>
/// <remarks>
///     <para>
///         This effect acts as a bridge between the Redux store and the HTTP handler.
///         The handler cannot depend on <c>IStore</c> directly because doing so would
///         create a circular DI chain:
///         <c>
///             HttpClient → AuthSimulationHeadersHandler → IStore → InletSignalRActionEffect →
///             AutoProjectionFetcher → HttpClient
///         </c>
///         .
///     </para>
///     <para>
///         By using an effect, the handler remains a plain transport concern with no
///         store dependency, and state flows through the standard Redux dispatch pipeline.
///     </para>
/// </remarks>
internal sealed class AuthSimulationSyncEffect
    : SimpleActionEffectBase<SetAuthSimulationProfileAction, AuthSimulationState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthSimulationSyncEffect" /> class.
    /// </summary>
    /// <param name="headersHandler">The handler whose profile is updated.</param>
    public AuthSimulationSyncEffect(
        AuthSimulationHeadersHandler headersHandler
    ) =>
        HeadersHandler = headersHandler;

    private AuthSimulationHeadersHandler HeadersHandler { get; }

    /// <inheritdoc />
    public override Task HandleAsync(
        SetAuthSimulationProfileAction action,
        AuthSimulationState currentState,
        CancellationToken cancellationToken
    )
    {
        // currentState already reflects the reducer output for this action.
        HeadersHandler.SetProfile(currentState.IsAnonymous, currentState.Roles, currentState.Claims);
        return Task.CompletedTask;
    }
}