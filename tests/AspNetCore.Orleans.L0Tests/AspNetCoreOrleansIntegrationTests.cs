using Allure.Xunit.Attributes;


namespace Mississippi.AspNetCore.Orleans.L0Tests;

/// <summary>
///     Basic tests for AspNetCoreOrleansIntegration.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("Orleans Integration")]
[AllureSubSuite("Integration")]
public sealed class AspNetCoreOrleansIntegrationTests
{
    /// <summary>
    ///     Tests that Version property returns a valid version string.
    /// </summary>
    [Fact(DisplayName = "Version Returns Valid Version String")]
    public void VersionShouldReturnValidVersion()
    {
        // Act
        string version = AspNetCoreOrleansIntegration.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }
}