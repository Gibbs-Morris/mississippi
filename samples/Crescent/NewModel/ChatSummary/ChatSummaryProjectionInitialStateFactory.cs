using System.Collections.Immutable;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     Factory for creating the initial state of <see cref="ChatSummaryProjection" /> snapshots.
/// </summary>
internal sealed class ChatSummaryProjectionInitialStateFactory : IInitialStateFactory<ChatSummaryProjection>
{
    /// <inheritdoc />
    public ChatSummaryProjection Create() => new(string.Empty, 0, null, null, ImmutableHashSet<string>.Empty);
}