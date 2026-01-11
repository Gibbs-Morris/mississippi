using System;


namespace Cascade.Web.Client.Services;

/// <summary>
///     Event arguments for received greeting messages from the GreeterGrain.
/// </summary>
internal sealed class GreetingReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GreetingReceivedEventArgs" /> class.
    /// </summary>
    /// <param name="greeting">The greeting message.</param>
    /// <param name="uppercaseName">The uppercase name.</param>
    /// <param name="generatedAt">The timestamp when the greeting was generated.</param>
    public GreetingReceivedEventArgs(
        string greeting,
        string uppercaseName,
        DateTimeOffset generatedAt
    )
    {
        Greeting = greeting;
        UppercaseName = uppercaseName;
        GeneratedAt = generatedAt;
    }

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public string Greeting { get; }

    /// <summary>
    ///     Gets the uppercase name.
    /// </summary>
    public string UppercaseName { get; }

    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }
}
