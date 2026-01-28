
using Mississippi.Inlet.Silo.Abstractions;


namespace Mississippi.Inlet.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="InletSubscription" />.
/// </summary>
public sealed class InletSubscriptionTests
{
    /// <summary>
    ///     Constructor should set all properties correctly.
    /// </summary>
    [Fact]
        public void ConstructorSetsAllProperties()
    {
        // Arrange & Act
        InletSubscription sut = new("sub-1", "cascade/channels", "entity-1");

        // Assert
        Assert.Equal("sub-1", sut.SubscriptionId);
        Assert.Equal("cascade/channels", sut.Path);
        Assert.Equal("entity-1", sut.EntityId);
    }

    /// <summary>
    ///     Two subscriptions with different values should not be equal.
    /// </summary>
    [Fact]
        public void DifferentSubscriptionsAreNotEqual()
    {
        // Arrange
        InletSubscription sub1 = new("sub-1", "cascade/channels", "entity-1");
        InletSubscription sub2 = new("sub-2", "cascade/channels", "entity-1");

        // Assert
        Assert.NotEqual(sub1, sub2);
    }

    /// <summary>
    ///     Two subscriptions with same values should be equal.
    /// </summary>
    [Fact]
        public void EqualSubscriptionsAreEqual()
    {
        // Arrange
        InletSubscription sub1 = new("sub-1", "cascade/channels", "entity-1");
        InletSubscription sub2 = new("sub-1", "cascade/channels", "entity-1");

        // Assert
        Assert.Equal(sub1, sub2);
    }
}