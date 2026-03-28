using System;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Represents a Brooks Azure operation that should be retried after the caller rechecks committed state.
/// </summary>
public sealed class BrookStorageRetryableException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageRetryableException" /> class.
    /// </summary>
    public BrookStorageRetryableException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageRetryableException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BrookStorageRetryableException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageRetryableException" /> class with a message and
    ///     inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception.</param>
    public BrookStorageRetryableException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}