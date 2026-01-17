using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Infrastructure;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Report that generates project dependency diagrams from .slnx solution files.
/// </summary>
public sealed class ProjectDependencyReport : IReport
{
    private ILogger<ProjectDependencyReport> Logger { get; }

    private ProjectDependencyOptions Options { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectDependencyReport" /> class.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public ProjectDependencyReport(
        IOptions<ProjectDependencyOptions> options,
        ILogger<ProjectDependencyReport> logger
    )
    {
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the name of this report.
    /// </summary>
    public string Name => "ProjectDependencies";

    /// <summary>
    ///     Gets the description of this report.
    /// </summary>
    public string Description => "Generates project dependency diagrams from solution files";

    /// <summary>
    ///     Executes the report.
    /// </summary>
    /// <param name="context">Report context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public Task ExecuteAsync(
        ReportContext context,
        CancellationToken cancellationToken
    )
    {
        string depsDir = Path.Combine(context.OutputDir, "dependencies");
        Directory.CreateDirectory(depsDir);

        // Process each solution
        List<SolutionDependencies> allSolutions = new();

        foreach (string solutionPath in context.Solutions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(solutionPath))
            {
                Logger.LogWarning("Solution file not found: {SolutionPath}", solutionPath);
                continue;
            }

            SolutionDependencies deps = ParseSlnxSolution(solutionPath, context.RepoRoot);
            allSolutions.Add(deps);

            // Generate individual solution page
            string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            string fileName = $"{solutionName.ToLowerInvariant()}-project-references.mdx";
            string content = GenerateSolutionPage(deps);
            context.Writer.WriteFile(Path.Combine(depsDir, fileName), content);

            Logger.LogInformation(
                "Generated dependency diagram for {Solution}: {ProjectCount} projects, {EdgeCount} edges",
                solutionName,
                deps.Projects.Count,
                deps.Edges.Count
            );
        }

        // Generate index page
        string indexContent = GenerateIndexPage(allSolutions);
        context.Writer.WriteFile(Path.Combine(depsDir, "index.mdx"), indexContent);

