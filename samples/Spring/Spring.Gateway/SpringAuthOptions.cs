namespace Mississippi.Spring.Gateway;

/// <summary>
///     Configuration options for optional Spring sample authentication.
/// </summary>
public sealed class SpringAuthOptions
{
    /// <summary>
    ///     Gets or sets the header used to force anonymous requests in development.
    /// </summary>
    public string AnonymousHeader { get; set; } = "X-Spring-Anonymous";

    /// <summary>
    ///     Gets or sets the header used to override user claims for local requests.
    ///     Claims use the format <c>type=value</c> and are delimited by commas or semicolons.
    /// </summary>
    public string ClaimsHeader { get; set; } = "X-Spring-Claims";

    /// <summary>
    ///     Gets or sets default claims when no claims header is provided.
    ///     Claims use the format <c>type=value</c> and are delimited by commas or semicolons.
    /// </summary>
    public string DefaultClaims { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the default roles when no roles header is provided.
    ///     Roles are comma-separated.
    /// </summary>
    public string DefaultRoles { get; set; } = "banking-operator,transfer-operator";

    /// <summary>
    ///     Gets or sets the default user identifier when no header is provided.
    /// </summary>
    public string DefaultUserId { get; set; } = "spring-local-user";

    /// <summary>
    ///     Gets or sets a value indicating whether authentication and authorization are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Gets or sets the header used to override user roles for local requests.
    /// </summary>
    public string RolesHeader { get; set; } = "X-Spring-Roles";

    /// <summary>
    ///     Gets or sets the default authentication scheme name.
    /// </summary>
    public string Scheme { get; set; } = "SpringLocalDev";

    /// <summary>
    ///     Gets or sets the header used to override user id for local requests.
    /// </summary>
    public string UserHeader { get; set; } = "X-Spring-User";
}