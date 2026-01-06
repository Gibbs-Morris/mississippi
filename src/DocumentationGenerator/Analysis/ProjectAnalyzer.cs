using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace Mississippi.DocumentationGenerator.Analysis;

/// <summary>
///     Represents information about a .NET project.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    ///     Gets or sets the project file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the project name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the project references.
    /// </summary>
    public IReadOnlyList<string> ProjectReferences { get; init; } = Array.Empty<string>();

    /// <summary>
    ///     Gets or sets the package references.
    /// </summary>
    public IReadOnlyList<string> PackageReferences { get; init; } = Array.Empty<string>();

    /// <summary>
    ///     Gets or sets whether this is a test project.
    /// </summary>
    public bool IsTestProject { get; init; }

    /// <summary>
    ///     Gets or sets whether this is a console application.
    /// </summary>
    public bool IsConsoleApp { get; init; }
}

/// <summary>
///     Analyzes .NET projects and solution files.
/// </summary>
public sealed class ProjectAnalyzer
{
    /// <summary>
    ///     Parses a solution file (.slnx format) and returns project paths.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file.</param>
    /// <returns>List of project file paths.</returns>
    public IReadOnlyList<string> ParseSolution(
        string solutionPath
    )
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");
        }

        string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        List<string> projectPaths = new();

        // Parse .slnx format (XML)
        XDocument doc = XDocument.Load(solutionPath);
        IEnumerable<XElement> projectElements = doc.Descendants("Project");

        foreach (XElement projectElement in projectElements)
        {
            string? relativePath = projectElement.Attribute("Path")?.Value;
            if (!string.IsNullOrEmpty(relativePath))
            {
                // Normalize path separators
                relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                string fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativePath));
                if (File.Exists(fullPath))
                {
                    projectPaths.Add(fullPath);
                }
            }
        }

        return projectPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    ///     Analyzes a project file and returns project information.
    /// </summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <returns>Project information.</returns>
    public ProjectInfo AnalyzeProject(
        string projectPath
    )
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        string projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;
        string projectName = Path.GetFileNameWithoutExtension(projectPath);

        XDocument doc = XDocument.Load(projectPath);

        // Parse project references
        List<string> projectReferences = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p =>
            {
                string normalizedPath = p!.Replace('\\', Path.DirectorySeparatorChar);
                string fullPath = Path.GetFullPath(Path.Combine(projectDirectory, normalizedPath));
                return Path.GetFileNameWithoutExtension(fullPath);
            })
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Parse package references
        List<string> packageReferences = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Determine if test project
        bool isTestProject = projectName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ||
                             doc.Descendants("IsTestProject").Any(e => string.Equals(e.Value, "true", StringComparison.OrdinalIgnoreCase));

        // Determine if console app
        bool isConsoleApp = doc.Descendants("OutputType")
            .Any(e => string.Equals(e.Value, "Exe", StringComparison.OrdinalIgnoreCase));

        return new ProjectInfo
        {
            FilePath = projectPath,
            Name = projectName,
            ProjectReferences = projectReferences,
            PackageReferences = packageReferences,
            IsTestProject = isTestProject,
            IsConsoleApp = isConsoleApp,
        };
    }

    /// <summary>
    ///     Analyzes all projects in a solution.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file.</param>
    /// <returns>List of project information.</returns>
    public IReadOnlyList<ProjectInfo> AnalyzeSolution(
        string solutionPath
    )
    {
        IReadOnlyList<string> projectPaths = ParseSolution(solutionPath);
        return projectPaths.Select(AnalyzeProject).ToList();
    }
}
