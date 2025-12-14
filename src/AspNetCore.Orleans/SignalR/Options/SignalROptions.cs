namespace Mississippi.AspNetCore.Orleans.SignalR.Options;

/// <summary>
///     Configuration options for Orleans-backed SignalR scale-out.
/// </summary>
public sealed class SignalROptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether to enable backplane messaging.
    ///     Default: true.
    /// </summary>
    public bool EnableBackplane { get; set; } = true;

    /// <summary>
    ///     Gets or sets the Orleans stream provider name for SignalR messaging.
    ///     Default: "SignalRStream".
    /// </summary>
    public string StreamProviderName { get; set; } = "SignalRStream";
}