using Allure.Xunit.Attributes;


namespace Mississippi.Inlet.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="InletHubConstants" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Abstractions")]
[AllureSuite("Core")]
[AllureSubSuite("InletHubConstants")]
public sealed class InletHubConstantsTests
{
    /// <summary>
    ///     HubName should have expected value.
    /// </summary>
    [Fact]
    [AllureFeature("Constants")]
    public void HubNameHasExpectedValue()
    {
        // Assert
        Assert.Equal("InletHub", InletHubConstants.HubName);
    }

    /// <summary>
    ///     ProjectionUpdatedMethod should have expected value.
    /// </summary>
    [Fact]
    [AllureFeature("Constants")]
    public void ProjectionUpdatedMethodHasExpectedValue()
    {
        // Assert
        Assert.Equal("ProjectionUpdated", InletHubConstants.ProjectionUpdatedMethod);
    }

    /// <summary>
    ///     SubscribeMethod should have expected value.
    /// </summary>
    [Fact]
    [AllureFeature("Constants")]
    public void SubscribeMethodHasExpectedValue()
    {
        // Assert
        Assert.Equal("SubscribeAsync", InletHubConstants.SubscribeMethod);
    }

    /// <summary>
    ///     UnsubscribeMethod should have expected value.
    /// </summary>
    [Fact]
    [AllureFeature("Constants")]
    public void UnsubscribeMethodHasExpectedValue()
    {
        // Assert
        Assert.Equal("UnsubscribeAsync", InletHubConstants.UnsubscribeMethod);
    }
}