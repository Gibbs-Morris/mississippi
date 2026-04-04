using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Validates Spring auth-proof settings required for saga-access propagation.
/// </summary>
internal sealed class SpringSagaAccessOptionsValidator : IValidateOptions<SpringAuthOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaAccessOptionsValidator" /> class.
    /// </summary>
    /// <param name="accessContextProvider">Provider used to derive saga access fingerprints from authenticated callers.</param>
    public SpringSagaAccessOptionsValidator(
        SpringSagaAccessContextProvider accessContextProvider
    )
    {
        ArgumentNullException.ThrowIfNull(accessContextProvider);
    }

    /// <summary>
    ///     Validates the configured Spring auth-proof options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The configured Spring auth-proof options.</param>
    /// <returns>The validation outcome.</returns>
    public ValidateOptionsResult Validate(
        string? name,
        SpringAuthOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = new();
        if (string.IsNullOrWhiteSpace(options.DefaultUserId))
        {
            failures.Add("SpringAuth:DefaultUserId must be configured when SpringAuth is enabled.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}