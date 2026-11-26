using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Client.Abstractions.ActionEffects;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Adds response diagnostics to the shared command action effect lifecycle.
/// </summary>
/// <typeparam name="TAction">The command action type.</typeparam>
/// <typeparam name="TRequestDto">The request DTO type.</typeparam>
/// <typeparam name="TState">The feature state type.</typeparam>
/// <typeparam name="TExecutingAction">The executing lifecycle action type.</typeparam>
/// <typeparam name="TSucceededAction">The succeeded lifecycle action type.</typeparam>
/// <typeparam name="TFailedAction">The failed lifecycle action type.</typeparam>
/// <remarks>
///     This implementation base is public because generated command and saga effects in application
///     assemblies inherit it. Logging infrastructure stays in the client runtime, while the existing
///     abstraction remains available for consumers that provide their own diagnostics.
/// </remarks>
public abstract class LoggingCommandActionEffectBase<TAction, TRequestDto, TState, TExecutingAction, TSucceededAction,
    TFailedAction>
    : CommandActionEffectBase<TAction, TRequestDto, TState, TExecutingAction, TSucceededAction, TFailedAction>
    where TAction : ICommandAction
    where TRequestDto : class
    where TState : class, IFeatureState
    where TExecutingAction : ICommandExecutingAction<TExecutingAction>
    where TSucceededAction : ICommandSucceededAction<TSucceededAction>
    where TFailedAction : ICommandFailedAction<TFailedAction>
{
    /// <summary>
    ///     Initializes a new instance of the
    ///     <see
    ///         cref="LoggingCommandActionEffectBase{TAction, TRequestDto, TState, TExecutingAction, TSucceededAction, TFailedAction}" />
    ///     class for externally derived command effects.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    /// <param name="timeProvider">The optional clock for timestamps.</param>
    /// <param name="logger">The optional command action logger.</param>
    protected LoggingCommandActionEffectBase(
        HttpClient httpClient,
        IMapper<TAction, TRequestDto> mapper,
        TimeProvider? timeProvider = null,
        ILogger<TAction>? logger = null
    )
        : base(httpClient, mapper, timeProvider) =>
        Logger = logger ?? NullLogger<TAction>.Instance;

    private ILogger<TAction> Logger { get; }

    /// <inheritdoc />
    protected override void OnInvalidResponse(
        Exception exception,
        string commandId,
        string commandType,
        int statusCode
    ) =>
        Logger.InvalidCommandResponse(exception, commandId, commandType, statusCode);
}