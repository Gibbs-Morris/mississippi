using Microsoft.Extensions.Options;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Validates named options for the Cosmos-backed replica sink provider.
/// </summary>
internal sealed class CosmosReplicaSinkOptionsValidation : IValidateOptions<CosmosReplicaSinkOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        CosmosReplicaSinkOptions options
    )
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Cosmos replica sink options are required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientKey))
        {
            return ValidateOptionsResult.Fail($"Cosmos replica sink '{name ?? Options.DefaultName}' client key is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseId))
        {
            return ValidateOptionsResult.Fail($"Cosmos replica sink '{name ?? Options.DefaultName}' database identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ContainerId))
        {
            return ValidateOptionsResult.Fail($"Cosmos replica sink '{name ?? Options.DefaultName}' container identifier is required.");
        }

        if (options.QueryBatchSize <= 0)
        {
            return ValidateOptionsResult.Fail($"Cosmos replica sink '{name ?? Options.DefaultName}' query batch size must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
