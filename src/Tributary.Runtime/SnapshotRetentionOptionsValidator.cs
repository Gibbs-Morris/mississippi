using System.Collections.Generic;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     Validates snapshot retention configuration before the host starts.
/// </summary>
internal sealed class SnapshotRetentionOptionsValidator : IValidateOptions<SnapshotRetentionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        SnapshotRetentionOptions? options
    )
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Snapshot retention options cannot be null.");
        }

        List<string> failures = [];
        if (options.DefaultRetainModulus <= 0)
        {
            failures.Add(
                $"{nameof(SnapshotRetentionOptions.DefaultRetainModulus)} must be greater than zero, but was {options.DefaultRetainModulus}.");
        }

        foreach (KeyValuePair<string, int> item in options.StateTypeOverrides)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                failures.Add("StateTypeOverrides cannot contain an empty state type key.");
            }

            if (item.Value <= 0)
            {
                failures.Add($"StateTypeOverrides['{item.Key}'] must be greater than zero, but was {item.Value}.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}