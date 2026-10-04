using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Hosting.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Configures Brooks Cosmos storage within one runtime composition scope.
/// </summary>
/// <remarks>Public for the AddCosmosBrookStorageProvider callback; closed scopes cannot be configured again.</remarks>
public sealed class CosmosBrookStorageBuilder
{
    /// <summary>Initializes a new instance of the <see cref="CosmosBrookStorageBuilder" /> class.</summary>
    /// <param name="cosmosConnectionString">The optional Cosmos connection string owned by this scope.</param>
    /// <param name="blobStorageConnectionString">The optional Blob connection string owned by this scope.</param>
    internal CosmosBrookStorageBuilder(
        string? cosmosConnectionString = null,
        string? blobStorageConnectionString = null
    )
    {
        CosmosConnectionString = cosmosConnectionString;
        BlobStorageConnectionString = blobStorageConnectionString;
    }

    /// <summary>Gets or sets the Cosmos container identifier for brook events.</summary>
    public string ContainerId
    {
        get => Options.ContainerId;
        set
        {
            ThrowIfClosed();
            Options.ContainerId = value;
        }
    }

    /// <summary>Gets or sets the keyed Cosmos client service key.</summary>
    public string CosmosClientServiceKey
    {
        get => Options.CosmosClientServiceKey;
        set
        {
            ThrowIfClosed();
            Options.CosmosClientServiceKey = value;
        }
    }

    /// <summary>Gets or sets the Cosmos database identifier.</summary>
    public string DatabaseId
    {
        get => Options.DatabaseId;
        set
        {
            ThrowIfClosed();
            Options.DatabaseId = value;
        }
    }

    /// <summary>Gets or sets the finite Blob lease duration in seconds.</summary>
    public int LeaseDurationSeconds
    {
        get => Options.LeaseDurationSeconds;
        set
        {
            ThrowIfClosed();
            Options.LeaseDurationSeconds = value;
        }
    }

    /// <summary>Gets or sets the lease renewal threshold in seconds.</summary>
    public int LeaseRenewalThresholdSeconds
    {
        get => Options.LeaseRenewalThresholdSeconds;
        set
        {
            ThrowIfClosed();
            Options.LeaseRenewalThresholdSeconds = value;
        }
    }

    /// <summary>Gets or sets the Blob container name used for distributed locks.</summary>
    public string LockContainerName
    {
        get => Options.LockContainerName;
        set
        {
            ThrowIfClosed();
            Options.LockContainerName = value;
        }
    }

    /// <summary>Gets or sets the maximum number of events in one batch.</summary>
    public int MaxEventsPerBatch
    {
        get => Options.MaxEventsPerBatch;
        set
        {
            ThrowIfClosed();
            Options.MaxEventsPerBatch = value;
        }
    }

    /// <summary>Gets or sets the maximum estimated request size in bytes.</summary>
    public long MaxRequestSizeBytes
    {
        get => Options.MaxRequestSizeBytes;
        set
        {
            ThrowIfClosed();
            Options.MaxRequestSizeBytes = value;
        }
    }

    /// <summary>Gets or sets the maximum number of events requested per query page, or -1 for dynamic sizing.</summary>
    public int QueryBatchSize
    {
        get => Options.QueryBatchSize;
        set
        {
            ThrowIfClosed();
            Options.QueryBatchSize = value;
        }
    }

    private string? BlobStorageConnectionString { get; }

    private string? CosmosConnectionString { get; }

    private bool IsClosed { get; set; }

    private BrookStorageOptions Options { get; } = new();

    /// <summary>Returns the current options diagnostics without registering services.</summary>
    /// <returns>The current validation diagnostics.</returns>
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (IsClosed)
        {
            return
            [
                new(
                    BrookStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
                    "The Brooks Cosmos storage configuration scope has closed.",
                    "Configure Brooks Cosmos storage inside a fresh AddCosmosBrookStorageProvider(...) callback."),
            ];
        }

        return BrookStorageOptionsValidation.Validate(Options);
    }

    /// <summary>Validates and applies this scope to the staged silo services.</summary>
    /// <param name="silo">The staged silo receiving the provider graph.</param>
    internal void Apply(
        ISiloBuilder silo
    )
    {
        ArgumentNullException.ThrowIfNull(silo);
        IReadOnlyList<BuilderDiagnostic> diagnostics = Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        BrookStorageProviderRegistrations.RegisterCore(
            silo.Services,
            Snapshot(),
            CosmosConnectionString,
            BlobStorageConnectionString);
    }

    /// <summary>Copies configuration values into this nested scope before its final snapshot is taken.</summary>
    /// <param name="configuration">The configuration section containing BrookStorageOptions property names.</param>
    internal void Bind(
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ThrowIfClosed();
        configuration.Bind(Options);
    }

    /// <summary>Closes this scope after its callback has completed.</summary>
    internal void Close() => IsClosed = true;

    /// <summary>Returns an immutable-by-convention copy for deferred service registration.</summary>
    /// <returns>The final BrookStorageOptions snapshot.</returns>
    internal BrookStorageOptions Snapshot() =>
        new()
        {
            ContainerId = Options.ContainerId,
            CosmosClientServiceKey = Options.CosmosClientServiceKey,
            DatabaseId = Options.DatabaseId,
            LeaseDurationSeconds = Options.LeaseDurationSeconds,
            LeaseRenewalThresholdSeconds = Options.LeaseRenewalThresholdSeconds,
            LockContainerName = Options.LockContainerName,
            MaxEventsPerBatch = Options.MaxEventsPerBatch,
            MaxRequestSizeBytes = Options.MaxRequestSizeBytes,
            QueryBatchSize = Options.QueryBatchSize,
        };

    private void ThrowIfClosed()
    {
        if (IsClosed)
        {
            throw new BuilderValidationException(Validate());
        }
    }
}