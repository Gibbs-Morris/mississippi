using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Effects.L0Tests;

/// <summary>
///     Smoke tests for the Effects project.
/// </summary>
public sealed class SmokeTests
{
    /// <summary>
    ///     Verifies the test project runs.
    /// </summary>
    [AllureEpic("Effects")]
    [Fact]
    public void PlaceholderShouldBeTrue()
    {
        Assert.True(true);
    }
}