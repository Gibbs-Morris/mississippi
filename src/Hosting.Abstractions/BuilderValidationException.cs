using System;
using System.Collections.Generic;
using System.Linq;


namespace Mississippi.Hosting.Abstractions;

/// <summary>
///     Reports structured failures that prevent a Mississippi builder from attaching to a host.
/// </summary>
/// <remarks>Public so host applications can inspect and report composition diagnostics.</remarks>
public sealed class BuilderValidationException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class without diagnostics.
    /// </summary>
    public BuilderValidationException()
        : base("The Mississippi builder composition is invalid.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BuilderValidationException(
        string? message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class with an underlying cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public BuilderValidationException(
        string? message,
        Exception? innerException
    )
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderValidationException" /> class.
    /// </summary>
    /// <param name="diagnostics">The nonempty collection of composition failures.</param>
    public BuilderValidationException(
        IEnumerable<BuilderDiagnostic> diagnostics
    )
        : this(CopyDiagnostics(diagnostics))
    {
    }

    private BuilderValidationException(
        BuilderDiagnostic[] diagnostics
    )
        : base(
            string.Join(
                Environment.NewLine,
                diagnostics.Select(diagnostic =>
                    $"{diagnostic.Code}: {diagnostic.Message} {diagnostic.Remediation}"))) =>
        Diagnostics = Array.AsReadOnly(diagnostics);

    /// <summary>
    ///     Gets an immutable snapshot of the composition failures, empty for standard message-only constructors.
    /// </summary>
    public IReadOnlyList<BuilderDiagnostic> Diagnostics { get; } = [];

    private static BuilderDiagnostic[] CopyDiagnostics(
        IEnumerable<BuilderDiagnostic> diagnostics
    )
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        BuilderDiagnostic[] snapshot = diagnostics.ToArray();
        if ((snapshot.Length == 0) || snapshot.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException("Provide at least one non-null builder diagnostic.", nameof(diagnostics));
        }

        return snapshot;
    }
}