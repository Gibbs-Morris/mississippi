using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace Mississippi.DocumentationGenerator.Analysis;

/// <summary>
///     Represents information about an Orleans grain.
/// </summary>
public sealed class GrainInfo
{
    /// <summary>
    ///     Gets or sets the grain class name.
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the grain interface name.
    /// </summary>
    public string InterfaceName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether this grain is a stateless worker.
    /// </summary>
    public bool IsStatelessWorker { get; init; }

    /// <summary>
    ///     Gets or sets whether this grain is reentrant.
    /// </summary>
    public bool IsReentrant { get; init; }

    /// <summary>
    ///     Gets or sets the methods with [ReadOnly] attribute.
    /// </summary>
    public IReadOnlyList<string> ReadOnlyMethods { get; init; } = Array.Empty<string>();

    /// <summary>
    ///     Gets or sets the grain interfaces this grain calls.
    /// </summary>
    public IReadOnlyList<string> GrainCalls { get; init; } = Array.Empty<string>();

    /// <summary>
    ///     Gets the display name for the grain.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Namespace) ? ClassName : $"{Namespace}.{ClassName}";
}

/// <summary>
///     Analyzes C# source files to find Orleans grains.
/// </summary>
public sealed partial class GrainAnalyzer
{
    private static readonly Regex NamespaceRegex = GetNamespaceRegex();
    private static readonly Regex ClassDeclarationRegex = GetClassDeclarationRegex();
    private static readonly Regex InterfaceImplementationRegex = GetInterfaceImplementationRegex();
    private static readonly Regex StatelessWorkerRegex = GetStatelessWorkerRegex();
    private static readonly Regex ReentrantRegex = GetReentrantRegex();
    private static readonly Regex ReadOnlyMethodRegex = GetReadOnlyMethodRegex();
    private static readonly Regex GrainCallRegex = GetGrainCallRegex();
    private static readonly Regex GrainFactoryCallRegex = GetGrainFactoryCallRegex();

    /// <summary>
    ///     Analyzes all C# files in a project directory for Orleans grains.
    /// </summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <returns>List of grain information.</returns>
    public IReadOnlyList<GrainInfo> AnalyzeProject(
        string projectPath
    )
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        string? projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return Array.Empty<GrainInfo>();
        }

        List<GrainInfo> grains = new();

        foreach (string csFile in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            // Skip obj and bin directories
            if (csFile.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                csFile.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            GrainInfo? grainInfo = AnalyzeFile(csFile);
            if (grainInfo != null)
            {
                grains.Add(grainInfo);
            }
        }

        return grains.OrderBy(g => g.DisplayName, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    ///     Analyzes a single C# file for Orleans grain implementation.
    /// </summary>
    /// <param name="filePath">Path to the C# file.</param>
    /// <returns>Grain information if the file contains a grain, null otherwise.</returns>
    public GrainInfo? AnalyzeFile(
        string filePath
    )
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string content = File.ReadAllText(filePath);

        // Check if this file implements IGrainBase
        if (!content.Contains("IGrainBase", StringComparison.Ordinal))
        {
            return null;
        }

        // Extract namespace
        Match namespaceMatch = NamespaceRegex.Match(content);
        string ns = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : string.Empty;

        // Extract class declaration
        Match classMatch = ClassDeclarationRegex.Match(content);
        if (!classMatch.Success)
        {
            return null;
        }

        string className = classMatch.Groups[1].Value;

        // Check if class implements IGrainBase (either directly or through interface list)
        string implementsList = classMatch.Groups[2].Value;
        if (!implementsList.Contains("IGrainBase", StringComparison.Ordinal))
        {
            return null;
        }

        // Extract interface name if implementing a grain interface
        Match interfaceMatch = InterfaceImplementationRegex.Match(implementsList);
        string interfaceName = interfaceMatch.Success ? interfaceMatch.Groups[1].Value : string.Empty;

        // Check for [StatelessWorker] attribute
        bool isStatelessWorker = StatelessWorkerRegex.IsMatch(content);

        // Check for [Reentrant] attribute
        bool isReentrant = ReentrantRegex.IsMatch(content);

        // Find [ReadOnly] methods (these are typically on interfaces, but we'll look in both)
        List<string> readOnlyMethods = ReadOnlyMethodRegex.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        // Find grain calls (GetGrain<T>, grain factory calls, etc.)
        HashSet<string> grainCalls = new(StringComparer.Ordinal);

        foreach (Match match in GrainCallRegex.Matches(content))
        {
            grainCalls.Add(match.Groups[1].Value);
        }

        foreach (Match match in GrainFactoryCallRegex.Matches(content))
        {
            // Factory method names like GetBrookReaderGrain capture "BrookReaderGrain"
            // Prefix with 'I' to match interface naming convention
            string grainName = match.Groups[1].Value;
            grainCalls.Add("I" + grainName);
        }

        return new GrainInfo
        {
            ClassName = className,
            InterfaceName = interfaceName,
            Namespace = ns,
            FilePath = filePath,
            IsStatelessWorker = isStatelessWorker,
            IsReentrant = isReentrant,
            ReadOnlyMethods = readOnlyMethods,
            GrainCalls = grainCalls.OrderBy(c => c, StringComparer.Ordinal).ToList(),
        };
    }

    [GeneratedRegex(@"namespace\s+([\w.]+)\s*[;{]", RegexOptions.Compiled)]
    private static partial Regex GetNamespaceRegex();

    [GeneratedRegex(@"(?:public|internal|private|protected)\s+(?:sealed\s+|abstract\s+)*class\s+(\w+)(?:<[^>]+>)?\s*:\s*([^{]+)", RegexOptions.Compiled)]
    private static partial Regex GetClassDeclarationRegex();

    [GeneratedRegex(@"(I\w+Grain)\b", RegexOptions.Compiled)]
    private static partial Regex GetInterfaceImplementationRegex();

    [GeneratedRegex(@"\[StatelessWorker(?:\([^)]*\))?\]", RegexOptions.Compiled)]
    private static partial Regex GetStatelessWorkerRegex();

    [GeneratedRegex(@"\[Reentrant\]", RegexOptions.Compiled)]
    private static partial Regex GetReentrantRegex();

    [GeneratedRegex(@"\[ReadOnly\]\s*(?:\[[^\]]*\]\s*)*(?:public|internal)?\s*(?:async\s+)?(?:Task|ValueTask)(?:<[^>]+>)?\s+(\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex GetReadOnlyMethodRegex();

    [GeneratedRegex(@"GetGrain<(I\w+Grain)>", RegexOptions.Compiled)]
    private static partial Regex GetGrainCallRegex();

    [GeneratedRegex(@"Get(\w+Grain)\s*\(", RegexOptions.Compiled)]
    private static partial Regex GetGrainFactoryCallRegex();
}
