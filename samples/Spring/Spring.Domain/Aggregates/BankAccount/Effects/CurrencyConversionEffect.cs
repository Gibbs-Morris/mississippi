using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
///     <para>
///         <strong>Production Considerations:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Configure the API URI via <see cref="CurrencyConversionOptions" /> for environment-specific endpoints.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Consider adding retry/circuit-breaker patterns using Polly or
///                 Microsoft.Extensions.Http.Resilience for transient fault handling.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Monitor conversion failures and consider compensating events for failed conversions.
///             </description>
///         </item>
///     </list>
/// </remarks>
internal sealed class CurrencyConversionEffect : EventEffectBase<DollarsDeposited, BankAccountAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CurrencyConversionEffect" /> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="options">Configuration options for currency conversion.</param>
    /// <param name="logger">Logger for effect diagnostics.</param>
    public CurrencyConversionEffect(
        IHttpClientFactory httpClientFactory,
        IOptions<CurrencyConversionOptions> options,
        ILogger<CurrencyConversionEffect> logger
    )
    {
        HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IHttpClientFactory HttpClientFactory { get; }

    private ILogger<CurrencyConversionEffect> Logger { get; }

    private CurrencyConversionOptions Options { get; }

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

        // Build the API URI from configuration
        Uri apiUri = new(
            Options.FrankfurterApiBaseUri,
            $"latest?base={Options.SourceCurrency}&symbols={Options.TargetCurrency}");

        FrankfurterResponse? response;
        try
        {
            // Fetch exchange rate from Frankfurter API.
            // For production, consider adding resilience via Microsoft.Extensions.Http.Resilience
            // or Polly for retry/circuit-breaker patterns.
            using HttpClient httpClient = HttpClientFactory.CreateClient();
            response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(
                apiUri,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogExchangeRateFetchHttpError(ex);
            yield break;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (TaskCanceledException not from our token)
            Logger.LogExchangeRateFetchFailed();
            yield break;
        }

        // Validate response structure
        if (response is null)
        {
            Logger.LogExchangeRateResponseInvalid("Response was null");
            yield break;
        }

        if (response.Rates is null)
        {
            Logger.LogExchangeRateResponseInvalid("Rates object was null");
            yield break;
        }

        if (response.Rates.Gbp <= 0)
        {
            Logger.LogExchangeRateResponseInvalid($"GBP rate was invalid: {response.Rates.Gbp}");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(response.Date))
        {
            Logger.LogExchangeRateResponseInvalid("Rate date was missing");
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