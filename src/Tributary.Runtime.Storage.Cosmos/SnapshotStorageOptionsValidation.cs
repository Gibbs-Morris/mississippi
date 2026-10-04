using System;
using System.Collections.Generic;

using Mississippi.Hosting.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Validates structural constraints for Snapshot Cosmos storage options.
/// </summary>
internal static class SnapshotStorageOptionsValidation
{
    /// <summary>Returns diagnostics for invalid Snapshot Cosmos storage options.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validation diagnostics, or an empty list when the options are valid.</returns>
    internal static IReadOnlyList<BuilderDiagnostic> Validate(
        SnapshotStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        List<BuilderDiagnostic> diagnostics = [];
        AddRequired(
            diagnostics,
            nameof(SnapshotStorageOptions.ContainerId),
            options.ContainerId,
            SnapshotStorageBuilderDiagnosticCodes.ContainerIdRequired);
        AddRequired(
            diagnostics,
            nameof(SnapshotStorageOptions.CosmosClientServiceKey),
            options.CosmosClientServiceKey,
            SnapshotStorageBuilderDiagnosticCodes.CosmosClientServiceKeyRequired);
        AddRequired(
            diagnostics,
            nameof(SnapshotStorageOptions.DatabaseId),
            options.DatabaseId,
            SnapshotStorageBuilderDiagnosticCodes.DatabaseIdRequired);
        if ((options.QueryBatchSize == 0) || (options.QueryBatchSize < -1))
        {
            AddInvalid(
                diagnostics,
                SnapshotStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid,
                nameof(SnapshotStorageOptions.QueryBatchSize),
                "must be positive or -1 for dynamic Cosmos query page sizing",
                "Set QueryBatchSize to a positive value or -1.");
        }

        return diagnostics;
    }

    private static void AddInvalid(
        List<BuilderDiagnostic> diagnostics,
        string code,
        string propertyName,
        string requirement,
        string remediation
    ) =>
        diagnostics.Add(new(code, $"SnapshotStorageOptions.{propertyName} {requirement}.", remediation));

    private static void AddRequired(
        List<BuilderDiagnostic> diagnostics,
        string propertyName,
        string? value,
        string code
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddInvalid(diagnostics, code, propertyName, "cannot be empty", $"Set {propertyName} to a nonempty value.");
        }
    }
}