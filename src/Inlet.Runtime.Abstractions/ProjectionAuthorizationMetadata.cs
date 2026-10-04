namespace Mississippi.Inlet.Runtime.Abstractions;

/// <summary>
///     Authorization metadata resolved for a projection path.
/// </summary>
/// <param name="Policy">The optional policy name from <c>[GenerateAuthorization]</c>.</param>
/// <param name="Roles">The optional roles from <c>[GenerateAuthorization]</c>.</param>
/// <param name="AuthenticationSchemes">The optional authentication schemes from <c>[GenerateAuthorization]</c>.</param>
/// <param name="HasAuthorize">Indicates whether <c>[GenerateAuthorization]</c> was present.</param>
/// <param name="HasAllowAnonymous">Indicates whether <c>[GenerateAllowAnonymous]</c> was present.</param>
public sealed record ProjectionAuthorizationMetadata(
    string? Policy,
    string? Roles,
    string? AuthenticationSchemes,
    bool HasAuthorize,
    bool HasAllowAnonymous
)
{
    /// <summary>
    ///     Gets a value indicating whether any authorization metadata was discovered for the projection.
    /// </summary>
    public bool HasAnyAuthorizationMetadata => HasAuthorize || HasAllowAnonymous;
}