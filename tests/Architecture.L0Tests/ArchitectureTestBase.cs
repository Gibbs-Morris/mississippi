using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

using ArchUnitNET.Loader;

using ArchUnitArchitecture = ArchUnitNET.Domain.Architecture;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Base class providing the shared architecture model for all architecture tests.
/// </summary>
[SuppressMessage("Performance", "CA1515:Because an application's API isn't typically referenced from outside the assembly, types can be made internal", Justification = "xUnit requires public test classes")]
public abstract class ArchitectureTestBase
{
    /// <summary>
    ///     Gets the cached architecture model containing all Mississippi assemblies.
    /// </summary>
    protected static ArchUnitArchitecture ArchitectureModel { get; } = new ArchLoader()
        .LoadAssemblies(GetMississippiAssemblies())
        .Build();

    [SuppressMessage("Reliability", "CA2022:Avoid inexact read with 'Stream.Read'", Justification = "Assembly loading is best-effort")]
    [SuppressMessage("Major Code Smell", "S3885:\"Assembly.LoadFrom\" should not be used", Justification = "Required for runtime assembly discovery")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Best-effort assembly loading")]
    private static Assembly[] GetMississippiAssemblies()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Load all Mississippi.*.dll assemblies from the output directory
        string[] assemblyPaths = Directory.GetFiles(baseDir, "Mississippi.*.dll");

        var assemblies = new List<Assembly>();
        foreach (string path in assemblyPaths)
        {
            if (path.Contains(".L0Tests.", StringComparison.Ordinal) ||
                path.Contains(".L1Tests.", StringComparison.Ordinal) ||
                path.Contains(".L2Tests.", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                assemblies.Add(Assembly.LoadFrom(path));
            }
            catch
            {
                // Skip assemblies that can't be loaded
            }
        }

        return assemblies.ToArray();
    }
}
