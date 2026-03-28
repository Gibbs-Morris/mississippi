using System;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Represents an append or recovery outcome that could not be safely resolved as committed or rolled back.
/// </summary>
public sealed class BrookStorageAmbiguousOutcomeException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageAmbiguousOutcomeException" /> class.
    /// </summary>
    public BrookStorageAmbiguousOutcomeException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageAmbiguousOutcomeException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BrookStorageAmbiguousOutcomeException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageAmbiguousOutcomeException" /> class with a message
    ///     and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception.</param>
    public BrookStorageAmbiguousOutcomeException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}