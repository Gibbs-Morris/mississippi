using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionKey.ForProjection{TProjection}" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionKey ForProjection")]
public sealed class UxProjectionKeyForProjectionTests
{
    /// <summary>
    ///     Another projection class decorated with <see cref="BrookNameAttribute" /> for testing multiple projections.
    /// </summary>
    /// <param name="Name">The sample name.</param>
    [BrookName("TEST", "OTHER", "STREAM")]
    private sealed record AnotherTestProjection(string? Name);

    /// <summary>
    ///     Test projection class decorated with <see cref="BrookNameAttribute" />.
    /// </summary>
    /// <param name="Value">The sample value.</param>
    [BrookName("TEST", "MODULE", "STREAM")]
    private sealed record TestProjection(int Value);

    /// <summary>
    ///     Test projection without BrookNameAttribute.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    private sealed record UnattributedProjection(int Id);

    /// <summary>
    ///     Same projection type for different entity IDs should produce different keys.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void DifferentEntityIdsProduceDifferentKeys()
    {
        UxProjectionKey key1 = UxProjectionKey.ForProjection<TestProjection>("entity1");
        UxProjectionKey key2 = UxProjectionKey.ForProjection<TestProjection>("entity2");
        Assert.NotEqual(key1, key2);
        Assert.Equal(key1.ProjectionTypeName, key2.ProjectionTypeName);
        Assert.NotEqual(key1.BrookKey.EntityId, key2.BrookKey.EntityId);
    }

    /// <summary>
    ///     Different projection types should produce different keys.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void DifferentProjectionTypesProduceDifferentKeys()
    {
        UxProjectionKey key1 = UxProjectionKey.ForProjection<TestProjection>("entity123");
        UxProjectionKey key2 = UxProjectionKey.ForProjection<AnotherTestProjection>("entity123");
        Assert.NotEqual(key1, key2);
        Assert.Equal("TestProjection", key1.ProjectionTypeName);
        Assert.Equal("AnotherTestProjection", key2.ProjectionTypeName);
        Assert.NotEqual(key1.BrookKey, key2.BrookKey);
    }

    /// <summary>
    ///     ForProjection should create correct string representation.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void ForProjectionCreatesCorrectStringRepresentation()
    {
        UxProjectionKey key = UxProjectionKey.ForProjection<TestProjection>("entity123");
        Assert.Equal("TestProjection|TEST.MODULE.STREAM|entity123", key.ToString());
    }

    /// <summary>
    ///     ForProjection should create a key with the brook key from the projection's attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void ForProjectionCreatesKeyWithBrookKeyFromProjectionAttribute()
    {
        UxProjectionKey key = UxProjectionKey.ForProjection<TestProjection>("entity123");
        Assert.Equal("TEST.MODULE.STREAM", key.BrookKey.BrookName);
        Assert.Equal("entity123", key.BrookKey.EntityId);
    }

    /// <summary>
    ///     ForProjection should create a key with the projection type name.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void ForProjectionCreatesKeyWithProjectionTypeName()
    {
        UxProjectionKey key = UxProjectionKey.ForProjection<TestProjection>("entity123");
        Assert.Equal("TestProjection", key.ProjectionTypeName);
    }

    /// <summary>
    ///     ForProjection should throw InvalidOperationException when projection type lacks BrookNameAttribute.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ForProjectionThrowsWhenProjectionLacksBrookNameAttribute()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            UxProjectionKey.ForProjection<UnattributedProjection>("entity123"));
        Assert.Contains("BrookNameAttribute", exception.Message, StringComparison.Ordinal);
    }
}