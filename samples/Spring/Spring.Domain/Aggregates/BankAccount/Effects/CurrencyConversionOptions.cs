using System;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Configuration options for the currency conversion effect.
/// </summary>
/// <remarks>
///     <para>
///         This options class demonstrates the recommended pattern for configuring
///         external service dependencies in effects. The defaults are suitable for
///         development and demonstration purposes.
///     </para>
///     <para>
///         Production usage should configure these values via appsettings.json
///         or environment variables.
///     </para>
/// </remarks>
public sealed class CurrencyConversionOptions
{
    /// <summary>
    ///     The configuration section name for binding.
    /// </summary>
    public const string SectionName = "CurrencyConversion";

    /// <summary>
    ///     Gets or sets the base URI for the Frankfurter API.
    /// </summary>
    /// <remarks>
    ///     Defaults to the public Frankfurter API. Override for testing or
    ///     to use an alternative exchange rate provider.
    /// </remarks>
    public Uri FrankfurterApiBaseUri { get; set; } = new("https://api.frankfurter.dev/v1/");

    /// <summary>
    ///     Gets or sets the source currency code.
    /// </summary>
    public string SourceCurrency { get; set; } = "USD";

    /// <summary>
    ///     Gets or sets the target currency code.
    /// </summary>
    public string TargetCurrency { get; set; } = "GBP";
}
