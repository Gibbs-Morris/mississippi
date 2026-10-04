using System.Collections.Immutable;


namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Central registry of explicit alias validation exceptions.
/// </summary>
public static class AliasValidationExceptionRegistry
{
    /// <summary>
    ///     Gets the configured alias validation exception rules.
    /// </summary>
    public static ImmutableArray<AliasValidationExceptionRule> Rules { get; } = [];
}