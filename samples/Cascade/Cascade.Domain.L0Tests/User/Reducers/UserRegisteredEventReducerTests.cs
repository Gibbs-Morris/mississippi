using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Reducers;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="UserRegisteredEventReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("UserRegistered")]
public sealed class UserRegisteredEventReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserRegistered event creates a registered user state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered creates registered state")]
    public void ReduceCreatesRegisteredState()
    {
        // Arrange
        UserRegisteredEventReducer eventReducer = new();
        DateTimeOffset registeredAt = DateTimeOffset.UtcNow;
        UserRegistered evt = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            RegisteredAt = registeredAt,
        };

        // Act
        UserAggregate result = eventReducer.Reduce(null!, evt);

        // Assert
        Assert.True(result.IsRegistered);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.Equal(registeredAt, result.RegisteredAt);
        Assert.False(result.IsOnline);
        Assert.Empty(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserRegistered event overwrites existing state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered overwrites existing state")]
    public void ReduceOverwritesExistingState()
    {
        // Arrange
        UserRegisteredEventReducer eventReducer = new();
        DateTimeOffset registeredAt = DateTimeOffset.UtcNow;
        UserRegistered evt = new()
        {
            UserId = "user-456",
            DisplayName = "Jane Doe",
            RegisteredAt = registeredAt,
        };
        UserAggregate existingState = new()
        {
            IsRegistered = true,
            UserId = "old-user",
            DisplayName = "Old Name",
        };

        // Act
        UserAggregate result = eventReducer.Reduce(existingState, evt);

        // Assert
        Assert.True(result.IsRegistered);
        Assert.Equal("user-456", result.UserId);
        Assert.Equal("Jane Doe", result.DisplayName);
        Assert.Equal(registeredAt, result.RegisteredAt);
    }
}