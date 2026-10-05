namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Defines a narrow allowlisted alias exception.
/// </summary>
/// <param name="TypeFullName">The specific CLR type full name to match.</param>
/// <param name="ExpectedAlias">The specific expected alias string to match.</param>
/// <param name="Classification">The reason class for the exception.</param>
/// <param name="Reason">The human-readable reason for the exception.</param>
/// <param name="Owner">Optional ownership or review note.</param>
public sealed record AliasValidationExceptionRule(
    string? TypeFullName,
    string? ExpectedAlias,
    AliasExceptionClassification Classification,
    string Reason,
    string? Owner = null
);