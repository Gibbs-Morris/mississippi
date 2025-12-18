using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionKey.For{TProjection,TBrook}" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionKey For")]
public sealed class UxProjectionKeyForTests
{
    /// <summary>
    ///     Another projection class for testing multiple projections per brook.
    /// </summary>
    /// <param name="Name">The sample name.</param>
    private sealed record AnotherTestProjection(string? Name);

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
        UxProjectionKey key1 = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity1");
        UxProjectionKey key2 = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity2");
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
        UxProjectionKey key1 = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity123");
        UxProjectionKey key2 = UxProjectionKey.For<AnotherTestProjection, TestBrookDefinition>("entity123");
        Assert.NotEqual(key1, key2);
        Assert.Equal("TestProjection", key1.ProjectionTypeName);
        Assert.Equal("AnotherTestProjection", key2.ProjectionTypeName);
        Assert.Equal(key1.BrookKey, key2.BrookKey);
    }

    /// <summary>
    ///     For should create correct string representation.
    /// </summary>
    [Fact]
    public void ForCreatesCorrectStringRepresentation()
    {
        UxProjectionKey key = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity123");
        Assert.Equal("TestProjection|TEST.MODULE.STREAM|entity123", key.ToString());
    }

    /// <summary>
    ///     For should create a key with the brook key from the definition.
    /// </summary>
    [Fact]
    public void ForCreatesKeyWithBrookKeyFromDefinition()
    {
        UxProjectionKey key = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity123");
        Assert.Equal("TEST.MODULE.STREAM", key.BrookKey.Type);
        Assert.Equal("entity123", key.BrookKey.Id);
    }

    /// <summary>
    ///     For should create a key with the projection type name.
    /// </summary>
    [Fact]
    public void ForCreatesKeyWithProjectionTypeName()
    {
        UxProjectionKey key = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity123");
        Assert.Equal("TestProjection", key.ProjectionTypeName);
    }
}