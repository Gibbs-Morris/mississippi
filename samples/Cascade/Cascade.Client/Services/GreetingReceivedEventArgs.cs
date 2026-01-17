using System;


namespace Cascade.Client.Services;

/// <summary>
///     Event arguments for received greeting messages from the GreeterGrain.
/// </summary>
/// <param name="greeting">The greeting message.</param>
/// <param name="uppercaseName">The uppercase name.</param>
/// <param name="generatedAt">The timestamp when the greeting was generated.</param>
internal sealed class GreetingReceivedEventArgs(
    string greeting,
    string uppercaseName,
    DateTimeOffset generatedAt
) : EventArgs
{
    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; } = generatedAt;

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public string Greeting { get; } = greeting;

    /// <summary>
    ///     Gets the uppercase name.
    /// </summary>
    public string UppercaseName { get; } = uppercaseName;
}