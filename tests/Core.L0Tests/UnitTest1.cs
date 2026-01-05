using Allure.Xunit.Attributes;


namespace Mississippi.Core.L0Tests;

/// <summary>
///     A test class.
/// </summary>
[AllureParentSuite("Core")]
[AllureSuite("Core")]
[AllureSubSuite("Smoke Tests")]
public sealed class UnitTest1
{
    /// <summary>
    ///     A test method.
    /// </summary>
    [Fact(DisplayName = "Placeholder Test Passes")]
    public void Test1()
    {
        Assert.Equal(1, 1);
    }
}