using System;
using System.Linq;

using Mississippi.DomainModeling.TestHarness.Architecture;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Tests the shared alias validation helper behavior.
/// </summary>
public sealed class AliasValidationHelperTests
{
    /// <summary>
    ///     Verifies that method-level aliases alone do not create type-level mismatches.
    /// </summary>
    [Fact]
    public void AnalyzeAssembliesShouldIgnoreMethodLevelAliasesWithoutTypeLevelAlias()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationHelperTests).Assembly],
            [
                new(
                    typeof(GenericAliasFixture<>).FullName,
                    null,
                    AliasExceptionClassification.NonContractHelper,
                    "Helper regression fixture."),
                new(
                    typeof(NamespaceMismatchFixture).FullName,
                    null,
                    AliasExceptionClassification.NonContractHelper,
                    "Helper regression fixture."),
            ]);
        Assert.DoesNotContain(
            summary.Mismatches,
            mismatch => string.Equals(
                mismatch.TypeFullName,
                typeof(IMethodAliasOnlyGrain).FullName,
                StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that stale exception rules are rejected.
    /// </summary>
    [Fact]
    public void AnalyzeAssembliesShouldRejectStaleExceptionRules()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationHelperTests).Assembly],
            [
                new("Missing.Type", null, AliasExceptionClassification.NonContractHelper, "Invalid stale rule."),
            ]);
        Assert.Contains(
            summary.ConfigurationErrors,
            static error => error.Contains("is stale", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that wildcard exception rules are rejected.
    /// </summary>
    [Fact]
    public void AnalyzeAssembliesShouldRejectWildcardExceptionRules()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationHelperTests).Assembly],
            [
                new("Mississippi.*", null, AliasExceptionClassification.NonContractHelper, "Invalid wildcard rule."),
            ]);
        Assert.Contains(
            summary.ConfigurationErrors,
            static error => error.Contains("must not use wildcard matching", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that mismatch ordering is deterministic and ordinal.
    /// </summary>
    [Fact]
    public void AnalyzeAssembliesShouldReturnDeterministicallySortedMismatches()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationHelperTests).Assembly],
            []);
        string[] actualOrder = summary.Mismatches.Select(static mismatch => mismatch.TypeFullName).ToArray();
        string[] expectedOrder = summary.Mismatches.Select(static mismatch => mismatch.TypeFullName)
            .OrderBy(static typeFullName => typeFullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedOrder, actualOrder);
    }

    /// <summary>
    ///     Verifies that generic types preserve CLR generic arity in expected aliases.
    /// </summary>
    [Fact]
    public void GetExpectedAliasShouldPreserveGenericArity()
    {
        string expectedAlias = AliasValidation.GetExpectedAlias(typeof(GenericAliasFixture<>));
        Assert.Equal(typeof(GenericAliasFixture<>).FullName, expectedAlias);
        Assert.Equal(typeof(string), GenericAliasFixture<string>.GenericArgumentType);
    }

    /// <summary>
    ///     Verifies that nested types use the CLR nested type separator in expected aliases.
    /// </summary>
    [Fact]
    public void GetExpectedAliasShouldUseNestedTypeFullName()
    {
        string expectedAlias = AliasValidation.GetExpectedAlias(typeof(NestedAliasFixtureContainer.NestedAliasFixture));
        Assert.Equal(typeof(NestedAliasFixtureContainer.NestedAliasFixture).FullName, expectedAlias);
    }

    /// <summary>
    ///     Verifies that generated-name patterns are excluded from validation.
    /// </summary>
    [Fact]
    public void IsGeneratedTypeShouldExcludeKnownGeneratedNamePatterns()
    {
        Assert.True(AliasValidation.IsGeneratedType(typeof(Codec_GeneratedAliasFixture)));
        Assert.False(AliasValidation.IsGeneratedType(typeof(NamespaceMismatchFixture)));
    }
}