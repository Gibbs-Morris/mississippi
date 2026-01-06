namespace Cascade.WebApi.Controllers.Contracts;

/// <summary>
///     Request to send a message.
/// </summary>
/// <param name="Content">The message content.</param>
public sealed record SendMessageRequest(string Content);
