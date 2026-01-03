using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Channel;
using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;
using Cascade.Domain.Channel.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Channel.Handlers;

/// <summary>
///     Tests for <see cref="ArchiveChannelHandler" />.
/// </summary>
[AllureSuite("Channel")]
[AllureSubSuite("Handlers")]
[AllureFeature("ArchiveChannel")]
public sealed class ArchiveChannelHandlerTests
{
    /// <summary>
    ///     Verifies that archiving a channel returns a ChannelArchived event.
    /// </summary>
    [Fact]
    [AllureStep("Handle ArchiveChannel when created")]
    public void HandleReturnsChannelArchivedEventWhenCreated()
    {
        // Arrange
        ArchiveChannelHandler handler = new();
        ArchiveChannel command = new()
        {
            ArchivedBy = "user-123",
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
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        ChannelArchived archived = Assert.IsType<ChannelArchived>(singleEvent);
        Assert.Equal("user-123", archived.ArchivedBy);
        Assert.True(archived.ArchivedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    /// <summary>
    ///     Verifies that archiving an already archived channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle ArchiveChannel when already archived")]
    public void HandleReturnsErrorWhenAlreadyArchived()
    {
        // Arrange
        ArchiveChannelHandler handler = new();
        ArchiveChannel command = new()
        {
            ArchivedBy = "user-123",
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
    ///     Verifies that archiving with empty archivedBy returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle ArchiveChannel with empty archivedBy")]
    public void HandleReturnsErrorWhenArchivedByEmpty()
    {
        // Arrange
        ArchiveChannelHandler handler = new();
        ArchiveChannel command = new()
        {
            ArchivedBy = string.Empty,
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
    ///     Verifies that archiving a non-existent channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle ArchiveChannel when not created")]
    public void HandleReturnsErrorWhenNotCreated()
    {
        // Arrange
        ArchiveChannelHandler handler = new();
        ArchiveChannel command = new()
        {
            ArchivedBy = "user-123",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }
}