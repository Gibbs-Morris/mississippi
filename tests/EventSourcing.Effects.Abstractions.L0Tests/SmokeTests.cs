using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Effects.Abstractions.L0Tests;

/// <summary>
///     Smoke tests for the Effects abstractions project.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Effects Abstractions")]
[AllureSubSuite("Smoke Tests")]
public sealed class SmokeTests
{
    /// <summary>
    ///     Verifies the test project runs.
    /// </summary>
    [Fact]
    public void PlaceholderShouldBeTrue()
    {
        Assert.True(true);
    }
}