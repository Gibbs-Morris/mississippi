using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client.Abstractions.ActionEffects;

/// <summary>
///     Abstract base class for saga start action effects that POST to saga APIs.
/// </summary>
/// <typeparam name="TAction">
///     The action type that triggers this action effect. Must implement
///     <see cref="ISagaAction" />.
/// </typeparam>
/// <typeparam name="TState">The feature state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         This base class provides the common HTTP POST pattern for saga start requests:
///         executing → POST → success/failure. Each saga has its own lifecycle actions
///         to enable per-saga tracking and correlation.
///     </para>
///     <para>
///         <b>Key principle:</b> Action effects extract all needed data from the action itself.
///         They do NOT read from state. This keeps action effects decoupled from state evolution.
///         The state parameter is provided for pattern alignment but is intentionally ignored.
///     </para>
///     <para>
///         <b>Lifecycle actions:</b> Use static abstract factory methods for creation,
///         eliminating the need for virtual overrides.
///     </para>
///     <para>
///         Derived types only need to implement <see cref="SagaRoute" /> to specify the saga
///         endpoint segment. The full URL is constructed as:
///         <c>/api/sagas/{SagaRoute}/{SagaId}</c>.
///     </para>
/// </remarks>
public abstract class SagaActionEffectBase<TAction, TState> : IActionEffect<TState>
    where TAction : ISagaAction
    where TState : class, IFeatureState
{
    /// <summary>
    ///     Initializes a new instance of the
    ///     <see
    ///         cref="SagaActionEffectBase{TAction, TState}" />
    ///     class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="timeProvider">The time provider for timestamps. If null, uses <see cref="System.TimeProvider.System" />.</param>
    protected SagaActionEffectBase(
        HttpClient httpClient,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        Http = httpClient;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    ///     Gets the HTTP client for API calls.
    /// </summary>
    protected HttpClient Http { get; }

    /// <summary>
    ///     Gets the saga route segment (e.g., "transfer-funds").
    /// </summary>
    /// <remarks>
    ///     This is used to construct the full saga endpoint URL.
    /// </remarks>
    protected abstract string SagaRoute { get; }

    /// <summary>
    ///     Gets the saga type name for lifecycle action creation.
    /// </summary>
    protected virtual string SagaTypeName =>
        typeof(TAction).Name.Replace("Action", string.Empty, StringComparison.Ordinal);

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
    /// <remarks>
    ///     <para>
    ///         The <paramref name="currentState" /> parameter is provided for interface consistency but
    ///         should not be read in saga effects. Per the "action effects extract all needed data
    ///         from the action itself" principle, effects must remain pure observers of actions.
    ///     </para>
    /// </remarks>
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        TState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // Discard currentState - effects extract all needed data from the action itself.
        _ = currentState;
        if (action is not TAction typedAction)
        {
            yield break;
        }

        Guid sagaId = typedAction.SagaId;
        string sagaType = SagaTypeName;
        yield return CreateExecutingAction(sagaId, sagaType, TimeProvider.GetUtcNow());
        string? errorCode = null;
        string? errorMessage = null;
        try
        {
            string endpoint = GetEndpoint(typedAction);
            object requestBody = CreateRequestBody(typedAction);
            using HttpResponseMessage response = await Http.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

            // For saga start, Accepted (202) indicates success
            if (response.StatusCode is not HttpStatusCode.Accepted and not HttpStatusCode.OK)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                errorCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
                errorMessage = $"Server error ({(int)response.StatusCode}): {responseBody}";
            }
        }
        catch (HttpRequestException ex)
        {
            errorCode = "HttpError";
            errorMessage = $"Network error: {ex.Message}";
        }
        catch (TaskCanceledException ex)
        {
            errorCode = "Cancelled";
            errorMessage = $"Request cancelled: {ex.Message}";
        }

        if (errorMessage is not null)
        {
            yield return CreateFailedAction(sagaId, errorCode, errorMessage, TimeProvider.GetUtcNow());
            yield break;
        }

        yield return CreateSucceededAction(sagaId, TimeProvider.GetUtcNow());
    }

    /// <summary>
    ///     Creates the executing lifecycle action.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="timestamp">The timestamp for the action.</param>
    /// <returns>The executing action.</returns>
    protected abstract IAction CreateExecutingAction(
        Guid sagaId,
        string sagaType,
        DateTimeOffset timestamp
    );

    /// <summary>
    ///     Creates the failed lifecycle action.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="errorCode">The error code describing the failure.</param>
    /// <param name="errorMessage">The human-readable failure message.</param>
    /// <param name="timestamp">The timestamp for the action.</param>
    /// <returns>The failed action.</returns>
    protected abstract IAction CreateFailedAction(
        Guid sagaId,
        string? errorCode,
        string errorMessage,
        DateTimeOffset timestamp
    );

    /// <summary>
    ///     Creates the request payload for the saga start call.
    /// </summary>
    /// <param name="action">The saga action containing the request data.</param>
    /// <returns>The request payload to POST.</returns>
    protected abstract object CreateRequestBody(
        TAction action
    );

    /// <summary>
    ///     Creates the succeeded lifecycle action.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="timestamp">The timestamp for the action.</param>
    /// <returns>The succeeded action.</returns>
    protected abstract IAction CreateSucceededAction(
        Guid sagaId,
        DateTimeOffset timestamp
    );

    /// <summary>
    ///     Gets the API endpoint for the saga by combining the saga route and saga ID.
    /// </summary>
    /// <param name="action">The action containing the saga ID.</param>
    /// <returns>The full API endpoint URL.</returns>
    protected virtual string GetEndpoint(
        TAction action
    ) =>
        $"/api/sagas/{SagaRoute}/{action.SagaId}";
}