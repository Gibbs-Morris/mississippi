namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Represents one alias mismatch discovered by the validator.
/// </summary>
/// <param name="AssemblyName">The assembly containing the type.</param>
/// <param name="TypeFullName">The current CLR type full name.</param>
/// <param name="TypeCategory">The categorized type kind.</param>
/// <param name="ActualAlias">The current alias value found on the type.</param>
/// <param name="ExpectedAlias">The alias value expected from the current type identity.</param>
/// <param name="MismatchCategory">The mismatch classification.</param>
public sealed record AliasValidationResult(
    string AssemblyName,
    string TypeFullName,
    AliasTypeCategory TypeCategory,
    string ActualAlias,
    string ExpectedAlias,
    AliasMismatchCategory MismatchCategory
);