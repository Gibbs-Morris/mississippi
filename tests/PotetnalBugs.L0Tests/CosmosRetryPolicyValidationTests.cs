using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Cosmos.Retry;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates missing constructor validation behavior in <see cref="CosmosRetryPolicy" />.
/// </summary>
public sealed class CosmosRetryPolicyValidationTests
{
    /// <summary>
    ///     Negative maxRetries is accepted, causing ExecuteAsync to skip the operation loop entirely
    ///     and fail without a single attempt.
    /// </summary>
    /// <returns>A task that completes when the behavior has been validated.</returns>
    [Fact]
    [ValidatingPotetnalBug(
        "CosmosRetryPolicy constructor accepts negative maxRetries. ExecuteAsync then runs zero " +
        "attempts because the retry loop condition is never true, failing immediately without invoking operation.",
        FilePath = "src/Common.Cosmos/Retry/CosmosRetryPolicy.cs",
        LineNumbers = "24-30,71,107",
        Severity = "Medium",
        Category = "MissingValidation")]
    public async Task NegativeMaxRetriesSkipsOperationExecution()
    {
        // Arrange
        CosmosRetryPolicy sut = new(NullLogger<CosmosRetryPolicy>.Instance, maxRetries: -1);
        int calls = 0;

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync(async () =>
            {
                calls++;
                await Task.Yield();
                return 42;
            }));

        // Assert - operation was never attempted
        Assert.Equal(0, calls);
        Assert.Equal("Operation failed after 0 attempts", ex.Message);
    }
}
