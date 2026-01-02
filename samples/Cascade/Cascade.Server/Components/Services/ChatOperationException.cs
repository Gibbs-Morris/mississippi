// <copyright file="ChatOperationException.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Exception thrown when a chat operation fails.
/// </summary>
/// <remarks>
///     <para>
///         This exception wraps errors from aggregate grain operations,
///         providing a user-friendly error message and preserving the
///         error code for programmatic handling.
///     </para>
/// </remarks>
#pragma warning disable S3871 // Exception types should be "public" - internal is fine for sample application
internal sealed class ChatOperationException : Exception
#pragma warning restore S3871
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