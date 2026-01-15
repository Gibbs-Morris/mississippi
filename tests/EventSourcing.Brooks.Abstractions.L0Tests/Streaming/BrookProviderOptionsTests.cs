using Allure.Xunit.Attributes;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;


namespace Mississippi.EventSourcing.Brooks.Abstractions.L0Tests.Streaming;

/// <summary>
///     Tests for <see cref="BrookProviderOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.EventSourcing.Brooks.Abstractions")]
[AllureSuite("Streaming")]
[AllureSubSuite("BrookProviderOptions")]
public sealed class BrookProviderOptionsTests
{
    /// <summary>
    ///     OrleansStreamProviderName should default to MississippiDefaults.StreamProviderName.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void OrleansStreamProviderNameDefaultsToMississippiDefaultsStreamProviderName()
    {
        // Arrange & Act
        BrookProviderOptions sut = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamProviderName, sut.OrleansStreamProviderName);
    }

    /// <summary>
    ///     OrleansStreamProviderName should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void OrleansStreamProviderNameCanBeSet()
    {
        // Arrange
        BrookProviderOptions sut = new();
        const string customName = "CustomStreamProvider";

        // Act
        sut.OrleansStreamProviderName = customName;

        // Assert
        Assert.Equal(customName, sut.OrleansStreamProviderName);
    }
}
