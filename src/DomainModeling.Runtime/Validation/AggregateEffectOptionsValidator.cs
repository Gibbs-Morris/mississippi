using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.Validation;

/// <summary>
///     Validates <see cref="AggregateEffectOptions" /> at startup via <c>ValidateOnStart</c>.
/// </summary>
internal sealed class AggregateEffectOptionsValidator : IValidateOptions<AggregateEffectOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        AggregateEffectOptions options
    )
    {
        if (options.MaxEffectIterations <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"AggregateEffectOptions.MaxEffectIterations must be greater than zero but was {options.MaxEffectIterations}.");
        }

        return ValidateOptionsResult.Success;
    }
}