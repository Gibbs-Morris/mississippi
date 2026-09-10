namespace Mississippi.Hosting.Runtime.Abstractions;

/// <summary>
///     Defines stable diagnostics for runtime-specific composition failures.
/// </summary>
/// <remarks>Public so applications and runtime extensions can identify native integration failures.</remarks>
public static class RuntimeBuilderDiagnosticCodes
{
    /// <summary>
    ///     Identifies repeated application of the same runtime configuration.
    /// </summary>
    public const string DuplicateSiloApplication = "MSB102";

    /// <summary>
    ///     Identifies native configuration added after application has begun.
    /// </summary>
    public const string SiloConfigurationAlreadyApplied = "MSB104";

    /// <summary>
    ///     Identifies a native configuration callback that failed before completion.
    /// </summary>
    public const string SiloConfigurationFailed = "MSB103";

    /// <summary>
    ///     Identifies application to a different silo host.
    /// </summary>
    public const string SiloHostMismatch = "MSB101";
}