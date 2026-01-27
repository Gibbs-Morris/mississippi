namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Represents authorization configuration for generated endpoints.
/// </summary>
public sealed class AuthorizationInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthorizationInfo" /> class.
    /// </summary>
    /// <param name="allowAnonymous">Whether anonymous access is allowed.</param>
    /// <param name="requiresAuthentication">Whether authentication is required.</param>
    /// <param name="authorizeRoles">The required roles, if any.</param>
    /// <param name="authorizePolicy">The required policy, if any.</param>
    public AuthorizationInfo(
        bool allowAnonymous,
        bool requiresAuthentication,
        string? authorizeRoles,
        string? authorizePolicy
    )
    {
        AllowAnonymous = allowAnonymous;
        RequiresAuthentication = requiresAuthentication;
        AuthorizeRoles = authorizeRoles;
        AuthorizePolicy = authorizePolicy;
    }

    /// <summary>
    ///     Gets a value indicating whether anonymous access is allowed.
    /// </summary>
    public bool AllowAnonymous { get; }

    /// <summary>
    ///     Gets the required authorization policy, if any.
    /// </summary>
    public string? AuthorizePolicy { get; }

    /// <summary>
    ///     Gets the required authorization roles, if any.
    /// </summary>
    public string? AuthorizeRoles { get; }

    /// <summary>
    ///     Gets a value indicating whether any authorization is configured.
    /// </summary>
    public bool HasAuthorization =>
        AllowAnonymous || RequiresAuthentication ||
        !string.IsNullOrEmpty(AuthorizeRoles) || !string.IsNullOrEmpty(AuthorizePolicy);

    /// <summary>
    ///     Gets a value indicating whether authentication (but not specific roles/policy) is required.
    /// </summary>
    public bool RequiresAuthentication { get; }

    /// <summary>
    ///     Gets an empty authorization info with no configuration.
    /// </summary>
    public static AuthorizationInfo None { get; } = new(false, false, null, null);
}
