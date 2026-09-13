namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Defines stable diagnostics for Aqueduct composition.
/// </summary>
/// <remarks>Public so applications can identify configuration failures without parsing messages.</remarks>
public static class AqueductBuilderDiagnosticCodes
{
    /// <summary>Identifies configuration through a closed Aqueduct scope.</summary>
    public const string ConfigurationScopeClosed = "MSB206";

    /// <summary>Identifies more than one Aqueduct composition for the same runtime.</summary>
    public const string DuplicateComposition = "MSB207";

    /// <summary>Identifies an empty server-targeted namespace.</summary>
    public const string ServerNamespaceRequired = "MSB202";

    /// <summary>Identifies an empty stream-provider name.</summary>
    public const string StreamProviderRequired = "MSB201";
}