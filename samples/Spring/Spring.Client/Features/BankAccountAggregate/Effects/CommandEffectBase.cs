using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Effects;

/// <summary>
///     Abstract base class for aggregate command effects that POST to command APIs.
/// </summary>
/// <typeparam name="TAction">The action type that triggers this effect.</typeparam>
/// <typeparam name="TRequestDto">The DTO type to POST to the API.</typeparam>
/// <remarks>
///     <para>
///         This base class provides the common HTTP POST pattern for aggregate commands:
///         executing → POST → success/failure. All command effects share the same
///         result action types since they update the same aggregate state.
///     </para>
///     <para>
///         <b>Key principle:</b> Effects extract all needed data from the action itself.
///         They do NOT read from state. This keeps effects decoupled from state evolution.
///     </para>
///     <para>
///         <b>Mapping:</b> IMapper&lt;TAction, TRequestDto&gt; converts the action to the
///         request DTO, keeping mapping logic in dedicated mapper classes.
///     </para>
/// </remarks>
[PendingSourceGenerator]
internal abstract class CommandEffectBase<TAction, TRequestDto> : IEffect
    where TAction : IAction
    where TRequestDto : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandEffectBase{TAction, TRequestDto}" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    protected CommandEffectBase(
        HttpClient httpClient,
        IMapper<TAction, TRequestDto> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(mapper);
        Http = httpClient;
        Mapper = mapper;
    }

    /// <summary>
    ///     Gets the HTTP client for API calls.
    /// </summary>
    protected HttpClient Http { get; }

    /// <summary>
    ///     Gets the mapper for action-to-DTO conversion.
    /// </summary>
    protected IMapper<TAction, TRequestDto> Mapper { get; }

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

        yield return new CommandExecutingAction();
        OperationResultDto? result = null;
        string? errorMessage = null;
        try
        {
            string endpoint = GetEndpoint(typedAction);
            TRequestDto requestBody = Mapper.Map(typedAction);
            using HttpResponseMessage response = await Http.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
            result = await response.Content.ReadFromJsonAsync<OperationResultDto>(cancellationToken);
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
            yield return new CommandFailedAction("HttpError", errorMessage);
            yield break;
        }

        if (result is null)
        {
            yield return new CommandFailedAction("NoResponse", "No response from server.");
            yield break;
        }

        if (!result.Success)
        {
            yield return new CommandFailedAction(result.ErrorCode ?? "Unknown", result.ErrorMessage ?? "Unknown error");
            yield break;
        }

        yield return new CommandSucceededAction();
    }

    /// <summary>
    ///     Gets the API endpoint for the command. Extract entity ID from the action.
    /// </summary>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>The API endpoint URL.</returns>
    protected abstract string GetEndpoint(
        TAction action
    );
}