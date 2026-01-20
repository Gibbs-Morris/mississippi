using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Spring.Client.Features.BankAccountBalance.Actions;
using Spring.Client.Features.BankAccountBalance.Dtos;


namespace Spring.Client.Features.BankAccountBalance.Effects;

/// <summary>
///     Effect that handles fetching the BankAccountBalance projection data.
/// </summary>
[PendingSourceGenerator]
internal sealed class FetchBankAccountBalanceEffect : IEffect
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FetchBankAccountBalanceEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    public FetchBankAccountBalanceEffect(
        HttpClient httpClient
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        Http = httpClient;
    }

    private HttpClient Http { get; }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    ) =>
        action is FetchBankAccountBalanceAction;

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (action is not FetchBankAccountBalanceAction fetchAction)
        {
            yield break;
        }

        yield return new BankAccountBalanceLoadingAction();
        BankAccountBalanceDto? result = null;
        string? errorMessage = null;
        try
        {
            result = await Http.GetFromJsonAsync<BankAccountBalanceDto>(
                $"/api/projections/bank-account-balance/{Uri.EscapeDataString(fetchAction.AccountId)}",
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Network error: {ex.Message}";
        }
        catch (TaskCanceledException ex)
        {
            errorMessage = $"Request cancelled: {ex.Message}";
        }
        catch (JsonException ex)
        {
            errorMessage = $"Parse error: {ex.Message}";
        }

        if (errorMessage is not null)
        {
            yield return new BankAccountBalanceFetchFailedAction(errorMessage);
            yield break;
        }

        if (result is null)
        {
            yield return new BankAccountBalanceFetchFailedAction("No response from server.");
            yield break;
        }

        yield return new BankAccountBalanceLoadedAction(
            fetchAction.AccountId,
            result.Balance,
            result.HolderName,
            result.IsOpen);
    }
}
