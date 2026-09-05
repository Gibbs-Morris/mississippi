using System;

using Orleans;


namespace Mississippi.Brooks.Abstractions.Writer;

/// <summary>
///     Reports a cursor publication failure after the events have been durably appended.
/// </summary>
/// <remarks>
///     Consumers may retry publication for <see cref="Position" /> without appending the events again.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Brooks.Abstractions.Writer.BrookCursorPublicationException")]
public sealed class BrookCursorPublicationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookCursorPublicationException" /> class.
    /// </summary>
    public BrookCursorPublicationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookCursorPublicationException" /> class.
    /// </summary>
    /// <param name="message">The message describing the publication failure.</param>
    public BrookCursorPublicationException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookCursorPublicationException" /> class.
    /// </summary>
    /// <param name="message">The message describing the publication failure.</param>
    /// <param name="innerException">The publication failure.</param>
    public BrookCursorPublicationException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookCursorPublicationException" /> class.
    /// </summary>
    /// <param name="position">The confirmed committed position.</param>
    /// <param name="innerException">The publication failure.</param>
    public BrookCursorPublicationException(
        BrookPosition position,
        Exception innerException
    )
        : base("Events were committed, but the cursor update could not be published.", innerException) =>
        Position = position;

    /// <summary>
    ///     Gets the committed position, or the unset position when it was not supplied.
    /// </summary>
    [Id(0)]
    public BrookPosition Position { get; } = new(-1);
}