using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Spring.Client.Features.Greet.Actions;


namespace Spring.Client.Features.Greet.Effects;

/// <summary>
///     Effect that handles greeting requests by calling the API.
/// </summary>
internal sealed class GreetEffect : IEffect
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GreetEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    public GreetEffect(
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
        action is GreetRequestedAction;

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (action is not GreetRequestedAction greetAction)
        {
            yield break;
        }

        // Signal loading state
        yield return new GreetLoadingAction();

        // Store result outside try block so we can yield after error handling
        GreetResultDto? result = null;
        string? errorMessage = null;

        try
        {
            result = await Http.GetFromJsonAsync<GreetResultDto>(
                $"/api/greet/{Uri.EscapeDataString(greetAction.Name)}",
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
        catch (TaskCanceledException ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
        catch (JsonException ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }

        // Yield error if captured
        if (errorMessage is not null)
        {
            yield return new GreetFailedAction(errorMessage);
            yield break;
        }

        // Yield success or null-response error
        if (result is null)
        {
            yield return new GreetFailedAction("Error: No response from server");
            yield break;
        }

        yield return new GreetSucceededAction(result.Greeting, result.GeneratedAt);
    }
}
