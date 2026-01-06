using System;


namespace Cascade.Components.Services;

/// <summary>
///     Exception thrown when a chat operation fails.
/// </summary>
/// <remarks>
///     <para>
///         This exception wraps errors from aggregate operations,
///         providing a user-friendly error message and preserving the
///         error code for programmatic handling.
///     </para>
/// </remarks>
public sealed class ChatOperationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatOperationException" /> class.
    /// </summary>
    public ChatOperationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatOperationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ChatOperationException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatOperationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code from the operation result.</param>
    public ChatOperationException(
        string message,
        string? errorCode
    )
        : base(message) =>
        ErrorCode = errorCode;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatOperationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ChatOperationException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Gets the error code from the operation result.
    /// </summary>
    public string? ErrorCode { get; }
}