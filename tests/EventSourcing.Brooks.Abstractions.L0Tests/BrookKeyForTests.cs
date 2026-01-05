using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookKey.ForGrain{TGrain}" /> and <see cref="BrookKey.ForType{T}" /> functionality.
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
    ///     Test projection type for testing BrookKey.ForType functionality.
    /// </summary>
    /// <param name="Value">A sample value.</param>
    [BrookName("PROJ", "DOMAIN", "ENTITY")]
    private sealed record TestProjection(int Value);

    /// <summary>
    ///     Type without BrookNameAttribute for negative testing.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    private sealed record UnattributedType(int Id);

    /// <summary>
    ///     ForGrain should create a key using the grain's brook name attribute.
    /// </summary>
    [Fact]
    [AllureFeature("ForGrain")]
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
    [AllureFeature("ForGrain")]
    public void ForGrainCreatesKeyWithCorrectStringFormat()
    {
        BrookKey key = BrookKey.ForGrain<TestGrain>("order-456");
        string stringKey = key;
        Assert.Equal("TEST.MODULE.STREAM|order-456", stringKey);
    }

    /// <summary>
    ///     ForType should create a key using the type's brook name attribute.
    /// </summary>
    [Fact]
    [AllureFeature("ForType")]
    public void ForTypeCreatesKeyWithBrookName()
    {
        BrookKey key = BrookKey.ForType<TestProjection>("entity-789");
        Assert.Equal("PROJ.DOMAIN.ENTITY", key.BrookName);
        Assert.Equal("entity-789", key.EntityId);
    }

    /// <summary>
    ///     ForType should create keys that convert to correct string format.
    /// </summary>
    [Fact]
    [AllureFeature("ForType")]
    public void ForTypeCreatesKeyWithCorrectStringFormat()
    {
        BrookKey key = BrookKey.ForType<TestProjection>("proj-456");
        string stringKey = key;
        Assert.Equal("PROJ.DOMAIN.ENTITY|proj-456", stringKey);
    }

    /// <summary>
    ///     ForType should work with any type decorated with BrookNameAttribute, not just grains.
    /// </summary>
    [Fact]
    [AllureFeature("ForType")]
    public void ForTypeWorksWithNonGrainTypes()
    {
        // TestProjection is a record, not a grain class
        BrookKey key = BrookKey.ForType<TestProjection>("record-123");
        Assert.Equal("PROJ.DOMAIN.ENTITY", key.BrookName);
        Assert.Equal("record-123", key.EntityId);
    }

    /// <summary>
    ///     ForType should throw InvalidOperationException when type lacks BrookNameAttribute.
    /// </summary>
    [Fact]
    [AllureFeature("ForType")]
    public void ForTypeThrowsWhenTypeLacksBrookNameAttribute()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                BrookKey.ForType<UnattributedType>("entity-123"));
        Assert.Contains("BrookNameAttribute", exception.Message, StringComparison.Ordinal);
    }
}