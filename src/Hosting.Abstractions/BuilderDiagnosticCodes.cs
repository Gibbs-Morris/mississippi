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
    ///     Identifies a second attachment attempted for the same host role.
    /// </summary>
    public const string DuplicateHostAttachment = "MSB001";
}