using System;
using System.Collections.Generic;


namespace Mississippi.Common.Builders.Abstractions;

/// <summary>
///     Exception thrown when builder validation fails at terminal host attachment.
/// </summary>
public sealed class BuilderValidationException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class.
    /// </summary>
    public BuilderValidationException() => Diagnostics = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class.
    /// </summary>
    /// <param name="message">Validation failure message.</param>
    public BuilderValidationException(
        string? message
    )
        : base(message) =>
        Diagnostics = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class.
    /// </summary>
    /// <param name="message">Validation failure message.</param>
    /// <param name="innerException">Inner exception.</param>
    public BuilderValidationException(
        string? message,
        Exception? innerException
    )
        : base(message, innerException) =>
        Diagnostics = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class.
    /// </summary>
    /// <param name="message">Validation failure message.</param>
    /// <param name="diagnostics">Associated diagnostics.</param>
    public BuilderValidationException(
        string? message,
        IReadOnlyList<BuilderDiagnostic> diagnostics
    )
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     Gets the diagnostics associated with this validation failure.
    /// </summary>
    public IReadOnlyList<BuilderDiagnostic> Diagnostics { get; }
}