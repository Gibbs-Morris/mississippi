using System.Collections.Immutable;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Represents resolved authorization metadata and diagnostics for a generated HTTP endpoint surface.
/// </summary>
public sealed class GeneratedApiAuthorizationModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratedApiAuthorizationModel" /> class.
    /// </summary>
    /// <param name="policy">The policy value, if provided.</param>
    /// <param name="roles">The normalized roles value, if provided.</param>
    /// <param name="authenticationSchemes">The normalized authentication schemes value, if provided.</param>
    /// <param name="hasAuthorize">A value indicating whether an authorize attribute should be emitted.</param>
    /// <param name="hasAllowAnonymous">A value indicating whether an allow-anonymous attribute should be emitted.</param>
    /// <param name="diagnostics">Diagnostics emitted while analyzing metadata.</param>
    public GeneratedApiAuthorizationModel(
        string? policy,
        string? roles,
        string? authenticationSchemes,
        bool hasAuthorize,
        bool hasAllowAnonymous,
        ImmutableArray<Diagnostic> diagnostics
    )
    {
        Policy = policy;
        Roles = roles;
        AuthenticationSchemes = authenticationSchemes;
        HasAuthorize = hasAuthorize;
        HasAllowAnonymous = hasAllowAnonymous;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     Gets the normalized authentication schemes list.
    /// </summary>
    public string? AuthenticationSchemes { get; }

    /// <summary>
    ///     Gets diagnostics emitted while analyzing metadata.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets a value indicating whether an allow-anonymous attribute should be emitted.
    /// </summary>
    public bool HasAllowAnonymous { get; }

    /// <summary>
    ///     Gets a value indicating whether generated authorization metadata exists.
    /// </summary>
    public bool HasAnyAuthorizationMetadata => HasAuthorize || HasAllowAnonymous;

    /// <summary>
    ///     Gets a value indicating whether an authorize attribute should be emitted.
    /// </summary>
    public bool HasAuthorize { get; }

    /// <summary>
    ///     Gets the policy value.
    /// </summary>
    public string? Policy { get; }

    /// <summary>
    ///     Gets the normalized roles list.
    /// </summary>
    public string? Roles { get; }
}