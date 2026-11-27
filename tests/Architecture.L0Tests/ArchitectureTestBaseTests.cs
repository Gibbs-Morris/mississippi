using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Regression tests for architecture assembly discovery and fail-closed behavior.
/// </summary>
public sealed class ArchitectureTestBaseTests
{
    /// <summary>Ensures an assembly load failure is visible to the architecture suite.</summary>
    [Fact]
    public void DiscoveryFailsClosedWhenCandidateCannotLoad()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ArchitectureTestBase.LoadMississippiAssemblies(
                [@"C:\run\Mississippi.Core.dll"],
                _ => throw new BadImageFormatException("fixture load failure")));
        Assert.Contains("fixture load failure", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Ensures an empty candidate set cannot produce an empty passing model.</summary>
    [Fact]
    public void DiscoveryFailsClosedWhenNoCandidatesArePresent()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ArchitectureTestBase.LoadMississippiAssemblies(
                Array.Empty<string>(),
                _ => typeof(ArchitectureTestBase).Assembly));
        Assert.Contains("No Mississippi architecture assembly candidates", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Ensures parent directory names do not suppress production assemblies.</summary>
    [Fact]
    public void DiscoveryFiltersTestAssembliesByFileNameNotParentDirectory()
    {
        List<string> loadedPaths = new();
        string[] paths =
        [
            Path.Join("run.L0Tests.1", "Mississippi.Core.dll"),
            Path.Join("run", "Mississippi.Architecture.L0Tests.dll"),
        ];
        Assembly[] assemblies = ArchitectureTestBase.LoadMississippiAssemblies(
            paths,
            path =>
            {
                loadedPaths.Add(path);
                return typeof(ArchitectureTestBase).Assembly;
            });
        Assert.Single(assemblies);
        Assert.Equal([paths[0]], loadedPaths);
    }

    /// <summary>Ensures a missing required file cannot silently shrink standalone coverage.</summary>
    [Fact]
    public void RequiredDiscoveryFailsWhenExpectedAssemblyIsMissing()
    {
        string missingPath = Path.Combine("standalone", "Mississippi.Missing.dll");
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ArchitectureTestBase.LoadMississippiAssemblies(
                ArchitectureTestBase.GetRequiredAssemblyPaths(
                    "standalone",
                    ["Mississippi.Core", "Mississippi.Missing"]),
                path => path == missingPath
                    ? throw new FileNotFoundException("Required fixture assembly is absent.", path)
                    : typeof(ArchitectureTestBase).Assembly));
        Assert.Contains(missingPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Required fixture assembly is absent.", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Ensures all required paths are loaded in deterministic order.</summary>
    [Fact]
    public void RequiredDiscoveryLoadsEveryExpectedAssembly()
    {
        List<string> loadedPaths = new();
        Assembly[] assemblies = ArchitectureTestBase.LoadMississippiAssemblies(
            ArchitectureTestBase.GetRequiredAssemblyPaths("standalone", ["Mississippi.Second", "Mississippi.First"]),
            path =>
            {
                loadedPaths.Add(path);
                return typeof(ArchitectureTestBase).Assembly;
            });
        Assert.Equal(2, assemblies.Length);
        Assert.Equal(
            [Path.Combine("standalone", "Mississippi.First.dll"), Path.Combine("standalone", "Mississippi.Second.dll")],
            loadedPaths);
    }

    /// <summary>Ensures the source inventory is required even when no assemblies are recorded.</summary>
    [Fact]
    public void RequiredDiscoveryRejectsEmptyExpectedInventory()
    {
        Assert.Throws<InvalidOperationException>(() => ArchitectureTestBase.LoadMississippiAssemblies(
            ArchitectureTestBase.GetRequiredAssemblyPaths("standalone", []),
            _ => typeof(ArchitectureTestBase).Assembly));
    }
}