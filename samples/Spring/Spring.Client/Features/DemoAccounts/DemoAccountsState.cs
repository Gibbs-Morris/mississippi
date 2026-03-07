using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Feature state for tracking demo account identifiers used by the Spring sample UI.
/// </summary>
internal sealed record DemoAccountsState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "demoAccounts";

    /// <summary>
    ///     Gets the demo account A identifier.
    /// </summary>
    public string? AccountAId { get; init; }

    /// <summary>
    ///     Gets the demo account A display name.
    /// </summary>
    public string? AccountAName { get; init; }

    /// <summary>
    ///     Gets the demo account B identifier.
    /// </summary>
    public string? AccountBId { get; init; }

    /// <summary>
    ///     Gets the demo account B display name.
    /// </summary>
    public string? AccountBName { get; init; }
}