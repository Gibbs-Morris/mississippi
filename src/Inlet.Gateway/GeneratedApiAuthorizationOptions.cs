namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Configuration options for source-generated HTTP API authorization.
/// </summary>
public sealed class GeneratedApiAuthorizationOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether generated <c>[AllowAnonymous]</c> metadata can opt out of force mode.
    /// </summary>
    public bool AllowAnonymousOptOut { get; set; } = true;

    /// <summary>
    ///     Gets or sets the default authentication schemes for generated authorization metadata.
    /// </summary>
    public string? DefaultAuthenticationSchemes { get; set; }

    /// <summary>
    ///     Gets or sets the default authorization policy for generated authorization metadata.
    /// </summary>
    public string? DefaultPolicy { get; set; }

    /// <summary>
    ///     Gets or sets the default roles for generated authorization metadata.
    /// </summary>
    public string? DefaultRoles { get; set; }

    /// <summary>
    ///     Gets or sets the authorization mode for source-generated HTTP APIs.
    /// </summary>
    public GeneratedApiAuthorizationMode Mode { get; set; } = GeneratedApiAuthorizationMode.Disabled;
}