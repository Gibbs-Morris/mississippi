using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mississippi.DomainModeling.TestHarness.Architecture;

using Moq;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing Orleans alias alignment and documented identity exceptions.
/// </summary>
public sealed class OrleansSerializationAliasArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    ///     Analyzes complete assemblies using the shared type-alias exception policy.
    /// </summary>
    /// <param name="assemblies">The assemblies to validate.</param>
    /// <param name="exceptionRules">The explicit, justified identity exceptions.</param>
    /// <returns>The shared alias validation summary.</returns>
    internal static AliasValidationSummary AnalyzeAliases(
        IEnumerable<Assembly> assemblies,
        IEnumerable<AliasValidationExceptionRule> exceptionRules
    )
    {
        Assembly[] completeAssemblies = assemblies.ToArray();
        foreach (Assembly assembly in completeAssemblies)
        {
            // The shared validator tolerates partial type loads; this architecture audit must fail closed.
            _ = assembly.GetTypes();
        }

        return AliasValidation.AnalyzeAssemblies(completeAssemblies, exceptionRules);
    }

    /// <summary>Verifies that partial type loading cannot silently omit an invalid alias.</summary>
    [Fact]
    public void AliasAuditShouldRejectPartialTypeLoads()
    {
        ReflectionTypeLoadException expected = new(
            [typeof(NamespaceMismatchFixture), null],
            [new TypeLoadException("Fixture type could not be loaded.")]);
        Mock<Assembly> assembly = new();
        assembly.SetupGet(candidate => candidate.FullName).Returns("Fixture");
        assembly.Setup(candidate => candidate.GetName()).Returns(new AssemblyName("Fixture"));
        assembly.Setup(candidate => candidate.GetTypes()).Throws(expected);
        ReflectionTypeLoadException exception = Assert.Throws<ReflectionTypeLoadException>(() =>
            AnalyzeAliases([assembly.Object], []));
        Assert.Same(expected, exception);
    }

    /// <summary>Verifies that a justified exception preserves one identity without hiding other mismatches.</summary>
    [Fact]
    public void AliasAuditShouldRespectJustifiedExceptionsWithoutHidingOtherMismatches()
    {
        Assembly fixtureAssembly = typeof(NamespaceMismatchFixture).Assembly;
        AliasValidationSummary withoutException = AnalyzeAliases([fixtureAssembly], []);
        Assert.Contains(
            withoutException.Mismatches,
            mismatch => mismatch.TypeFullName == typeof(NamespaceMismatchFixture).FullName);
        AliasValidationExceptionRule preservedIdentity = new(
            typeof(NamespaceMismatchFixture).FullName,
            null,
            AliasExceptionClassification.IntentionalPreservedIdentity,
            "Preserve this fixture's earlier message identity.");
        AliasValidationSummary summary = AnalyzeAliases([fixtureAssembly], [preservedIdentity]);
        Assert.Empty(summary.ConfigurationErrors);
        Assert.DoesNotContain(
            summary.Mismatches,
            mismatch => mismatch.TypeFullName == typeof(NamespaceMismatchFixture).FullName);
        Assert.Equal(preservedIdentity, Assert.Single(summary.ActiveExceptions));
        Assert.Contains(
            summary.Mismatches,
            mismatch => (mismatch.TypeFullName == typeof(GenericAliasFixture<>).FullName) &&
                        (mismatch.ActualAlias == "Wrong.GenericAliasFixture"));
    }

    /// <summary>
    ///     Verifies current type aliases through the central registry after complete assembly loading.
    /// </summary>
    [Fact]
    public void OrleansTypeAliasesShouldMatchCurrentFullTypeName()
    {
        AliasValidationSummary summary = AnalyzeAliases(MississippiAssemblies, AliasValidationExceptionRegistry.Rules);
        Assert.True(summary.ConfigurationErrors.IsEmpty, summary.FormatReport());
        Assert.True(summary.Mismatches.IsEmpty, summary.FormatReport());
    }
}