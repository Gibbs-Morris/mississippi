using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.Channel;
using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Aggregates.Channel.Events;
using Cascade.Domain.Aggregates.Channel.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Channel.Handlers;

/// <summary>
///     Tests for <see cref="AddMemberHandler" /> and <see cref="RemoveMemberHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Channel")]
[AllureSubSuite("Handlers")]
[AllureFeature("MemberManagement")]
public sealed class MemberHandlerTests
{
    /// <summary>
    ///     Verifies that adding a member when already a member returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle AddMember when already member")]
    public void HandleAddMemberReturnsErrorWhenAlreadyMember()
    {
        // Arrange
        AddMemberHandler handler = new();
        AddMember command = new()
        {
            UserId = "user-123",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a member to archived channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle AddMember when channel archived")]
    public void HandleAddMemberReturnsErrorWhenArchived()
    {
        // Arrange
        AddMemberHandler handler = new();
        AddMember command = new()
        {
            UserId = "user-456",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            IsArchived = true,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a member when channel not created returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle AddMember when channel not created")]
    public void HandleAddMemberReturnsErrorWhenNotCreated()
    {
        // Arrange
        AddMemberHandler handler = new();
        AddMember command = new()
        {
            UserId = "user-456",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a member with empty user ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle AddMember with empty user ID")]
    public void HandleAddMemberReturnsErrorWhenUserIdEmpty()
    {
        // Arrange
        AddMemberHandler handler = new();
        AddMember command = new()
        {
            UserId = string.Empty,
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a member returns a MemberAdded event.
    /// </summary>
    [Fact]
    [AllureStep("Handle AddMember when not already member")]
    public void HandleAddMemberReturnsMemberAddedEvent()
    {
        // Arrange
        AddMemberHandler handler = new();
        AddMember command = new()
        {
            UserId = "user-456",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        MemberAdded added = Assert.IsType<MemberAdded>(singleEvent);
        Assert.Equal("user-456", added.UserId);
        Assert.True(added.AddedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    /// <summary>
    ///     Verifies that removing a member from archived channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RemoveMember when channel archived")]
    public void HandleRemoveMemberReturnsErrorWhenArchived()
    {
        // Arrange
        RemoveMemberHandler handler = new();
        RemoveMember command = new()
        {
            UserId = "user-123",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            IsArchived = true,
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that removing a member when not a member returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RemoveMember when not member")]
    public void HandleRemoveMemberReturnsErrorWhenNotMember()
    {
        // Arrange
        RemoveMemberHandler handler = new();
        RemoveMember command = new()
        {
            UserId = "user-789",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that removing a member with empty user ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RemoveMember with empty user ID")]
    public void HandleRemoveMemberReturnsErrorWhenUserIdEmpty()
    {
        // Arrange
        RemoveMemberHandler handler = new();
        RemoveMember command = new()
        {
            UserId = string.Empty,
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that removing a member returns a MemberRemoved event.
    /// </summary>
    [Fact]
    [AllureStep("Handle RemoveMember when member")]
    public void HandleRemoveMemberReturnsMemberRemovedEvent()
    {
        // Arrange
        RemoveMemberHandler handler = new();
        RemoveMember command = new()
        {
            UserId = "user-123",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123", "user-456"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        MemberRemoved removed = Assert.IsType<MemberRemoved>(singleEvent);
        Assert.Equal("user-123", removed.UserId);
        Assert.True(removed.RemovedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}