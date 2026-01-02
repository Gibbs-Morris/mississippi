using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Integration tests for <see cref="IUxProjectionCursorGrain" /> behavior using a real Orleans cluster.
/// </summary>
[Collection(ClusterTestSuite.Name)]
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionCursorGrain Integration")]
public sealed class UxProjectionCursorGrainIntegrationTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    ///     Verifies that cursor grain activates correctly with valid key format.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Grain Activation")]
    public async Task CursorGrainActivatesWithValidKeyFormat()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("valid-key-test"));
        IUxProjectionCursorGrain cursor = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());

        // Act - this call implicitly activates the grain
        BrookPosition position = await cursor.GetPositionAsync();

        // Assert
        Assert.True(position.Value >= -1);
    }

    /// <summary>
    ///     Verifies that the cursor grain can be activated and returns initial position of -1.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Grain Activation")]
    public async Task CursorGrainReturnsInitialMinusOnePosition()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("grain-test-1"));
        IUxProjectionCursorGrain cursor = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());

        // Act
        BrookPosition position = await cursor.GetPositionAsync();

        // Assert
        Assert.Equal(-1, position.Value);
        Assert.True(position.NotSet);
    }

    /// <summary>
    ///     Verifies that DeactivateAsync completes without throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Grain Lifecycle")]
    public async Task DeactivateAsyncCompletesWithoutError()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("grain-deactivate-test"));
        IUxProjectionCursorGrain cursor = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());

        // Act
        await cursor.DeactivateAsync();

        // Assert - no exception means success; verify we can still read after re-activation
        BrookPosition position = await cursor.GetPositionAsync();
        Assert.True(position.Value >= -1);
    }

    /// <summary>
    ///     Verifies that grain reference obtained multiple times refers to same grain.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Grain Identity")]
    public async Task GrainReferenceObtainedMultipleTimesIsSameGrain()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("same-grain-test"));
        IUxProjectionCursorGrain cursor1 = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());
        IUxProjectionCursorGrain cursor2 = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());

        // Act
        BrookPosition position1 = await cursor1.GetPositionAsync();
        BrookPosition position2 = await cursor2.GetPositionAsync();

        // Assert - same key should reference the same grain instance
        Assert.Equal(position1, position2);
    }

    /// <summary>
    ///     Verifies that multiple cursor grains for different entities return independent initial positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Multi-Grain")]
    public async Task MultipleCursorGrainsReturnIndependentInitialPositions()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key1 = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("multi-init-1"));
        UxProjectionCursorKey key2 = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("multi-init-2"));
        IUxProjectionCursorGrain cursor1 = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key1.ToString());
        IUxProjectionCursorGrain cursor2 = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key2.ToString());

        // Act
        BrookPosition position1 = await cursor1.GetPositionAsync();
        BrookPosition position2 = await cursor2.GetPositionAsync();

        // Assert - both should have initial -1 position
        Assert.Equal(-1, position1.Value);
        Assert.Equal(-1, position2.Value);
    }

    /// <summary>
    ///     Verifies that the same grain reference returns consistent positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Position Consistency")]
    public async Task SameGrainReferenceReturnsConsistentPosition()
    {
        // Arrange - use UxProjectionCursorKey format (brookName|entityId)
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>("consistent-position"));
        IUxProjectionCursorGrain cursor = cluster.GrainFactory.GetGrain<IUxProjectionCursorGrain>(key.ToString());

        // Act - call multiple times
        BrookPosition position1 = await cursor.GetPositionAsync();
        BrookPosition position2 = await cursor.GetPositionAsync();
        BrookPosition position3 = await cursor.GetPositionAsync();

        // Assert - all should be same initial value
        Assert.Equal(position1, position2);
        Assert.Equal(position2, position3);
    }
}