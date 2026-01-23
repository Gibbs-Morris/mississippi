using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Reducers;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="DisplayNameUpdatedEventReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("DisplayNameUpdated")]
public sealed class DisplayNameUpdatedEventReducerTests
{
    /// <summary>
    ///     Verifies that reducing a DisplayNameUpdated event updates the display name.
    /// </summary>
    [Fact]
    [AllureStep("Reduce DisplayNameUpdated updates name")]
    public void ReduceUpdatesDisplayName()
    {
        // Arrange
        DisplayNameUpdatedEventReducer eventReducer = new();
        DisplayNameUpdated evt = new()
        {
            OldDisplayName = "Old Name",
            NewDisplayName = "New Name",
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "Old Name",
        };

        // Act
        UserAggregate result = eventReducer.Reduce(state, evt);

        // Assert
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("user-123", result.UserId);
        Assert.True(result.IsRegistered);
    }
}