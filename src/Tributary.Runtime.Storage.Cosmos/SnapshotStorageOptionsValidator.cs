using System.Collections.Generic;
using System.Linq;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Hosting.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Validates Snapshot Cosmos options and the required keyed Cosmos client during options resolution.
/// </summary>
internal sealed class SnapshotStorageOptionsValidator : IValidateOptions<SnapshotStorageOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStorageOptionsValidator" /> class.
    /// </summary>
    /// <param name="keyedServiceChecker">The final service graph keyed-service checker.</param>
    public SnapshotStorageOptionsValidator(
        IServiceProviderIsKeyedService keyedServiceChecker
    ) =>
        KeyedServiceChecker = keyedServiceChecker;

    private IServiceProviderIsKeyedService KeyedServiceChecker { get; }

    /// <summary>
    ///     Validates scalar options and the selected keyed Cosmos client registration.
    /// </summary>
    /// <param name="name">The named-options name, when one was supplied.</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>The options validation result.</returns>
    public ValidateOptionsResult Validate(
        string? name,
        SnapshotStorageOptions options
    )
    {
        List<BuilderDiagnostic> diagnostics = [.. SnapshotStorageOptionsValidation.Validate(options)];
        AddMissingClientDiagnostic(diagnostics, options.CosmosClientServiceKey);
        return diagnostics.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message} {diagnostic.Remediation}"));
    }

    private void AddMissingClientDiagnostic(
        List<BuilderDiagnostic> diagnostics,
        string key
    )
    {
        if (string.IsNullOrWhiteSpace(key) || !KeyedServiceChecker.IsKeyedService(typeof(CosmosClient), key))
        {
            diagnostics.Add(
                new(
                    SnapshotStorageBuilderDiagnosticCodes.CosmosClientRegistrationRequired,
                    $"A keyed CosmosClient registration is required for Snapshot Cosmos key '{key}'.",
                    $"Register CosmosClient with the keyed service key '{key}' before starting Snapshot Cosmos storage."));
        }
    }
}