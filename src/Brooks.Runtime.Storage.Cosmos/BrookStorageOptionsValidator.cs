using System.Collections.Generic;
using System.Linq;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Hosting.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>Validates Brooks Cosmos options and their required keyed SDK services during options resolution.</summary>
internal sealed class BrookStorageOptionsValidator : IValidateOptions<BrookStorageOptions>
{
    /// <summary>Initializes a new instance of the <see cref="BrookStorageOptionsValidator" /> class.</summary>
    /// <param name="keyedServiceChecker">The final service graph keyed-service checker.</param>
    public BrookStorageOptionsValidator(
        IServiceProviderIsKeyedService keyedServiceChecker
    ) =>
        KeyedServiceChecker = keyedServiceChecker;

    private IServiceProviderIsKeyedService KeyedServiceChecker { get; }

    /// <summary>Validates the options and required keyed Cosmos and Blob clients.</summary>
    /// <param name="name">The named-options name, when one was supplied.</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>The options validation result.</returns>
    public ValidateOptionsResult Validate(
        string? name,
        BrookStorageOptions options
    )
    {
        List<BuilderDiagnostic> diagnostics = [.. BrookStorageOptionsValidation.Validate(options)];
        AddMissingClientDiagnostic<CosmosClient>(
            diagnostics,
            options.CosmosClientServiceKey,
            BrookStorageBuilderDiagnosticCodes.CosmosClientRegistrationRequired,
            nameof(CosmosClient));
        AddMissingClientDiagnostic<BlobServiceClient>(
            diagnostics,
            BrookCosmosDefaults.BlobLockingServiceKey,
            BrookStorageBuilderDiagnosticCodes.BlobClientRegistrationRequired,
            nameof(BlobServiceClient));
        return diagnostics.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message} {diagnostic.Remediation}"));
    }

    private void AddMissingClientDiagnostic<TClient>(
        List<BuilderDiagnostic> diagnostics,
        string key,
        string code,
        string clientName
    )
    {
        if (!KeyedServiceChecker.IsKeyedService(typeof(TClient), key))
        {
            diagnostics.Add(
                new(
                    code,
                    $"A keyed {clientName} registration is required for Brooks Cosmos key '{key}'.",
                    $"Register {clientName} with the keyed service key '{key}' before starting Brooks Cosmos storage."));
        }
    }
}