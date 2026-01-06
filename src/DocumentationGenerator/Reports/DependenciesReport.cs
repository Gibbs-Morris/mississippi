using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DocumentationGenerator.Analysis;
using Mississippi.DocumentationGenerator.Output;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Generates project dependency documentation with Mermaid diagrams.
/// </summary>
public sealed class DependenciesReport : IDocumentationReport
{
    private ProjectAnalyzer Analyzer { get; }

    private DocumentationWriter Writer { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DependenciesReport" /> class.
    /// </summary>
    /// <param name="analyzer">The project analyzer.</param>
    /// <param name="writer">The documentation writer.</param>
    public DependenciesReport(
        ProjectAnalyzer analyzer,
        DocumentationWriter writer
    )
    {
        Analyzer = analyzer;
        Writer = writer;
    }

    /// <inheritdoc />
    public string Name => "Dependencies";

    /// <inheritdoc />
    public string Description => "Generates project dependency graphs for each solution";

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public Task GenerateAsync(
        ReportContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        string dependenciesDir = Path.Combine(context.OutputDirectory, "dependencies");
        Writer.WriteCategoryJson(dependenciesDir, "Dependencies", 2);

        // Generate index for dependencies
        GenerateDependenciesIndex(dependenciesDir);

        // Generate reports for each solution
        foreach (string solutionPath in context.SolutionFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            IReadOnlyList<ProjectInfo> projects = Analyzer.AnalyzeSolution(solutionPath);

            GenerateSolutionDependencyReport(dependenciesDir, solutionName, projects);
        }

        return Task.CompletedTask;
    }

    private void GenerateDependenciesIndex(
        string dependenciesDir
    )
    {
        StringBuilder content = new();
        content.AppendLine("This section contains project dependency diagrams for each solution in the repository.");
        content.AppendLine();
        content.AppendLine("## Solutions");
        content.AppendLine();
        content.AppendLine("- **[Mississippi](./mississippi-project-references)** - Framework core projects");
        content.AppendLine("- **[Samples](./samples-project-references)** - Sample applications and framework projects");

        string indexPath = Path.Combine(dependenciesDir, "index.mdx");
        Writer.WriteMdxFile(indexPath, "Project Dependencies", content.ToString(), 1);
    }

    private void GenerateSolutionDependencyReport(
        string dependenciesDir,
        string solutionName,
        IReadOnlyList<ProjectInfo> projects
    )
    {
        StringBuilder content = new();

        content.AppendLine($"Project reference diagram for the `{solutionName}.slnx` solution.");
        content.AppendLine();
        content.AppendLine("## Dependency Graph");
        content.AppendLine();
        content.AppendLine("```mermaid");
        content.AppendLine("graph TD");

        // Generate nodes and edges
        HashSet<string> addedNodes = new(StringComparer.Ordinal);

        // Sort projects for deterministic output
        IEnumerable<ProjectInfo> sortedProjects = projects
            .Where(p => !p.IsTestProject)
            .OrderBy(p => p.Name, StringComparer.Ordinal);

        foreach (ProjectInfo project in sortedProjects)
        {
            string nodeId = SanitizeNodeId(project.Name);

            if (!addedNodes.Contains(nodeId))
            {
                string nodeStyle = project.IsConsoleApp ? $"    {nodeId}[(\"{project.Name}\")]" : $"    {nodeId}[\"{project.Name}\"]";
                content.AppendLine(nodeStyle);
                addedNodes.Add(nodeId);
            }

            foreach (string reference in project.ProjectReferences.OrderBy(r => r, StringComparer.Ordinal))
            {
                string refNodeId = SanitizeNodeId(reference);
                if (!addedNodes.Contains(refNodeId))
                {
                    content.AppendLine($"    {refNodeId}[\"{reference}\"]");
                    addedNodes.Add(refNodeId);
                }

                content.AppendLine($"    {nodeId} --> {refNodeId}");
            }
        }

        content.AppendLine("```");
        content.AppendLine();

        // Add project list table
        content.AppendLine("## Project List");
        content.AppendLine();
        content.AppendLine("| Project | Type | References |");
        content.AppendLine("|---------|------|------------|");

        foreach (ProjectInfo project in projects.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            string type = project.IsTestProject ? "Test" : project.IsConsoleApp ? "Console" : "Library";
            string references = project.ProjectReferences.Count > 0
                ? string.Join(", ", project.ProjectReferences)
                : "-";
            content.AppendLine($"| {project.Name} | {type} | {references} |");
        }

        string fileName = $"{solutionName.ToLowerInvariant()}-project-references.mdx";
        string filePath = Path.Combine(dependenciesDir, fileName);
        int position = solutionName.Equals("mississippi", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
        Writer.WriteMdxFile(filePath, $"{solutionName} Project References", content.ToString(), position);
    }

    private static string SanitizeNodeId(
        string name
    )
    {
        // Replace dots and special characters with underscores for valid Mermaid node IDs
        return name.Replace(".", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal);
    }
}
