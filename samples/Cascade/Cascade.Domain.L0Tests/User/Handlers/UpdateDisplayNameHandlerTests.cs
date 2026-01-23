using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.User.Handlers;

/// <summary>
///     Tests for <see cref="UpdateDisplayNameHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("User")]
[AllureSubSuite("Handlers")]
[AllureFeature("UpdateDisplayName")]
public sealed class UpdateDisplayNameHandlerTests
{
    /// <summary>
    ///     Verifies that updating display name for a registered user returns a DisplayNameUpdated event.
    /// </summary>
    [Fact]
    [AllureStep("Handle UpdateDisplayName when registered")]
    public void HandleReturnsDisplayNameUpdatedEventWhenRegistered()
    {
        // Arrange
        UpdateDisplayNameHandler handler = new();
        UpdateDisplayName command = new()
        {
            NewDisplayName = "New Name",
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "Old Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        DisplayNameUpdated updated = Assert.IsType<DisplayNameUpdated>(singleEvent);
        Assert.Equal("New Name", updated.NewDisplayName);
    }

    /// <summary>
    ///     Verifies that updating display name with empty value returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle UpdateDisplayName with empty display name")]
    public void HandleReturnsErrorWhenDisplayNameEmpty()
    {
        // Arrange
        UpdateDisplayNameHandler handler = new();
        UpdateDisplayName command = new()
        {
            NewDisplayName = string.Empty,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "Old Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that updating display name for an unregistered user returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle UpdateDisplayName when not registered")]
    public void HandleReturnsErrorWhenNotRegistered()
    {
        // Arrange
        UpdateDisplayNameHandler handler = new();
        UpdateDisplayName command = new()
        {
            NewDisplayName = "New Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }
}