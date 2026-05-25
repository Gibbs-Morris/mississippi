using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing deterministic time usage in test code.
/// </summary>
public sealed partial class TestTimeUsageArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static IEnumerable<string> EnumerateTestFiles()
    {
        string testsRoot = Path.Combine(RepositoryRoot, "tests");
        string samplesRoot = Path.Combine(RepositoryRoot, "samples");
        IEnumerable<string> repositoryTests = Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories);
        IEnumerable<string> sampleTests = Directory.EnumerateFiles(samplesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => IsSampleTestPath(path));
        return repositoryTests.Concat(sampleTests).Where(static path => !IsBuildArtifactPath(path));
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "go.ps1")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }

    [GeneratedRegex(@"\bDateTime(?:Offset)?\.(?:UtcNow|Now)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenClockApiPattern();

    private static IEnumerable<string> GetViolations(
        string path
    )
    {
        string relativePath = Path.GetRelativePath(RepositoryRoot, path);
        string[] lines = File.ReadAllLines(path);
        return lines.Select(static (
                line,
                index
            ) => new
            {
                Line = line,
                LineNumber = index + 1,
            })
            .Where(static entry => ForbiddenClockApiPattern().IsMatch(entry.Line))
            .Select(entry => $"{relativePath}:{entry.LineNumber}: {entry.Line.Trim()}");
    }

    private static bool IsBuildArtifactPath(
        string path
    ) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static bool IsSampleTestPath(
        string path
    ) =>
        path.Contains(".L0Tests", StringComparison.Ordinal) ||
        path.Contains(".L1Tests", StringComparison.Ordinal) ||
        path.Contains(".L2Tests", StringComparison.Ordinal) ||
        path.Contains(".L3Tests", StringComparison.Ordinal) ||
        path.Contains(".L4Tests", StringComparison.Ordinal);

    /// <summary>
    ///     Verifies that test code uses <see cref="TimeProvider" /> abstractions instead of direct wall-clock APIs.
    /// </summary>
    [Fact]
    public void TestFilesShouldNotUseDateTimeNowApisDirectly()
    {
        List<string> violations = EnumerateTestFiles()
            .SelectMany(GetViolations)
            .OrderBy(static violation => violation, StringComparer.Ordinal)
            .ToList();
        Assert.True(
            violations.Count == 0,
            $"Found direct DateTime/DateTimeOffset wall-clock usage in test files:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}