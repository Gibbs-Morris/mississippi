namespace Mississippi.AspNetCore.Orleans.L0Tests;

/// <summary>
///     Basic tests for AspNetCoreOrleansIntegration.
/// </summary>
public sealed class AspNetCoreOrleansIntegrationTests
{
    /// <summary>
    ///     Tests that Version property returns a valid version string.
    /// </summary>
    [Fact]
    public void VersionShouldReturnValidVersion()
    {
        // Act
        string version = AspNetCoreOrleansIntegration.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }
}