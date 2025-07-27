namespace Mississippi.EventSourcing.Cosmos;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict occurs during event storage operations.
/// </summary>
public class OptimisticConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticConcurrencyException"/> class.
    /// </summary>
    public OptimisticConcurrencyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OptimisticConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticConcurrencyException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OptimisticConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}