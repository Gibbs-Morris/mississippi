using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Internal state for the chat aggregate.
///     Maintains the last 50 messages in the chat.
/// </summary>
[SnapshotName("CRESCENT", "NEWMODEL", "CHATSTATE")]
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.ChatState")]
internal sealed record ChatState
{
    /// <summary>
    ///     The maximum number of messages to retain in state.
    /// </summary>
    public const int MaxMessages = 50;

    /// <summary>
    ///     Gets a value indicating whether the chat has been created.
    /// </summary>
    [Id(0)]
    public bool IsCreated { get; init; }

    /// <summary>
    ///     Gets the messages in the chat, limited to the last 50.
    /// </summary>
    [Id(2)]
    public ImmutableList<ChatMessage> Messages { get; init; } = [];

    /// <summary>
    ///     Gets the name of the chat.
    /// </summary>
    [Id(1)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the total count of messages ever added to the chat.
    /// </summary>
    [Id(3)]
    public int TotalMessageCount { get; init; }
}