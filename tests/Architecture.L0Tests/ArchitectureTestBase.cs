using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using ArchUnitNET.Loader;

using ArchUnitArchitecture = ArchUnitNET.Domain.Architecture;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Base class providing the shared architecture model for all Mississippi assemblies.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1515:Because an application's API isn't typically referenced from outside the assembly, types can be made internal",
    Justification = "xUnit requires public test classes")]
public abstract class ArchitectureTestBase
{
    static ArchitectureTestBase()
    {
        MississippiAssemblies = GetMississippiAssemblies();
        ArchitectureModel = new ArchLoader().LoadAssemblies(MississippiAssemblies.ToArray()).Build();
    }

    /// <summary>
    ///     Gets the cached architecture model containing all Mississippi assemblies.
    /// </summary>
    protected static ArchUnitArchitecture ArchitectureModel { get; }

    /// <summary>
    ///     Gets the loaded Mississippi assemblies used by reflection-backed architecture diagnostics.
    /// </summary>
    protected static IReadOnlyList<Assembly> MississippiAssemblies { get; }

    /// <summary>
    ///     Loads assembly candidates using the runtime assembly loader.
    /// </summary>
    /// <param name="assemblyPaths">Assembly file paths to inspect.</param>
    /// <returns>Loaded Mississippi assemblies.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S3885:\"Assembly.LoadFrom\" should not be used",
        Justification = "Required for runtime assembly discovery")]
    internal static Assembly[] LoadMississippiAssemblies(
        IEnumerable<string> assemblyPaths
    ) =>
        LoadMississippiAssemblies(assemblyPaths, Assembly.LoadFrom);

    /// <summary>
    ///     Loads all non-test Mississippi assembly candidates and fails closed when discovery is incomplete.
    /// </summary>
    /// <param name="assemblyPaths">Assembly file paths to inspect.</param>
    /// <param name="loader">Assembly loader used for each candidate.</param>
    /// <returns>Loaded Mississippi assemblies.</returns>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Assembly discovery must aggregate every load failure and fail closed")]
    internal static Assembly[] LoadMississippiAssemblies(
        IEnumerable<string> assemblyPaths,
        Func<string, Assembly> loader
    )
    {
        ArgumentNullException.ThrowIfNull(assemblyPaths);
        ArgumentNullException.ThrowIfNull(loader);
        List<Assembly> assemblies = new();
        List<string> failures = new();
        int candidateCount = 0;
        foreach (string path in assemblyPaths)
        {
            string fileName = Path.GetFileName(path);
            if (fileName.Contains(".L0Tests.", StringComparison.Ordinal) ||
                fileName.Contains(".L1Tests.", StringComparison.Ordinal) ||
                fileName.Contains(".L2Tests.", StringComparison.Ordinal) ||
                fileName.Contains("Mississippi.Testing.Utilities", StringComparison.Ordinal))
            {
                continue;
            }

            candidateCount++;
            try
            {
                assemblies.Add(loader(path));
            }
            catch (Exception exception)
            {
                failures.Add($"{path}: {exception.Message}");
            }
        }

        if (candidateCount == 0)
        {
            throw new InvalidOperationException("No Mississippi architecture assembly candidates were discovered.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unable to load {failures.Count} Mississippi architecture assembly candidate(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        if (assemblies.Count == 0)
        {
            throw new InvalidOperationException("No Mississippi architecture assemblies were loaded.");
        }

        return assemblies.ToArray();
    }

    private static Assembly[] GetMississippiAssemblies()
    {
        // Keep discovery in the test base so every architecture rule shares the same fail-closed assembly set.
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] assemblyPaths = Directory.GetFiles(baseDir, "Mississippi.*.dll");
        return LoadMississippiAssemblies(assemblyPaths);
    }
}
