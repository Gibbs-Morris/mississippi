using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookKey.For{TBrook}" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Brook Key")]
public class BrookKeyForTests
{
    /// <summary>
    ///     Test brook definition for testing purposes.
    /// </summary>
    [BrookName("TEST", "MODULE", "STREAM")]
    private sealed class TestBrookDefinition : IBrookDefinition
    {
        /// <inheritdoc />
        public static string BrookName => "TEST.MODULE.STREAM";
    }

    /// <summary>
    ///     For should create a key using the brook definition's name.
    /// </summary>
    [Fact]
    public void ForCreatesKeyWithBrookName()
    {
        BrookKey key = BrookKey.For<TestBrookDefinition>("entity-123");
        Assert.Equal("TEST.MODULE.STREAM", key.Type);
        Assert.Equal("entity-123", key.Id);
    }

    /// <summary>
    ///     For should create keys that convert to correct string format.
    /// </summary>
    [Fact]
    public void ForCreatesKeyWithCorrectStringFormat()
    {
        BrookKey key = BrookKey.For<TestBrookDefinition>("order-456");
        string stringKey = key;
        Assert.Equal("TEST.MODULE.STREAM|order-456", stringKey);
    }
}