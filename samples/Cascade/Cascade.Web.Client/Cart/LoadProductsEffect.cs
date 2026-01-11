using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Web.Client.Cart;

/// <summary>
///     Effect that loads available products from the API.
/// </summary>
internal sealed class LoadProductsEffect : IEffect
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LoadProductsEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    public LoadProductsEffect(
        HttpClient httpClient
    ) =>
        Http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private HttpClient Http { get; }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    ) =>
        action is LoadProductsAction;

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // Signal that loading has started
        yield return new ProductsLoadingAction();

        string[]? products = null;
        string? errorMessage = null;

        try
        {
            products = await Http.GetFromJsonAsync<string[]>("api/products", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = ex.Message;
        }

        if (errorMessage is not null)
        {
            yield return new ProductsLoadFailedAction(errorMessage);
            yield break;
        }

        if (products is null)
        {
            yield return new ProductsLoadFailedAction("No products returned from API");
            yield break;
        }

        yield return new ProductsLoadedAction([.. products]);
    }
}
