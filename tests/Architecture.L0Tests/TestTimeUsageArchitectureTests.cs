using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing deterministic time usage in test code.
/// </summary>
public sealed partial class TestTimeUsageArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static int CountConsecutiveQuotes(
        string content,
        int startIndex
    )
    {
        int quoteCount = 0;
        while (((startIndex + quoteCount) < content.Length) && (content[startIndex + quoteCount] == '"'))
        {
            quoteCount++;
        }

        return quoteCount;
    }

    private static IEnumerable<string> EnumerateTestFiles()
    {
        string testsRoot = Path.Join(RepositoryRoot, "tests");
        string samplesRoot = Path.Join(RepositoryRoot, "samples");
        IEnumerable<string> repositoryTests = Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories);
        IEnumerable<string> sampleTests = Directory.EnumerateFiles(samplesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => IsSampleTestPath(path));
        return repositoryTests.Concat(sampleTests).Where(static path => !IsBuildArtifactPath(path));
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Join(current.FullName, "go.ps1")))
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
        string[] lines = StripCommentsAndStringLiterals(File.ReadAllText(path)).Split(Environment.NewLine);
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
        path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Any(static segment => IsTestProjectSegment(segment));

    private static bool IsTestProjectSegment(
        string segment
    ) =>
        segment.EndsWith(".L0Tests", StringComparison.Ordinal) ||
        segment.EndsWith(".L1Tests", StringComparison.Ordinal) ||
        segment.EndsWith(".L2Tests", StringComparison.Ordinal) ||
        segment.EndsWith(".L3Tests", StringComparison.Ordinal) ||
        segment.EndsWith(".L4Tests", StringComparison.Ordinal);

    private static string StripCommentsAndStringLiterals(
        string content
    )
    {
        StringBuilder sanitized = new(content.Length);
        bool inLineComment = false;
        bool inBlockComment = false;
        bool inRegularString = false;
        bool inVerbatimString = false;
        bool inCharacterLiteral = false;
        bool inRawString = false;
        int rawStringQuoteCount = 0;
        int index = 0;
        while (index < content.Length)
        {
            char current = content[index];
            char next = (index + 1) < content.Length ? content[index + 1] : '\0';
            if (inLineComment)
            {
                if (current == '\n')
                {
                    inLineComment = false;
                    sanitized.Append(current);
                }
                else if (current == '\r')
                {
                    sanitized.Append(current);
                }
                else
                {
                    sanitized.Append(' ');
                }

                index++;
                continue;
            }

            if (inBlockComment)
            {
                if ((current == '*') && (next == '/'))
                {
                    sanitized.Append("  ");
                    index += 2;
                    inBlockComment = false;
                }
                else
                {
                    sanitized.Append(current is '\r' or '\n' ? current : ' ');
                    index++;
                }

                continue;
            }

            if (inRegularString)
            {
                if ((current == '\\') && (next != '\0'))
                {
                    sanitized.Append("  ");
                    index += 2;
                }
                else if (current == '"')
                {
                    sanitized.Append(' ');
                    inRegularString = false;
                    index++;
                }
                else
                {
                    sanitized.Append(current is '\r' or '\n' ? current : ' ');
                    index++;
                }

                continue;
            }

            if (inVerbatimString)
            {
                if ((current == '"') && (next == '"'))
                {
                    sanitized.Append("  ");
                    index += 2;
                }
                else if (current == '"')
                {
                    sanitized.Append(' ');
                    inVerbatimString = false;
                    index++;
                }
                else
                {
                    sanitized.Append(current is '\r' or '\n' ? current : ' ');
                    index++;
                }

                continue;
            }

            if (inCharacterLiteral)
            {
                if ((current == '\\') && (next != '\0'))
                {
                    sanitized.Append("  ");
                    index += 2;
                }
                else if (current == '\'')
                {
                    sanitized.Append(' ');
                    inCharacterLiteral = false;
                    index++;
                }
                else
                {
                    sanitized.Append(current is '\r' or '\n' ? current : ' ');
                    index++;
                }

                continue;
            }

            if (inRawString)
            {
                if (current == '"')
                {
                    int quoteCount = CountConsecutiveQuotes(content, index);
                    if (quoteCount >= rawStringQuoteCount)
                    {
                        sanitized.Append(new string(' ', quoteCount));
                        index += quoteCount;
                        inRawString = false;
                        continue;
                    }
                }

                sanitized.Append(current is '\r' or '\n' ? current : ' ');
                index++;
                continue;
            }

            if ((current == '/') && (next == '/'))
            {
                sanitized.Append("  ");
                index += 2;
                inLineComment = true;
                continue;
            }

            if ((current == '/') && (next == '*'))
            {
                sanitized.Append("  ");
                index += 2;
                inBlockComment = true;
                continue;
            }

            if (((current == '$') || (current == '@')) &&
                ((index + 2) < content.Length) &&
                (((current == '$') && (next == '@') && (content[index + 2] == '"')) ||
                 ((current == '@') && (next == '$') && (content[index + 2] == '"'))))
            {
                sanitized.Append("   ");
                index += 3;
                inVerbatimString = true;
                continue;
            }

            if ((current == '@') && (next == '"'))
            {
                sanitized.Append("  ");
                index += 2;
                inVerbatimString = true;
                continue;
            }

            if ((current == '$') && (next == '"'))
            {
                sanitized.Append("  ");
                index += 2;
                inRegularString = true;
                continue;
            }

            if (current == '"')
            {
                int quoteCount = CountConsecutiveQuotes(content, index);
                if (quoteCount >= 3)
                {
                    rawStringQuoteCount = quoteCount;
                    sanitized.Append(new string(' ', quoteCount));
                    index += quoteCount;
                    inRawString = true;
                }
                else
                {
                    sanitized.Append(' ');
                    inRegularString = true;
                    index++;
                }

                continue;
            }

            if (current == '\'')
            {
                sanitized.Append(' ');
                inCharacterLiteral = true;
                index++;
                continue;
            }

            sanitized.Append(current);
            index++;
        }

        return sanitized.ToString();
    }

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
        const int maxDisplayedViolations = 20;
        IEnumerable<string> displayedViolations = violations.Take(maxDisplayedViolations);
        string additionalViolationsMessage = violations.Count > maxDisplayedViolations
            ? $"{Environment.NewLine}... and {violations.Count - maxDisplayedViolations} more."
            : string.Empty;
        Assert.True(
            violations.Count == 0,
            $"Found direct DateTime/DateTimeOffset wall-clock usage in test files:{Environment.NewLine}{string.Join(Environment.NewLine, displayedViolations)}{additionalViolationsMessage}");
    }
}