using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Reducers;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="DisplayNameUpdatedReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("DisplayNameUpdated")]
public sealed class DisplayNameUpdatedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a DisplayNameUpdated event updates the display name.
    /// </summary>
    [Fact]
    [AllureStep("Reduce DisplayNameUpdated updates name")]
    public void ReduceUpdatesDisplayName()
    {
        // Arrange
        DisplayNameUpdatedReducer reducer = new();
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
        UserAggregate result = reducer.Reduce(state, evt);

        // Assert
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("user-123", result.UserId);
        Assert.True(result.IsRegistered);
    }
}