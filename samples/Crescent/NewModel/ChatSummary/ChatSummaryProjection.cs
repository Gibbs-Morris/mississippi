using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     Read-optimized projection of a chat for UX display.
/// </summary>
[SnapshotStorageName("CRESCENT", "NEWMODEL", "CHATSUMMARY")]
[GenerateSerializer]
[Alias("Crescent.NewModel.ChatSummary.ChatSummaryProjection")]
internal sealed record ChatSummaryProjection
{
    /// <summary>
    ///     The maximum length of the message preview.
    /// </summary>
    public const int PreviewMaxLength = 100;

    /// <summary>
    ///     Gets the unique authors who have participated in the chat.
    /// </summary>
    [Id(4)]
    public ImmutableHashSet<string> Authors { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    ///     Gets the timestamp of the last message, if any.
    /// </summary>
    [Id(2)]
    public DateTimeOffset? LastMessageAt { get; init; }

    /// <summary>
    ///     Gets a preview of the last message content.
    /// </summary>
    [Id(3)]
    public string? LastMessagePreview { get; init; }

    /// <summary>
    ///     Gets the total number of messages.
    /// </summary>
    [Id(1)]
    public int MessageCount { get; init; }

    /// <summary>
    ///     Gets the name of the chat.
    /// </summary>
    [Id(0)]
    public string Name { get; init; } = string.Empty;
}