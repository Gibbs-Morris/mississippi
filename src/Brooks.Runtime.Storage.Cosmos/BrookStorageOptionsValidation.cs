using System;
using System.Collections.Generic;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Hosting.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>Validates the structural constraints for Brooks Cosmos storage options.</summary>
internal static class BrookStorageOptionsValidation
{
    /// <summary>Returns diagnostics for invalid Brooks Cosmos storage options.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validation diagnostics, or an empty list when the options are valid.</returns>
    internal static IReadOnlyList<BuilderDiagnostic> Validate(
        BrookStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        List<BuilderDiagnostic> diagnostics = [];
        AddRequired(
            diagnostics,
            nameof(BrookStorageOptions.ContainerId),
            options.ContainerId,
            BrookStorageBuilderDiagnosticCodes.ContainerIdRequired);
        AddRequired(
            diagnostics,
            nameof(BrookStorageOptions.CosmosClientServiceKey),
            options.CosmosClientServiceKey,
            BrookStorageBuilderDiagnosticCodes.CosmosClientServiceKeyRequired);
        AddRequired(
            diagnostics,
            nameof(BrookStorageOptions.DatabaseId),
            options.DatabaseId,
            BrookStorageBuilderDiagnosticCodes.DatabaseIdRequired);
        AddRequired(
            diagnostics,
            nameof(BrookStorageOptions.LockContainerName),
            options.LockContainerName,
            BrookStorageBuilderDiagnosticCodes.LockContainerNameRequired);
        if ((options.QueryBatchSize == 0) || (options.QueryBatchSize < -1))
        {
            AddInvalid(
                diagnostics,
                BrookStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid,
                nameof(BrookStorageOptions.QueryBatchSize),
                "must be positive or -1 for dynamic Cosmos query page sizing",
                "Set QueryBatchSize to a positive value or -1.");
        }

        if ((options.LeaseDurationSeconds < 15) || (options.LeaseDurationSeconds > 60))
        {
            AddInvalid(
                diagnostics,
                BrookStorageBuilderDiagnosticCodes.LeaseDurationInvalid,
                nameof(BrookStorageOptions.LeaseDurationSeconds),
                "must be between 15 and 60 seconds",
                "Set LeaseDurationSeconds to a finite Blob lease duration from 15 through 60 seconds.");
        }

        if ((options.LeaseRenewalThresholdSeconds < 0) ||
            (options.LeaseRenewalThresholdSeconds >= options.LeaseDurationSeconds))
        {
            AddInvalid(
                diagnostics,
                BrookStorageBuilderDiagnosticCodes.LeaseRenewalThresholdInvalid,
                nameof(BrookStorageOptions.LeaseRenewalThresholdSeconds),
                "must be nonnegative and less than LeaseDurationSeconds",
                "Set LeaseRenewalThresholdSeconds below LeaseDurationSeconds.");
        }

        if (options.MaxEventsPerBatch <= 0)
        {
            AddInvalid(
                diagnostics,
                BrookStorageBuilderDiagnosticCodes.MaxEventsPerBatchInvalid,
                nameof(BrookStorageOptions.MaxEventsPerBatch),
                "must be positive",
                "Set MaxEventsPerBatch to a positive value.");
        }

        if (options.MaxRequestSizeBytes <= BatchSizeEstimator.BatchOverheadBytes)
        {
            AddInvalid(
                diagnostics,
                BrookStorageBuilderDiagnosticCodes.MaxRequestSizeInvalid,
                nameof(BrookStorageOptions.MaxRequestSizeBytes),
                $"must exceed the {BatchSizeEstimator.BatchOverheadBytes} byte batch envelope",
                $"Set MaxRequestSizeBytes above {BatchSizeEstimator.BatchOverheadBytes}.");
        }

        return diagnostics;
    }

    private static void AddInvalid(
        ICollection<BuilderDiagnostic> diagnostics,
        string code,
        string propertyName,
        string requirement,
        string remediation
    ) =>
        diagnostics.Add(new(code, $"BrookStorageOptions.{propertyName} {requirement}.", remediation));

    private static void AddRequired(
        ICollection<BuilderDiagnostic> diagnostics,
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