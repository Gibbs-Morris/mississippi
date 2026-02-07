using System;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Cosmos.Retry;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that constructor validation in <see cref="CosmosRetryPolicy" /> is now fixed:
///     negative maxRetries throws <see cref="ArgumentOutOfRangeException" /> at construction time.
/// </summary>
public sealed class CosmosRetryPolicyValidationTests
{
    /// <summary>
    ///     FIXED: Negative maxRetries now throws <see cref="ArgumentOutOfRangeException" />
    ///     at construction time instead of silently accepting and skipping all attempts.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: CosmosRetryPolicy constructor previously accepted negative maxRetries, " +
        "causing ExecuteAsync to skip all attempts. Constructor now throws ArgumentOutOfRangeException.",
        FilePath = "src/Common.Cosmos/Retry/CosmosRetryPolicy.cs",
        LineNumbers = "24-30,71,107",
        Severity = "Medium",
        Category = "MissingValidation")]
    public void NegativeMaxRetriesThrowsAtConstruction()
    {
        // Act & Assert - constructor now rejects negative maxRetries
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CosmosRetryPolicy(NullLogger<CosmosRetryPolicy>.Instance, maxRetries: -1));
    }
}