        return Task.CompletedTask;
    }

    private SolutionDependencies ParseSlnxSolution(
        string solutionPath,
        string repoRoot
    )
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? repoRoot;
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        Dictionary<string, ProjectInfo> projects = new(StringComparer.OrdinalIgnoreCase);
        List<ProjectEdge> edges = new();

        try
        {
            // Parse .slnx as XML
            XDocument doc = XDocument.Load(solutionPath);
            XElement? root = doc.Root;
            if (root == null)
            {
                return new SolutionDependencies(solutionName, new List<ProjectInfo>(), new List<ProjectEdge>());
            }

            // Find all Project elements
            IEnumerable<XElement> projectElements = root.Descendants("Project");

            foreach (XElement projectElement in projectElements)
            {
                string? projectPath = projectElement.Attribute("Path")?.Value;
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                // Normalize path separators for cross-platform support
                string normalizedPath = projectPath.Replace('\\', Path.DirectorySeparatorChar);

                // Normalize path
                string fullPath = Path.GetFullPath(Path.Combine(solutionDir, normalizedPath));
                string projectName = Path.GetFileNameWithoutExtension(normalizedPath);

                // Apply filters
                if (!ShouldIncludeProject(projectName))
                {
                    continue;
                }

                projects[projectName] = new ProjectInfo(projectName, fullPath);
            }

            // Parse each project file for references
            foreach (ProjectInfo project in projects.Values)
            {
                if (!File.Exists(project.FullPath))
                {
                    Logger.LogDebug("Project file not found: {ProjectPath}", project.FullPath);
                    continue;
                }

                try
                {
                    XDocument projDoc = XDocument.Load(project.FullPath);
                    IEnumerable<XElement> refs = projDoc.Descendants("ProjectReference");

                    foreach (XElement refElement in refs)
                    {
                        string? includePath = refElement.Attribute("Include")?.Value;
                        if (string.IsNullOrEmpty(includePath))
                        {
                            continue;
                        }

                        // Normalize path separators for cross-platform support
                        string normalizedIncludePath = includePath.Replace('\\', Path.DirectorySeparatorChar);
                        string refProjectName = Path.GetFileNameWithoutExtension(normalizedIncludePath);
                        if (projects.ContainsKey(refProjectName))
                        {
                            edges.Add(new ProjectEdge(project.Name, refProjectName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Failed to parse project file: {ProjectPath}", project.FullPath);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse solution file: {SolutionPath}", solutionPath);
        }

        // Sort for determinism
        List<ProjectInfo> sortedProjects = projects.Values.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        List<ProjectEdge> sortedEdges = edges
            .OrderBy(e => e.From, StringComparer.Ordinal)
            .ThenBy(e => e.To, StringComparer.Ordinal)
            .Distinct()
            .ToList();

        return new SolutionDependencies(solutionName, sortedProjects, sortedEdges);
    }

    private bool ShouldIncludeProject(
        string projectName
    )
    {
        // Apply include filter
        if (Options.IncludeProjects.Count > 0)
        {
            bool included = Options.IncludeProjects.Any(
                p => projectName.Contains(p, StringComparison.OrdinalIgnoreCase)
            );
            if (!included)
            {
                return false;
            }
        }

        // Apply exclude filter
        if (Options.ExcludeProjects.Count > 0)
        {
            bool excluded = Options.ExcludeProjects.Any(
                p => projectName.Contains(p, StringComparison.OrdinalIgnoreCase)
            );
            if (excluded)
            {
                return false;
            }
        }

        return true;
    }

    private static string GenerateSolutionPage(
        SolutionDependencies deps
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine($"sidebar_position: 2");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {deps.SolutionName} Project References");
        sb.AppendLine();
        sb.AppendLine(
            $"This diagram shows the project reference dependencies in the {deps.SolutionName} solution."
        );
        sb.AppendLine();
        sb.AppendLine($"**Projects:** {deps.Projects.Count} | **Dependencies:** {deps.Edges.Count}");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("graph LR");

        // Define nodes
        foreach (ProjectInfo project in deps.Projects)
        {
            string nodeId = DeterministicWriter.SanitizeId(project.Name);
            sb.AppendLine($"    {nodeId}[\"{project.Name}\"]");
        }

        sb.AppendLine();

        // Define edges
        foreach (ProjectEdge edge in deps.Edges)
        {
            string fromId = DeterministicWriter.SanitizeId(edge.From);
            string toId = DeterministicWriter.SanitizeId(edge.To);
            sb.AppendLine($"    {fromId} --> {toId}");
        }

        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Project List");
        sb.AppendLine();
        sb.AppendLine("| Project | Dependencies |");
        sb.AppendLine("|---------|--------------|");

        foreach (ProjectInfo project in deps.Projects)
        {
            int depCount = deps.Edges.Count(e => e.From == project.Name);
            sb.AppendLine($"| {project.Name} | {depCount} |");
        }

        return sb.ToString();
    }

    private static string GenerateIndexPage(
        List<SolutionDependencies> solutions
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 1");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Project Dependencies");
        sb.AppendLine();
        sb.AppendLine(
            "This section contains project dependency diagrams for the Mississippi repository solutions."
        );
        sb.AppendLine();
        sb.AppendLine("## Solutions");
        sb.AppendLine();

        foreach (SolutionDependencies solution in solutions.OrderBy(s => s.SolutionName))
        {
            string link = $"./{solution.SolutionName.ToLowerInvariant()}-project-references";
            sb.AppendLine(
                $"- [{solution.SolutionName}]({link}) - {solution.Projects.Count} projects, {solution.Edges.Count} dependencies"
            );
        }

        sb.AppendLine();
        sb.AppendLine("## Overview Diagram");
        sb.AppendLine();
        sb.AppendLine("Combined view of all framework project dependencies:");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("graph TB");
        sb.AppendLine("    subgraph Solutions");

        foreach (SolutionDependencies solution in solutions.OrderBy(s => s.SolutionName))
        {
            string nodeId = DeterministicWriter.SanitizeId(solution.SolutionName);
            sb.AppendLine($"        {nodeId}[\"{solution.SolutionName}<br/>{solution.Projects.Count} projects\"]");
        }

        sb.AppendLine("    end");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private sealed record SolutionDependencies(
        string SolutionName,
        List<ProjectInfo> Projects,
        List<ProjectEdge> Edges
    );

    private sealed record ProjectInfo(
        string Name,
        string FullPath
    );

    private sealed record ProjectEdge(
        string From,
        string To
    );
}
