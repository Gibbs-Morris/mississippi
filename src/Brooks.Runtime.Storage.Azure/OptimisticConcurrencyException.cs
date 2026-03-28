using System;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Exception thrown when an optimistic concurrency conflict occurs during Brooks Azure storage operations.
/// </summary>
public class OptimisticConcurrencyException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OptimisticConcurrencyException" /> class.
    /// </summary>
    public OptimisticConcurrencyException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OptimisticConcurrencyException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OptimisticConcurrencyException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OptimisticConcurrencyException" /> class with a message and
    ///     inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception.</param>
    public OptimisticConcurrencyException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}