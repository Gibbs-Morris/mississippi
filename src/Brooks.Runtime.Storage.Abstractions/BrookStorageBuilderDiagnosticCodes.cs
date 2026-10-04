namespace Mississippi.Brooks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines stable diagnostics for Brooks storage composition.
/// </summary>
/// <remarks>Public so applications can identify configuration failures without parsing messages.</remarks>
public static class BrookStorageBuilderDiagnosticCodes
{
    /// <summary>Identifies a missing keyed Blob client registration.</summary>
    public const string BlobClientRegistrationRequired = "MSB311";

    /// <summary>Identifies configuration through a closed Brooks storage scope.</summary>
    public const string ConfigurationScopeClosed = "MSB312";

    /// <summary>Identifies an empty Cosmos container identifier.</summary>
    public const string ContainerIdRequired = "MSB301";

    /// <summary>Identifies a missing keyed Cosmos client registration.</summary>
    public const string CosmosClientRegistrationRequired = "MSB310";

    /// <summary>Identifies an empty keyed Cosmos client service key.</summary>
    public const string CosmosClientServiceKeyRequired = "MSB302";

    /// <summary>Identifies an empty Cosmos database identifier.</summary>
    public const string DatabaseIdRequired = "MSB303";

    /// <summary>Identifies more than one Brooks storage composition for the same runtime.</summary>
    public const string DuplicateComposition = "MSB313";

    /// <summary>Identifies an unsupported Blob lease duration.</summary>
    public const string LeaseDurationInvalid = "MSB306";

    /// <summary>Identifies an unsupported Blob lease renewal threshold.</summary>
    public const string LeaseRenewalThresholdInvalid = "MSB307";

    /// <summary>Identifies an empty Blob lock container name.</summary>
    public const string LockContainerNameRequired = "MSB304";

    /// <summary>Identifies an invalid maximum events per batch value.</summary>
    public const string MaxEventsPerBatchInvalid = "MSB308";

    /// <summary>Identifies a request-size limit that cannot admit the batch envelope.</summary>
    public const string MaxRequestSizeInvalid = "MSB309";

    /// <summary>Identifies an unsupported Cosmos query page size.</summary>
    public const string QueryBatchSizeInvalid = "MSB305";
}