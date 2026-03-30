using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents a provider write failure with a sanitized persistence disposition.
/// </summary>
public sealed class ReplicaSinkWriteException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkWriteException" /> class.
    /// </summary>
    public ReplicaSinkWriteException()
        : base("Provider write failed.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkWriteException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ReplicaSinkWriteException(
        string message
    )
        : base(message)
    {
        FailureCode = "provider_write_failed";
        FailureSummary = message;
        Disposition = ReplicaSinkWriteFailureDisposition.Retry;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkWriteException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReplicaSinkWriteException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        FailureCode = "provider_write_failed";
        FailureSummary = message;
        Disposition = ReplicaSinkWriteFailureDisposition.Retry;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkWriteException" /> class.
    /// </summary>
    /// <param name="disposition">The persistence disposition for this failure.</param>
    /// <param name="failureCode">A stable sanitized failure code.</param>
    /// <param name="failureSummary">A sanitized failure summary safe to persist.</param>
    /// <param name="innerException">The optional underlying exception.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="failureCode" /> or <paramref name="failureSummary" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="failureCode" /> or <paramref name="failureSummary" /> is empty or whitespace.
    /// </exception>
    public ReplicaSinkWriteException(
        ReplicaSinkWriteFailureDisposition disposition,
        string failureCode,
        string failureSummary,
        Exception? innerException = null
    )
        : base(failureSummary, innerException)
    {
        ArgumentNullException.ThrowIfNull(failureCode);
        ArgumentNullException.ThrowIfNull(failureSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureSummary);
        Disposition = disposition;
        FailureCode = failureCode;
        FailureSummary = failureSummary;
    }

    /// <summary>
    ///     Gets the persistence disposition for this failure.
    /// </summary>
    public ReplicaSinkWriteFailureDisposition Disposition { get; } = ReplicaSinkWriteFailureDisposition.Retry;

    /// <summary>
    ///     Gets a stable sanitized failure code.
    /// </summary>
    public string FailureCode { get; } = "provider_write_failed";

    /// <summary>
    ///     Gets a sanitized failure summary safe to persist.
    /// </summary>
    public string FailureSummary { get; } = "Provider write failed.";
}