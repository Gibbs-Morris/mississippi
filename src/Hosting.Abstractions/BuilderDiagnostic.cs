using System;


namespace Mississippi.Hosting.Abstractions;

/// <summary>
///     Describes a composition failure and the action required to correct it.
/// </summary>
/// <remarks>Public so consumers can report builder failures without parsing exception messages.</remarks>
public sealed record BuilderDiagnostic
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BuilderDiagnostic" /> class.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The description of the invalid composition.</param>
    /// <param name="remediation">The action required to correct the composition.</param>
    public BuilderDiagnostic(
        string code,
        string message,
        string remediation
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(remediation);
        Code = code;
        Message = message;
        Remediation = remediation;
    }

    /// <summary>
    ///     Gets the stable diagnostic code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     Gets the description of the invalid composition.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the action required to correct the composition.
    /// </summary>
    public string Remediation { get; }
}