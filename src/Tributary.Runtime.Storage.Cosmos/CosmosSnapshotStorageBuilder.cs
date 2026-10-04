using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Mississippi.Hosting.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Configures Snapshot Cosmos storage within one runtime composition scope.
/// </summary>
/// <remarks>Public for the AddCosmosSnapshotStorageProvider callback; closed scopes cannot be configured again.</remarks>
public sealed class CosmosSnapshotStorageBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosSnapshotStorageBuilder" /> class.
    /// </summary>
    /// <param name="cosmosConnectionString">The optional Cosmos connection string owned by this scope.</param>
    internal CosmosSnapshotStorageBuilder(
        string? cosmosConnectionString = null
    ) =>
        CosmosConnectionString = cosmosConnectionString;

    /// <summary>Gets or sets the Cosmos container identifier for snapshots.</summary>
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

    /// <summary>Gets or sets the maximum number of snapshot items requested per query page, or -1 for dynamic sizing.</summary>
    public int QueryBatchSize
    {
        get => Options.QueryBatchSize;
        set
        {
            ThrowIfClosed();
            Options.QueryBatchSize = value;
        }
    }

    private string? CosmosConnectionString { get; }

    private bool IsClosed { get; set; }

    private SnapshotStorageOptions Options { get; } = new();

    /// <summary>Returns the current options diagnostics without registering services.</summary>
    /// <returns>The current validation diagnostics.</returns>
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (IsClosed)
        {
            return
            [
                new(
                    SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
                    "The Snapshot Cosmos storage configuration scope has closed.",
                    "Configure Snapshot Cosmos storage inside a fresh AddCosmosSnapshotStorageProvider(...) callback."),
            ];
        }

        return SnapshotStorageOptionsValidation.Validate(Options);
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

        SnapshotStorageProviderRegistrations.RegisterCore(silo.Services, Snapshot(), CosmosConnectionString);
    }

    /// <summary>Copies configuration values into this nested scope before its final snapshot is taken.</summary>
    /// <param name="configuration">The configuration section containing SnapshotStorageOptions property names.</param>
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
    /// <returns>The final SnapshotStorageOptions snapshot.</returns>
    internal SnapshotStorageOptions Snapshot() =>
        new()
        {
            ContainerId = Options.ContainerId,
            CosmosClientServiceKey = Options.CosmosClientServiceKey,
            DatabaseId = Options.DatabaseId,
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