using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Server-side effect that converts USD deposits to GBP using the Frankfurter API.
/// </summary>
/// <remarks>
///     <para>
///         This effect demonstrates the server-side event effect pattern introduced
///         in this PR. When a <see cref="DollarsDeposited" /> event is persisted,
///         this effect:
///     </para>
///     <list type="number">
///         <item>Logs the USD deposit amount</item>
///         <item>Calls the Frankfurter API to get the current USD/GBP exchange rate</item>
///         <item>Calculates the GBP equivalent</item>
///         <item>Logs the exchange rate and converted amount</item>
///         <item>Yields a <see cref="ConvertedDollarsDeposited" /> event</item>
///     </list>
///     <para>
///         The yielded event is persisted immediately, allowing projections to
///         update in real-time with the converted balance.
///     </para>
/// </remarks>
internal sealed class CurrencyConversionEffect : EventEffectBase<DollarsDeposited, BankAccountAggregate>
{
    /// <summary>
    ///     The Frankfurter API endpoint for USD to GBP exchange rates.
    /// </summary>
    /// <remarks>
    ///     This is intentionally hardcoded for demonstration purposes.
    ///     Production code should use configuration options.
    /// </remarks>
#pragma warning disable S1075 // URIs should not be hardcoded - Demo code; production would use configuration
    private static readonly Uri FrankfurterApiUri = new("https://api.frankfurter.dev/v1/latest?base=USD&symbols=GBP");
#pragma warning restore S1075

    /// <summary>
    ///     Initializes a new instance of the <see cref="CurrencyConversionEffect" /> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger for effect diagnostics.</param>
    public CurrencyConversionEffect(
        IHttpClientFactory httpClientFactory,
        ILogger<CurrencyConversionEffect> logger
    )
    {
        HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IHttpClientFactory HttpClientFactory { get; }

    private ILogger<CurrencyConversionEffect> Logger { get; }

    /// <inheritdoc />
    public override IAsyncEnumerable<object> HandleAsync(
        DollarsDeposited eventData,
        BankAccountAggregate currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return HandleCoreAsync(eventData, cancellationToken);
    }

    private async IAsyncEnumerable<object> HandleCoreAsync(
        DollarsDeposited eventData,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        Logger.LogDollarsDeposited(eventData.AmountUsd);

        // Fetch exchange rate from Frankfurter API
        using HttpClient httpClient = HttpClientFactory.CreateClient();
        FrankfurterResponse? response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(
            FrankfurterApiUri,
            cancellationToken);
        if (response?.Rates?.Gbp is null)
        {
            Logger.LogExchangeRateFetchFailed();
            yield break;
        }

        decimal exchangeRate = response.Rates.Gbp;
        decimal amountGbp = Math.Round(eventData.AmountUsd * exchangeRate, 2);
        Logger.LogExchangeRateApplied(eventData.AmountUsd, exchangeRate, amountGbp, response.Date);
        yield return new ConvertedDollarsDeposited
        {
            AmountUsd = eventData.AmountUsd,
            AmountGbp = amountGbp,
            ExchangeRate = exchangeRate,
            RateDate = response.Date,
        };
    }

    /// <summary>
    ///     Response model for Frankfurter API.
    /// </summary>
    private sealed class FrankfurterResponse
    {
        /// <summary>
        ///     Gets or sets the base amount.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        ///     Gets or sets the base currency.
        /// </summary>
        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the rate date.
        /// </summary>
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the exchange rates.
        /// </summary>
        [JsonPropertyName("rates")]
        public RatesModel? Rates { get; set; }
    }

    /// <summary>
    ///     Rates model for Frankfurter API response.
    /// </summary>
    private sealed class RatesModel
    {
        /// <summary>
        ///     Gets or sets the GBP exchange rate.
        /// </summary>
        [JsonPropertyName("GBP")]
        public decimal Gbp { get; set; }
    }
}