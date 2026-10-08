namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Options for configuring the <see cref="InletSignalRActionEffect" />.
/// </summary>
public sealed class InletSignalRActionEffectOptions
{
    /// <summary>
    ///     Gets or sets the path to the Inlet SignalR hub.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>hubs/inlet</c>, relative to the application navigation base.
    /// </remarks>
    public string HubPath { get; set; } = "hubs/inlet";
}