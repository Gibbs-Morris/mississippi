namespace Cascade.Web.Contracts;

// TEMPORARY PLUMBING - TO BE REPLACED BY INLET
// This DTO exists to pass command data via HTTP to the BFF server.
// Once Inlet is integrated, commands will be dispatched directly via IInletStore
// and this DTO may no longer be needed.

/// <summary>
///     Request to send a message in a conversation.
/// </summary>
public sealed record SendMessageRequest
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    public required string SentBy { get; init; }
}