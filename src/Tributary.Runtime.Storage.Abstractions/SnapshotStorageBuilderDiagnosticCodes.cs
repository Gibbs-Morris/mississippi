namespace Mississippi.Tributary.Runtime.Storage.Abstractions;

/// <summary>
///     Defines stable diagnostics for Snapshot Cosmos storage composition.
/// </summary>
/// <remarks>Public so applications can identify configuration failures without parsing messages.</remarks>
public static class SnapshotStorageBuilderDiagnosticCodes
{
    /// <summary>Identifies configuration through a closed Snapshot Cosmos scope.</summary>
    public const string ConfigurationScopeClosed = "MSB406";

    /// <summary>Identifies an empty Cosmos container identifier.</summary>
    public const string ContainerIdRequired = "MSB401";

    /// <summary>Identifies a missing keyed Cosmos client registration.</summary>
    public const string CosmosClientRegistrationRequired = "MSB405";

    /// <summary>Identifies an empty keyed Cosmos client service key.</summary>
    public const string CosmosClientServiceKeyRequired = "MSB402";

    /// <summary>Identifies an empty Cosmos database identifier.</summary>
    public const string DatabaseIdRequired = "MSB403";

    /// <summary>Identifies more than one Snapshot Cosmos composition for the same runtime.</summary>
    public const string DuplicateComposition = "MSB407";

    /// <summary>Identifies an unsupported Cosmos query page size.</summary>
    public const string QueryBatchSizeInvalid = "MSB404";
}