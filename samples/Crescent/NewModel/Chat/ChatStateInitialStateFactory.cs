using System.Collections.Immutable;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Factory for creating the initial state of <see cref="ChatState" /> snapshots.
/// </summary>
internal sealed class ChatStateInitialStateFactory : IInitialStateFactory<ChatState>
{
    /// <inheritdoc />
    public ChatState Create() =>
        new()
        {
            IsCreated = false,
            Name = string.Empty,
            Messages = ImmutableList<ChatMessage>.Empty,
            TotalMessageCount = 0,
        };
}