using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

using Mississippi.DomainModeling.TestHarness.Architecture;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing that framework type aliases match current CLR namespaces.
/// </summary>
/// <remarks>
///     This rule validates type-level Orleans <c>[Alias]</c> values only.
///     It intentionally excludes member-level grain method aliases and generated artifacts.
/// </remarks>
public sealed class AliasAttributeArchitectureTests
{
    private static readonly AliasValidationSummary Summary = AliasValidation.AnalyzeAssemblies(
        LoadFrameworkAssemblies(),
        AliasValidationExceptionRegistry.Rules);

    private static ImmutableArray<Assembly> LoadFrameworkAssemblies()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Directory.GetFiles(baseDirectory, "Mississippi.*.dll")
            .Where(static path => !path.Contains(".Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains(".L0Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains(".L1Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains(".L2Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains(".L3Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains(".L4Tests.", StringComparison.Ordinal))
            .Where(static path => !path.Contains("Testing.Utilities", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .DistinctBy(static assembly => assembly.FullName, StringComparer.Ordinal)
            .OrderBy(static assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    /// <summary>
    ///     Verifies that the centralized exception registry is valid for framework assemblies.
    /// </summary>
    [Fact]
    public void AliasExceptionRegistryShouldBeValid()
    {
        Assert.True(Summary.ConfigurationErrors.IsEmpty, Summary.FormatReport());
    }

    /// <summary>
    ///     Verifies that framework aliases match the current fully qualified type names.
    /// </summary>
    [Fact]
    public void FrameworkAliasesShouldMatchCurrentTypeNames()
    {
        Assert.True(Summary.Mismatches.IsEmpty, Summary.FormatReport());
    }
}