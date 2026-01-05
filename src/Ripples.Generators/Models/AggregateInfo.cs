namespace Mississippi.Ripples.Generators.Models;

/// <summary>
///     Contains extracted information about an aggregate interface marked with [UxAggregate].
/// </summary>
internal sealed class AggregateInfo
{
    /// <summary>
    ///     Gets or sets the derived aggregate name (without "I" prefix and "Grain" suffix).
    /// </summary>
    public string AggregateName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the authorization policy name, if any.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets the commands exposed by this aggregate.
    /// </summary>
    public CommandInfo[] Commands { get; set; } = [];

    /// <summary>
    ///     Gets or sets the fully qualified type name of the aggregate interface.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the simple interface name (without namespace, with "I" prefix).
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace of the aggregate interface.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the base HTTP route for aggregate commands.
    /// </summary>
    public string Route { get; set; } = string.Empty;
}