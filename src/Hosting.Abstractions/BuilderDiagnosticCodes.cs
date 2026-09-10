namespace Mississippi.Hosting.Abstractions;

/// <summary>
///     Defines stable diagnostic codes for Mississippi builder validation.
/// </summary>
/// <remarks>Public so applications and builder extensions can identify failures without copying string literals.</remarks>
public static class BuilderDiagnosticCodes
{
    /// <summary>
    ///     Identifies configuration attempted through a builder that has already attached.
    /// </summary>
    public const string BuilderAlreadyAttached = "MSB002";

    /// <summary>
    ///     Identifies configuration attempted through a scope that closed without attaching.
    /// </summary>
    public const string ConfigurationScopeClosed = "MSB003";

    /// <summary>
    ///     Identifies a second attachment attempted for the same host role.
    /// </summary>
    public const string DuplicateHostAttachment = "MSB001";

    /// <summary>
    ///     Identifies direct host service changes made while its staged composition callback was running.
    /// </summary>
    public const string HostServicesChanged = "MSB004";

    /// <summary>
    ///     Identifies a host whose service graph could not be restored after failed publication.
    /// </summary>
    public const string HostServicesDamaged = "MSB006";

    /// <summary>
    ///     Identifies a host service collection that cannot accept terminal composition.
    /// </summary>
    public const string HostServicesReadOnly = "MSB005";
}