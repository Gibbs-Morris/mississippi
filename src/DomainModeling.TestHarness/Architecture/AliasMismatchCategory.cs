namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Identifies the kind of alias mismatch found by the validator.
/// </summary>
public enum AliasMismatchCategory
{
    /// <summary>
    ///     The alias does not match the current fully qualified CLR type name.
    /// </summary>
    AliasDoesNotMatchCurrentTypeName,
}