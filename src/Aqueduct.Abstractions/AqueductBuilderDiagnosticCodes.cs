namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Defines stable diagnostics for Aqueduct composition.
/// </summary>
/// <remarks>Public so applications can identify configuration failures without parsing messages.</remarks>
public static class AqueductBuilderDiagnosticCodes
{
    /// <summary>Identifies an empty broadcast namespace.</summary>
    public const string BroadcastNamespaceRequired = "MSB203";

    /// <summary>Identifies configuration through a closed Aqueduct scope.</summary>
    public const string ConfigurationScopeClosed = "MSB206";

    /// <summary>Identifies more than one Aqueduct composition for the same runtime.</summary>
    public const string DuplicateComposition = "MSB207";

    /// <summary>Identifies a nonpositive heartbeat interval.</summary>
    public const string InvalidHeartbeatInterval = "MSB204";

    /// <summary>Identifies a nonpositive dead-server timeout multiplier.</summary>
    public const string InvalidTimeoutMultiplier = "MSB205";

    /// <summary>Identifies an empty server-targeted namespace.</summary>
    public const string ServerNamespaceRequired = "MSB202";

    /// <summary>Identifies an empty stream-provider name.</summary>
    public const string StreamProviderRequired = "MSB201";
}