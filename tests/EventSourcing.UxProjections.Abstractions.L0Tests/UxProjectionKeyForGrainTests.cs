using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionKey.ForGrain{TProjection,TGrain}" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionKey ForGrain")]
public sealed class UxProjectionKeyForGrainTests
{
    /// <summary>
    ///     Another projection class for testing multiple projections per brook.
    /// </summary>
    /// <param name="Name">The sample name.</param>
    private sealed record AnotherTestProjection(string? Name);

    /// <summary>
    ///     Test grain type for testing purposes.
    /// </summary>
    [BrookName("TEST", "MODULE", "STREAM")]
    private sealed class TestGrain
    {
    }

    /// <summary>
    ///     Test projection class for testing purposes.
    /// </summary>
    /// <param name="Value">The sample value.</param>
    private sealed record TestProjection(int Value);

    /// <summary>
    ///     Same projection type for different entity IDs should produce different keys.
    /// </summary>
    [Fact]
    public void DifferentEntityIdsProduceDifferentKeys()
    {
        UxProjectionKey key1 = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity1");
        UxProjectionKey key2 = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity2");
        Assert.NotEqual(key1, key2);
        Assert.Equal(key1.ProjectionTypeName, key2.ProjectionTypeName);
        Assert.NotEqual(key1.BrookKey.Id, key2.BrookKey.Id);
    }

    /// <summary>
    ///     Different projection types for the same brook should produce different keys.
    /// </summary>
    [Fact]
    public void DifferentProjectionTypesProduceDifferentKeys()
    {
        UxProjectionKey key1 = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity123");
        UxProjectionKey key2 = UxProjectionKey.ForGrain<AnotherTestProjection, TestGrain>("entity123");
        Assert.NotEqual(key1, key2);
        Assert.Equal("TestProjection", key1.ProjectionTypeName);
        Assert.Equal("AnotherTestProjection", key2.ProjectionTypeName);
        Assert.Equal(key1.BrookKey, key2.BrookKey);
    }

    /// <summary>
    ///     ForGrain should create correct string representation.
    /// </summary>
    [Fact]
    public void ForGrainCreatesCorrectStringRepresentation()
    {
        UxProjectionKey key = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity123");
        Assert.Equal("TestProjection|TEST.MODULE.STREAM|entity123", key.ToString());
    }

    /// <summary>
    ///     ForGrain should create a key with the brook key from the grain's attribute.
    /// </summary>
    [Fact]
    public void ForGrainCreatesKeyWithBrookKeyFromGrainAttribute()
    {
        UxProjectionKey key = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity123");
        Assert.Equal("TEST.MODULE.STREAM", key.BrookKey.Type);
        Assert.Equal("entity123", key.BrookKey.Id);
    }

    /// <summary>
    ///     ForGrain should create a key with the projection type name.
    /// </summary>
    [Fact]
    public void ForGrainCreatesKeyWithProjectionTypeName()
    {
        UxProjectionKey key = UxProjectionKey.ForGrain<TestProjection, TestGrain>("entity123");
        Assert.Equal("TestProjection", key.ProjectionTypeName);
    }
}