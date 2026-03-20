using System;


namespace Mississippi.Common.Abstractions.Diagnostics;

/// <summary>
///     Exception thrown when Mississippi builder validation fails.
/// </summary>
/// <remarks>
///     Every instance carries a stable <see cref="DiagnosticCode" /> from
///     <see cref="MississippiDiagnosticCodes" /> so tests can assert on codes
///     rather than raw message strings.
/// </remarks>
public sealed class MississippiBuilderException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiBuilderException" /> class.
    /// </summary>
    public MississippiBuilderException() => DiagnosticCode = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiBuilderException" /> class
    ///     with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public MississippiBuilderException(
        string message
    )
        : base(message) =>
        DiagnosticCode = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiBuilderException" /> class
    ///     with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MississippiBuilderException(
        string message,
        Exception innerException
    )
        : base(message, innerException) =>
        DiagnosticCode = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiBuilderException" /> class
    ///     with a diagnostic code and message.
    /// </summary>
    /// <param name="diagnosticCode">The stable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    public MississippiBuilderException(
        string diagnosticCode,
        string message
    )
        : base($"[{diagnosticCode}] {message}") =>
        DiagnosticCode = diagnosticCode;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiBuilderException" /> class
    ///     with a diagnostic code, message, and inner exception.
    /// </summary>
    /// <param name="diagnosticCode">The stable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MississippiBuilderException(
        string diagnosticCode,
        string message,
        Exception innerException
    )
        : base($"[{diagnosticCode}] {message}", innerException) =>
        DiagnosticCode = diagnosticCode;

    /// <summary>
    ///     Gets the stable diagnostic code for this validation failure.
    /// </summary>
    public string DiagnosticCode { get; }
}