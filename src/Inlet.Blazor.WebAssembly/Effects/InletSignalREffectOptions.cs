namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Options for configuring the <see cref="InletSignalREffect" />.
/// </summary>
public sealed class InletSignalREffectOptions
{
    /// <summary>
    ///     Gets or sets the path to the Inlet SignalR hub.
    /// </summary>
    /// <remarks>
    ///     Defaults to "/hubs/inlet".
    /// </remarks>
    public string HubPath { get; set; } = "/hubs/inlet";
}
