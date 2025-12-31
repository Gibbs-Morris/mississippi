// <copyright file="ChannelReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Channel;
using Cascade.Domain.Channel.Events;
using Cascade.Domain.Channel.Reducers;

using Xunit;


namespace Cascade.Domain.L0Tests.Channel.Reducers;

/// <summary>
///     Tests for Channel reducers.
/// </summary>
[AllureSuite("Channel")]
[AllureSubSuite("Reducers")]
[AllureFeature("ChannelReducers")]
public sealed class ChannelReducerTests
{
    /// <summary>
    ///     Verifies that reducing a ChannelCreated event creates a channel state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ChannelCreated creates state")]
    public void ReduceChannelCreatedCreatesState()
    {
        // Arrange
        ChannelCreatedReducer reducer = new();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        ChannelCreated evt = new()
        {
            ChannelId = "channel-1",
            Name = "General",
            CreatedBy = "user-123",
            CreatedAt = createdAt,
        };

        // Act
        ChannelState result = reducer.Reduce(null!, evt);

        // Assert
        Assert.True(result.IsCreated);
        Assert.Equal("channel-1", result.ChannelId);
        Assert.Equal("General", result.Name);
        Assert.Equal("user-123", result.CreatedBy);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Contains("user-123", result.MemberIds);
    }

    /// <summary>
    ///     Verifies that reducing a ChannelRenamed event updates the name.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ChannelRenamed updates name")]
    public void ReduceChannelRenamedUpdatesName()
    {
        // Arrange
        ChannelRenamedReducer reducer = new();
        ChannelRenamed evt = new()
        {
            OldName = "Old Name",
            NewName = "New Name",
        };
        ChannelState state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "Old Name",
        };

        // Act
        ChannelState result = reducer.Reduce(state, evt);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("channel-1", result.ChannelId);
    }

    /// <summary>
    ///     Verifies that reducing a ChannelArchived event sets archived flag.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ChannelArchived sets archived")]
    public void ReduceChannelArchivedSetsArchived()
    {
        // Arrange
        ChannelArchivedReducer reducer = new();
        ChannelArchived evt = new()
        {
            ArchivedBy = "user-123",
            ArchivedAt = DateTimeOffset.UtcNow,
        };
        ChannelState state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            IsArchived = false,
        };

        // Act
        ChannelState result = reducer.Reduce(state, evt);

        // Assert
        Assert.True(result.IsArchived);
        Assert.Equal("channel-1", result.ChannelId);
    }

    /// <summary>
    ///     Verifies that reducing a MemberAdded event adds the member.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MemberAdded adds member")]
    public void ReduceMemberAddedAddsMember()
    {
        // Arrange
        MemberAddedReducer reducer = new();
        MemberAdded evt = new()
        {
            UserId = "user-456",
            AddedAt = DateTimeOffset.UtcNow,
        };
        ChannelState state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123"),
        };

        // Act
        ChannelState result = reducer.Reduce(state, evt);

        // Assert
        Assert.Contains("user-123", result.MemberIds);
        Assert.Contains("user-456", result.MemberIds);
        Assert.Equal(2, result.MemberIds.Count);
    }

    /// <summary>
    ///     Verifies that reducing a MemberRemoved event removes the member.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MemberRemoved removes member")]
    public void ReduceMemberRemovedRemovesMember()
    {
        // Arrange
        MemberRemovedReducer reducer = new();
        MemberRemoved evt = new()
        {
            UserId = "user-123",
            RemovedAt = DateTimeOffset.UtcNow,
        };
        ChannelState state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "General",
            MemberIds = ImmutableHashSet.Create("user-123", "user-456"),
        };

        // Act
        ChannelState result = reducer.Reduce(state, evt);

        // Assert
        Assert.DoesNotContain("user-123", result.MemberIds);
        Assert.Contains("user-456", result.MemberIds);
        Assert.Single(result.MemberIds);
    }
}
