namespace Mississippi.Sdk.Silo;

/// <summary>
///     Options for configuring Mississippi silo applications.
/// </summary>
public sealed class MississippiSiloOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether the Orleans dashboard is enabled.
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    ///     Gets or sets the Orleans stream provider name used for Mississippi streaming.
    /// </summary>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}