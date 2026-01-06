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
///     Generates Orleans grain documentation with call mapping and classifications.
/// </summary>
public sealed class OrleansGrainReport : IDocumentationReport
{
    private GrainAnalyzer GrainAnalyzer { get; }

    private ProjectAnalyzer ProjectAnalyzer { get; }

    private DocumentationWriter Writer { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrleansGrainReport" /> class.
    /// </summary>
    /// <param name="grainAnalyzer">The grain analyzer.</param>
    /// <param name="projectAnalyzer">The project analyzer.</param>
    /// <param name="writer">The documentation writer.</param>
    public OrleansGrainReport(
        GrainAnalyzer grainAnalyzer,
        ProjectAnalyzer projectAnalyzer,
        DocumentationWriter writer
    )
    {
        GrainAnalyzer = grainAnalyzer;
        ProjectAnalyzer = projectAnalyzer;
        Writer = writer;
    }

    /// <inheritdoc />
    public string Name => "OrleansGrains";

    /// <inheritdoc />
    public string Description => "Generates Orleans grain call mapping with classifications";

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public Task GenerateAsync(
        ReportContext context,
        CancellationToken cancellationToken = default
    )
    {
        string grainsDir = Path.Combine(context.OutputDirectory, "grains");
        Writer.WriteCategoryJson(grainsDir, "Orleans Grains", 3);

        // Collect all grains from all solutions
        List<GrainInfo> allGrains = new();

        foreach (string solutionPath in context.SolutionFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<string> projectPaths = ProjectAnalyzer.ParseSolution(solutionPath);

            foreach (string projectPath in projectPaths)
            {
                IReadOnlyList<GrainInfo> grains = GrainAnalyzer.AnalyzeProject(projectPath);
                allGrains.AddRange(grains);
            }
        }

        // Deduplicate grains by class name (in case of overlapping solutions)
        List<GrainInfo> uniqueGrains = allGrains
            .GroupBy(g => g.DisplayName, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(g => g.DisplayName, StringComparer.Ordinal)
            .ToList();

        // Generate index
        GenerateGrainsIndex(grainsDir, uniqueGrains);

        // Generate call mapping diagram
        GenerateCallMappingDiagram(grainsDir, uniqueGrains);

        // Generate classifications table
        GenerateClassificationsPage(grainsDir, uniqueGrains);

        return Task.CompletedTask;
    }

    private void GenerateGrainsIndex(
        string grainsDir,
        IReadOnlyList<GrainInfo> grains
    )
    {
        StringBuilder content = new();

        content.AppendLine("This section contains documentation for Orleans grains in the Mississippi framework.");
        content.AppendLine();
        content.AppendLine("## Overview");
        content.AppendLine();
        content.AppendLine($"The framework contains **{grains.Count}** Orleans grains implementing `IGrainBase`.");
        content.AppendLine();

        int statelessCount = grains.Count(g => g.IsStatelessWorker);
        int reentrantCount = grains.Count(g => g.IsReentrant);
        int withReadOnlyCount = grains.Count(g => g.ReadOnlyMethods.Count > 0);

        content.AppendLine("### Classification Summary");
        content.AppendLine();
        content.AppendLine("| Classification | Count |");
        content.AppendLine("|---------------|-------|");
        content.AppendLine($"| Stateless Workers | {statelessCount} |");
        content.AppendLine($"| Reentrant | {reentrantCount} |");
        content.AppendLine($"| With ReadOnly Methods | {withReadOnlyCount} |");
        content.AppendLine();
        content.AppendLine("## Documentation");
        content.AppendLine();
        content.AppendLine("- **[Call Mapping](./call-mapping)** - Visual diagram of grain-to-grain calls");
        content.AppendLine("- **[Classifications](./classifications)** - Detailed table of grain attributes");

        string indexPath = Path.Combine(grainsDir, "index.mdx");
        Writer.WriteMdxFile(indexPath, "Orleans Grains", content.ToString(), 1);
    }

    private void GenerateCallMappingDiagram(
        string grainsDir,
        IReadOnlyList<GrainInfo> grains
    )
    {
        StringBuilder content = new();

        content.AppendLine("Visual representation of grain-to-grain call relationships.");
        content.AppendLine();
        content.AppendLine("## Call Graph");
        content.AppendLine();
        content.AppendLine("```mermaid");
        content.AppendLine("graph TD");

        // Define class styles
        content.AppendLine("    classDef stateless fill:#90EE90,stroke:#228B22");
        content.AppendLine("    classDef reentrant fill:#87CEEB,stroke:#4682B4");
        content.AppendLine("    classDef normal fill:#FFF8DC,stroke:#DAA520");
        content.AppendLine();

        HashSet<string> addedNodes = new(StringComparer.Ordinal);
        List<string> edges = new();
        List<string> statelessNodes = new();
        List<string> reentrantNodes = new();

        foreach (GrainInfo grain in grains)
        {
            string nodeId = SanitizeNodeId(grain.ClassName);
            string displayName = grain.ClassName;

            if (!addedNodes.Contains(nodeId))
            {
                content.AppendLine($"    {nodeId}[\"{displayName}\"]");
                addedNodes.Add(nodeId);

                if (grain.IsStatelessWorker)
                {
                    statelessNodes.Add(nodeId);
                }
                else if (grain.IsReentrant)
                {
                    reentrantNodes.Add(nodeId);
                }
            }

            foreach (string call in grain.GrainCalls)
            {
                // Strip 'I' prefix from interface name to get grain class name
                string grainName = call.StartsWith("I", StringComparison.Ordinal) ? call.Substring(1) : call;
                string targetId = SanitizeNodeId(grainName);
                string edge = $"    {nodeId} --> {targetId}";
                if (!edges.Contains(edge, StringComparer.Ordinal))
                {
                    edges.Add(edge);
                }
            }
        }

        // Add edges sorted for determinism
        foreach (string edge in edges.OrderBy(e => e, StringComparer.Ordinal))
        {
            content.AppendLine(edge);
        }

        // Apply class styles
        if (statelessNodes.Count > 0)
        {
            content.AppendLine();
            content.AppendLine($"    class {string.Join(",", statelessNodes.OrderBy(n => n, StringComparer.Ordinal))} stateless");
        }

        if (reentrantNodes.Count > 0)
        {
            content.AppendLine($"    class {string.Join(",", reentrantNodes.OrderBy(n => n, StringComparer.Ordinal))} reentrant");
        }

        content.AppendLine("```");
        content.AppendLine();
        content.AppendLine("## Legend");
        content.AppendLine();
        content.AppendLine("- 🟢 **Green** - Stateless Worker grains");
        content.AppendLine("- 🔵 **Blue** - Reentrant grains");
        content.AppendLine("- 🟡 **Yellow** - Normal grains");

        string filePath = Path.Combine(grainsDir, "call-mapping.mdx");
        Writer.WriteMdxFile(filePath, "Grain Call Mapping", content.ToString(), 2);
    }

    private void GenerateClassificationsPage(
        string grainsDir,
        IReadOnlyList<GrainInfo> grains
    )
    {
        StringBuilder content = new();

        content.AppendLine("Detailed classification of all Orleans grains in the framework.");
        content.AppendLine();
        content.AppendLine("## Grain Classifications");
        content.AppendLine();
        content.AppendLine("| Grain | Stateless | Reentrant | ReadOnly Methods |");
        content.AppendLine("|-------|-----------|-----------|-----------------|");

        foreach (GrainInfo grain in grains)
        {
            string stateless = grain.IsStatelessWorker ? "✅" : "❌";
            string reentrant = grain.IsReentrant ? "✅" : "❌";
            string readOnlyMethods = grain.ReadOnlyMethods.Count > 0
                ? string.Join(", ", grain.ReadOnlyMethods)
                : "-";

            content.AppendLine($"| {grain.ClassName} | {stateless} | {reentrant} | {readOnlyMethods} |");
        }

        content.AppendLine();
        content.AppendLine("## Classification Descriptions");
        content.AppendLine();
        content.AppendLine("### Stateless Worker");
        content.AppendLine();
        content.AppendLine("Grains marked with `[StatelessWorker]` can have multiple activations per silo, enabling parallel request processing. They should not maintain mutable state.");
        content.AppendLine();
        content.AppendLine("### Reentrant");
        content.AppendLine();
        content.AppendLine("Grains marked with `[Reentrant]` allow multiple requests to interleave execution. This improves throughput but requires careful handling of shared state.");
        content.AppendLine();
        content.AppendLine("### ReadOnly Methods");
        content.AppendLine();
        content.AppendLine("Methods marked with `[ReadOnly]` indicate they do not modify grain state and can be safely interleaved with other read operations.");

        string filePath = Path.Combine(grainsDir, "classifications.mdx");
        Writer.WriteMdxFile(filePath, "Grain Classifications", content.ToString(), 3);
    }

    private static string SanitizeNodeId(
        string name
    )
    {
        return name.Replace(".", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("<", "_", StringComparison.Ordinal)
            .Replace(">", "_", StringComparison.Ordinal);
    }
}
