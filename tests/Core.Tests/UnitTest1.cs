using Allure.Xunit.Attributes;


namespace Mississippi.Core.Tests;

/// <summary>
///     A test class.
/// </summary>
public class UnitTest1
{
    /// <summary>
    ///     A test method.
    /// </summary>
    [AllureEpic("EF-1")]
    [AllureParentSuite("Web interface")]
    [AllureSuite("Essential features")]
    [Fact]
    public void Test1()
    {
        Assert.Equal(1, 1);
    }
}
