using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Effects;

/// <summary>
///     Abstract base class for aggregate command effects that POST to command APIs.
/// </summary>
/// <typeparam name="TAction">The action type that triggers this effect. Must implement <see cref="ICommandAction" />.</typeparam>
/// <typeparam name="TRequestDto">The DTO type to POST to the API.</typeparam>
/// <typeparam name="TExecutingAction">The executing lifecycle action type.</typeparam>
/// <typeparam name="TSucceededAction">The succeeded lifecycle action type.</typeparam>
/// <typeparam name="TFailedAction">The failed lifecycle action type.</typeparam>
/// <remarks>
///     <para>
///         This base class provides the common HTTP POST pattern for aggregate commands:
///         executing → POST → success/failure. Each command has its own lifecycle actions
///         to enable per-command tracking and correlation.
///     </para>
///     <para>
///         <b>Key principle:</b> Effects extract all needed data from the action itself.
///         They do NOT read from state. This keeps effects decoupled from state evolution.
///     </para>
///     <para>
///         <b>Lifecycle actions:</b> Use static abstract factory methods for creation,
///         eliminating the need for virtual overrides.
///     </para>
///     <para>
///         Derived types only need to implement <see cref="Route" /> to specify the command
///         endpoint segment. The full URL is constructed as:
///         <c>{AggregateRoutePrefix}/{EntityId}/{Route}</c>.
///     </para>
/// </remarks>
public abstract class
    CommandEffectBase<TAction, TRequestDto, TExecutingAction, TSucceededAction, TFailedAction> : IEffect
    where TAction : ICommandAction
    where TRequestDto : class
    where TExecutingAction : ICommandExecutingAction<TExecutingAction>
    where TSucceededAction : ICommandSucceededAction<TSucceededAction>
    where TFailedAction : ICommandFailedAction<TFailedAction>
{
    /// <summary>
    ///     Initializes a new instance of the
    ///     <see cref="CommandEffectBase{TAction, TRequestDto, TExecutingAction, TSucceededAction, TFailedAction}" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    /// <param name="timeProvider">The time provider for timestamps. If null, uses <see cref="System.TimeProvider.System" />.</param>
    protected CommandEffectBase(
        HttpClient httpClient,
        IMapper<TAction, TRequestDto> mapper,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(mapper);
        Http = httpClient;
        Mapper = mapper;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    ///     Gets the aggregate route prefix (e.g., "/api/aggregates/bank-account").
    /// </summary>
    /// <remarks>
    ///     This should return the base path to the aggregate's command endpoints,
    ///     not including the entity ID or command route segment.
    /// </remarks>
    protected abstract string AggregateRoutePrefix { get; }

    /// <summary>
    ///     Gets the HTTP client for API calls.
    /// </summary>
    protected HttpClient Http { get; }

    /// <summary>
    ///     Gets the mapper for action-to-DTO conversion.
    /// </summary>
    protected IMapper<TAction, TRequestDto> Mapper { get; }

    /// <summary>
    ///     Gets the command route segment (e.g., "deposit", "withdraw", "open").
    /// </summary>
    /// <remarks>
    ///     This is appended to the aggregate route prefix and entity ID to form the full endpoint.
    /// </remarks>
    protected abstract string Route { get; }

    /// <summary>
    ///     Gets the time provider for timestamps.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    ) =>
        action is TAction;

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (action is not TAction typedAction)
        {
            yield break;
        }

        string commandId = Guid.NewGuid().ToString("N");
        string commandType = typeof(TAction).Name;
        yield return TExecutingAction.Create(commandId, commandType, TimeProvider.GetUtcNow());
        OperationResultDto? result = null;
        string? errorMessage = null;
        try
        {
            string endpoint = GetEndpoint(typedAction);
            TRequestDto requestBody = Mapper.Map(typedAction);
            using HttpResponseMessage response = await Http.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

            // Check for non-success status codes before trying to parse response
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                errorMessage = $"Server error ({(int)response.StatusCode}): {responseBody}";
            }
            else
            {
                result = await response.Content.ReadFromJsonAsync<OperationResultDto>(cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Network error: {ex.Message}";
        }
        catch (TaskCanceledException ex)
        {
            errorMessage = $"Request cancelled: {ex.Message}";
        }

        if (errorMessage is not null)
        {
            yield return TFailedAction.Create(commandId, "HttpError", errorMessage, TimeProvider.GetUtcNow());
            yield break;
        }

        if (result is null)
        {
            yield return TFailedAction.Create(
                commandId,
                "NoResponse",
                "No response from server.",
                TimeProvider.GetUtcNow());
            yield break;
        }

        if (!result.Success)
        {
            yield return TFailedAction.Create(
                commandId,
                result.ErrorCode ?? "Unknown",
                result.ErrorMessage ?? "Unknown error",
                TimeProvider.GetUtcNow());
            yield break;
        }

        yield return TSucceededAction.Create(commandId, TimeProvider.GetUtcNow());
    }

    /// <summary>
    ///     Gets the API endpoint for the command by combining the aggregate route prefix,
    ///     entity ID, and command route.
    /// </summary>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>The full API endpoint URL.</returns>
    protected virtual string GetEndpoint(
        TAction action
    ) =>
        $"{AggregateRoutePrefix}/{action.EntityId}/{Route}";
}