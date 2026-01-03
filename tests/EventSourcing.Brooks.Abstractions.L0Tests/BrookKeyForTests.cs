using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookKey.ForGrain{TGrain}" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Brook Key")]
public class BrookKeyForTests
{
    /// <summary>
    ///     Test grain type for testing BrookKey.ForGrain functionality.
    /// </summary>
    [BrookName("TEST", "MODULE", "STREAM")]
    private sealed class TestGrain
    {
    }

    /// <summary>
    ///     ForGrain should create a key using the grain's brook name attribute.
    /// </summary>
    [Fact]
    public void ForGrainCreatesKeyWithBrookName()
    {
        BrookKey key = BrookKey.ForGrain<TestGrain>("entity-123");
        Assert.Equal("TEST.MODULE.STREAM", key.BrookName);
        Assert.Equal("entity-123", key.EntityId);
    }

    /// <summary>
    ///     ForGrain should create keys that convert to correct string format.
    /// </summary>
    [Fact]
    public void ForGrainCreatesKeyWithCorrectStringFormat()
    {
        BrookKey key = BrookKey.ForGrain<TestGrain>("order-456");
        string stringKey = key;
        Assert.Equal("TEST.MODULE.STREAM|order-456", stringKey);
    }
}