using System;
using System.Collections.Immutable;

using Orleans;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     Read-optimized projection of a chat for UX display.
/// </summary>
/// <param name="Name">The name of the chat.</param>
/// <param name="MessageCount">The total number of messages.</param>
/// <param name="LastMessageAt">The timestamp of the last message, if any.</param>
/// <param name="LastMessagePreview">A preview of the last message content.</param>
/// <param name="Authors">The unique authors who have participated in the chat.</param>
[GenerateSerializer]
[Alias("Crescent.NewModel.ChatSummary.ChatSummaryProjection")]
internal sealed record ChatSummaryProjection(
    [property: Id(0)] string Name,
    [property: Id(1)] int MessageCount,
    [property: Id(2)] DateTimeOffset? LastMessageAt,
    [property: Id(3)] string? LastMessagePreview,
    [property: Id(4)] ImmutableHashSet<string> Authors
)
{
    /// <summary>
    ///     The maximum length of the message preview.
    /// </summary>
    public const int PreviewMaxLength = 100;

    /// <summary>
    ///     Creates an initial empty projection.
    /// </summary>
    /// <param name="name">The name of the chat.</param>
    /// <returns>A new empty projection.</returns>
    public static ChatSummaryProjection CreateInitial(
        string name
    ) =>
        new(
            name,
            MessageCount: 0,
            LastMessageAt: null,
            LastMessagePreview: null,
            ImmutableHashSet<string>.Empty);
}
