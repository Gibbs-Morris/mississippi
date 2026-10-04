namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Configuration options for Inlet Server.
/// </summary>
public sealed class InletServerOptions
{
    /// <summary>
    ///     Gets or sets authorization settings for source-generated HTTP APIs.
    /// </summary>
    public GeneratedApiAuthorizationOptions GeneratedApiAuthorization { get; set; } = new();
}