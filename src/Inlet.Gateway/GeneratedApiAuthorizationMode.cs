namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Controls authorization behavior for source-generated HTTP APIs.
/// </summary>
public enum GeneratedApiAuthorizationMode
{
    /// <summary>
    ///     Leaves generated HTTP APIs unchanged unless generation attributes explicitly request authorization metadata.
    /// </summary>
    Disabled = 0,

    /// <summary>
    ///     Applies default authorization requirements to all source-generated HTTP APIs.
    /// </summary>
    RequireAuthorizationForAllGeneratedEndpoints = 1,
}