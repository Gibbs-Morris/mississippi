using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Effects.L0Tests;

/// <summary>
///     Smoke tests for the Effects project.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Effects")]
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