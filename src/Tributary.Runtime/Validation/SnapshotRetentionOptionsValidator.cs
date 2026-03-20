using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Validation;

/// <summary>
///     Validates <see cref="SnapshotRetentionOptions" /> at startup via <c>ValidateOnStart</c>.
/// </summary>
internal sealed class SnapshotRetentionOptionsValidator : IValidateOptions<SnapshotRetentionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        SnapshotRetentionOptions options
    )
    {
        if (options.DefaultRetainModulus <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"SnapshotRetentionOptions.DefaultRetainModulus must be greater than zero but was {options.DefaultRetainModulus}.");
        }

        return ValidateOptionsResult.Success;
    }
}