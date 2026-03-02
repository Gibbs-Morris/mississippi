using System.Collections.Generic;


namespace Mississippi.Common.Builders.Abstractions;

/// <summary>
///     Structured validation diagnostic emitted during builder finalization.
/// </summary>
/// <param name="ErrorCode">Stable error code for programmatic handling.</param>
/// <param name="Category">Diagnostic category.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
/// <param name="RemediationHint">Suggested remediation guidance.</param>
/// <param name="Context">Optional diagnostic context payload.</param>
public sealed record BuilderDiagnostic(
    string ErrorCode,
    string Category,
    string Message,
    string RemediationHint,
    IReadOnlyDictionary<string, string>? Context = null
);