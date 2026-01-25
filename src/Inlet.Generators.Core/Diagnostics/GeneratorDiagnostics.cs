using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Diagnostics;

/// <summary>
///     Provides diagnostic descriptors for Mississippi source generators.
/// </summary>
/// <remarks>
///     <para>Diagnostic codes are structured as follows:</para>
///     <list type="bullet">
///         <item>MG001-MG009: Reserved for structural/syntax errors.</item>
///         <item>MG010-MG019: Authorization and security configuration.</item>
///         <item>MG020-MG029: Opt-out and generation configuration.</item>
///         <item>MG030-MG039: Naming and convention warnings.</item>
///     </list>
/// </remarks>
#pragma warning disable RS2008 // Enable analyzer release tracking - this is a generator, not an analyzer
#pragma warning disable S1075 // Hardcoded URI is appropriate for help link base
public static class GeneratorDiagnostics
{
    private const string AnonymousAllowedMessage = "'{0}' allows anonymous access via AllowAnonymous = true";

    private const string Category = "Mississippi.Generators";

    private const string ConflictingAuthMessage =
        "'{0}' has AllowAnonymous = true but also specifies authorization settings";

    private const string HelpLinkBaseUri = "https://docs.mississippi.dev/diagnostics/";

    private const string UnsecuredCommandMessage =
        "Command '{0}' on aggregate '{1}' has no authorization; set AuthorizeRoles, AuthorizePolicy, or RequiresAuthentication";

    private const string UnsecuredProjectionMessage =
        "Projection '{0}' has no authorization; set AuthorizeRoles, AuthorizePolicy, or RequiresAuthentication";

    /// <summary>
    ///     MG010: RequireSecureEndpoints violation - command endpoint has no authorization.
    /// </summary>
    /// <remarks>
    ///     <para>Reported when:</para>
    ///     <list type="bullet">
    ///         <item>[RequireSecureEndpoints] is present at assembly level.</item>
    ///         <item>A command has AllowAnonymous = true and TreatAnonymousAsError = true.</item>
    ///         <item>A command has no authorization (no roles, policy, or RequiresAuthentication).</item>
    ///     </list>
    /// </remarks>
    public static readonly DiagnosticDescriptor UnsecuredCommandEndpoint = new(
        "MG010",
        "Command endpoint requires authorization",
        UnsecuredCommandMessage,
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: HelpLinkBaseUri + "MG010");

    /// <summary>
    ///     MG011: RequireSecureEndpoints violation - projection endpoint has no authorization.
    /// </summary>
    /// <remarks>
    ///     <para>Reported when:</para>
    ///     <list type="bullet">
    ///         <item>[RequireSecureEndpoints] is present at assembly level.</item>
    ///         <item>A projection has AllowAnonymous = true and TreatAnonymousAsError = true.</item>
    ///         <item>A projection has no authorization (no roles, policy, or RequiresAuthentication).</item>
    ///     </list>
    /// </remarks>
    public static readonly DiagnosticDescriptor UnsecuredProjectionEndpoint = new(
        "MG011",
        "Projection endpoint requires authorization",
        UnsecuredProjectionMessage,
        Category,
        DiagnosticSeverity.Error,
        true,
        helpLinkUri: HelpLinkBaseUri + "MG011");

    /// <summary>
    ///     MG012: Conflicting authorization settings - AllowAnonymous with authorization attributes.
    /// </summary>
    /// <remarks>
    ///     Reported when a command or projection sets AllowAnonymous = true alongside
    ///     AuthorizeRoles, AuthorizePolicy, or RequiresAuthentication = true.
    /// </remarks>
    public static readonly DiagnosticDescriptor ConflictingAuthorizationSettings = new(
        "MG012",
        "Conflicting authorization settings",
        ConflictingAuthMessage,
        Category,
        DiagnosticSeverity.Warning,
        true,
        helpLinkUri: HelpLinkBaseUri + "MG012");

    /// <summary>
    ///     MG013: Anonymous endpoint in secure assembly - informational when allowed.
    /// </summary>
    /// <remarks>
    ///     Reported as info when [RequireSecureEndpoints(TreatAnonymousAsError = false)]
    ///     is present and an endpoint uses AllowAnonymous = true.
    /// </remarks>
    public static readonly DiagnosticDescriptor AnonymousEndpointAllowed = new(
        "MG013",
        "Anonymous endpoint explicitly allowed",
        AnonymousAllowedMessage,
        Category,
        DiagnosticSeverity.Info,
        true,
        helpLinkUri: HelpLinkBaseUri + "MG013");
}
#pragma warning restore S1075
#pragma warning restore RS2008