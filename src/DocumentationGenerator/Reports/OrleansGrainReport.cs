using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Infrastructure;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Report that generates Orleans grain call mapping diagrams.
/// </summary>
public sealed class OrleansGrainReport : IReport
{
    private ILogger<OrleansGrainReport> Logger { get; }

    private OrleansOptions Options { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrleansGrainReport" /> class.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public OrleansGrainReport(
        IOptions<OrleansOptions> options,
        ILogger<OrleansGrainReport> logger
    )
    {
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the name of this report.
    /// </summary>
    public string Name => "OrleansGrains";

    /// <summary>
    ///     Gets the description of this report.
    /// </summary>
    public string Description => "Generates Orleans grain call mapping with read-only/stateless classifications";

    /// <summary>
    ///     Executes the report.
    /// </summary>
    /// <param name="context">Report context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task ExecuteAsync(
        ReportContext context,
        CancellationToken cancellationToken
    )
    {
        string orleansDir = Path.Combine(context.OutputDir, "orleans");
        Directory.CreateDirectory(orleansDir);

        // Analyze all solutions for grain information
        GrainAnalysisResult result = await AnalyzeGrainsAsync(context, cancellationToken);

        Logger.LogInformation(
            "Orleans analysis complete: {GrainCount} grains, {InterfaceCount} interfaces, {EdgeCount} call edges",
            result.Grains.Count,
            result.Interfaces.Count,
            result.CallEdges.Count
        );

        // Generate call graph page
        string callGraphContent = GenerateCallGraphPage(result);
        context.Writer.WriteFile(Path.Combine(orleansDir, "grain-call-graph.mdx"), callGraphContent);

        // Generate call matrix page
        string matrixContent = GenerateCallMatrixPage(result);
        context.Writer.WriteFile(Path.Combine(orleansDir, "grain-call-matrix.mdx"), matrixContent);

        // Generate index page
        string indexContent = GenerateIndexPage(result);
        context.Writer.WriteFile(Path.Combine(orleansDir, "index.mdx"), indexContent);
    }

    private async Task<GrainAnalysisResult> AnalyzeGrainsAsync(
        ReportContext context,
        CancellationToken cancellationToken
    )
    {
        List<GrainInfo> grains = new();
        List<GrainInterfaceInfo> interfaces = new();
        List<GrainCallEdge> callEdges = new();

        // Collect all C# files from solutions
        HashSet<string> projectPaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (string solutionPath in context.Solutions)
        {
            foreach (string projectPath in DiscoverProjects(solutionPath, context.RepoRoot))
            {
                if (ShouldIncludeProject(projectPath))
                {
                    projectPaths.Add(projectPath);
                }
            }
        }

        // Parse all source files
        List<SyntaxTree> allTrees = new();
        Dictionary<SyntaxTree, string> treeToFile = new();

        foreach (string projectPath in projectPaths.OrderBy(p => p, StringComparer.Ordinal))
        {
            string projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
            string[] csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            foreach (string csFile in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string code = await File.ReadAllTextAsync(csFile, cancellationToken);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code, path: csFile);
                allTrees.Add(tree);
                treeToFile[tree] = csFile;
            }
        }

        if (allTrees.Count == 0)
        {
            return new GrainAnalysisResult(grains, interfaces, callEdges);
        }

        // Create compilation
        MetadataReference[] references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        CSharpCompilation compilation = CSharpCompilation.Create(
            "OrleansAnalysis",
            allTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // First pass: identify grain classes and interfaces
        Dictionary<string, GrainInfo> grainByName = new(StringComparer.Ordinal);
        Dictionary<string, GrainInterfaceInfo> interfaceByName = new(StringComparer.Ordinal);

        foreach (SyntaxTree tree in allTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SemanticModel model = compilation.GetSemanticModel(tree);
            SyntaxNode root = await tree.GetRootAsync(cancellationToken);

            // Find grain classes (implement IGrainBase)
            foreach (ClassDeclarationSyntax classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                INamedTypeSymbol? symbol = model.GetDeclaredSymbol(classDecl);
                if (symbol == null)
                {
                    continue;
                }

                if (ImplementsIGrainBase(symbol))
                {
                    GrainClassification classification = ClassifyGrain(symbol);
                    GrainInfo grain = new(
                        symbol.Name,
                        symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                        classification
                    );
                    grainByName[symbol.Name] = grain;
                }
            }

            // Find grain interfaces
            foreach (InterfaceDeclarationSyntax ifaceDecl in root.DescendantNodes()
                         .OfType<InterfaceDeclarationSyntax>())
            {
                INamedTypeSymbol? symbol = model.GetDeclaredSymbol(ifaceDecl);
                if (symbol == null)
                {
                    continue;
                }

                if (IsGrainInterface(symbol))
                {
                    // Collect method info
                    List<GrainMethodInfo> methods = new();
                    foreach (IMethodSymbol method in symbol.GetMembers().OfType<IMethodSymbol>())
                    {
                        bool isReadOnly = HasAttribute(method, "ReadOnly", "ReadOnlyAttribute");
                        methods.Add(new GrainMethodInfo(method.Name, isReadOnly));
                    }

                    GrainInterfaceInfo iface = new(
                        symbol.Name,
                        symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                        methods.OrderBy(m => m.Name, StringComparer.Ordinal).ToList()
                    );
                    interfaceByName[symbol.Name] = iface;
                }
            }
        }

        grains = grainByName.Values.OrderBy(g => g.Name, StringComparer.Ordinal).ToList();
        interfaces = interfaceByName.Values.OrderBy(i => i.Name, StringComparer.Ordinal).ToList();

        // Second pass: find grain-to-grain call edges
        foreach (SyntaxTree tree in allTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SemanticModel model = compilation.GetSemanticModel(tree);
            SyntaxNode root = await tree.GetRootAsync(cancellationToken);

            foreach (ClassDeclarationSyntax classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(classDecl);
                if (classSymbol == null || !grainByName.ContainsKey(classSymbol.Name))
                {
                    continue;
                }

                string sourceGrain = classSymbol.Name;

                // Find all invocations within this grain class
                foreach (InvocationExpressionSyntax invocation in classDecl.DescendantNodes()
                             .OfType<InvocationExpressionSyntax>())
                {
                    SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                    {
                        continue;
                    }

                    // Check if the receiver is a grain interface
                    INamedTypeSymbol? receiverType = methodSymbol.ContainingType;
                    if (receiverType == null || !interfaceByName.ContainsKey(receiverType.Name))
                    {
                        continue;
                    }

                    // Check if this is an awaited call
                    bool isAwaited = invocation.Parent is AwaitExpressionSyntax;

                    // Check if target method is read-only
                    bool isReadOnly = HasAttribute(methodSymbol, "ReadOnly", "ReadOnlyAttribute");

                    callEdges.Add(new GrainCallEdge(
                        sourceGrain,
                        receiverType.Name,
                        methodSymbol.Name,
                        isAwaited,
                        isReadOnly
                    ));
                }
            }
        }

        // Deduplicate and sort edges
        callEdges = callEdges
            .GroupBy(e => (e.SourceGrain, e.TargetInterface, e.MethodName))
            .Select(g => new GrainCallEdge(
                g.Key.SourceGrain,
                g.Key.TargetInterface,
                g.Key.MethodName,
                g.Any(e => e.IsAwaited),
                g.Any(e => e.IsReadOnly)
            ))
            .OrderBy(e => e.SourceGrain, StringComparer.Ordinal)
            .ThenBy(e => e.TargetInterface, StringComparer.Ordinal)
            .ThenBy(e => e.MethodName, StringComparer.Ordinal)
            .ToList();

        // Apply max edges cap
        bool truncated = false;
        if (callEdges.Count > Options.MaxEdges)
        {
            callEdges = callEdges.Take(Options.MaxEdges).ToList();
            truncated = true;
        }

        return new GrainAnalysisResult(grains, interfaces, callEdges, truncated);
    }

    private List<string> DiscoverProjects(
        string solutionPath,
        string repoRoot
    )
    {
        List<string> projects = new();

        if (!File.Exists(solutionPath))
        {
            return projects;
        }

        try
        {
            string solutionDir = Path.GetDirectoryName(solutionPath) ?? repoRoot;
            System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(solutionPath);
            System.Xml.Linq.XElement? root = doc.Root;
            if (root == null)
            {
                return projects;
            }

            IEnumerable<System.Xml.Linq.XElement> projectElements = root.Descendants("Project");
            foreach (System.Xml.Linq.XElement projectElement in projectElements)
            {
                string? projectPath = projectElement.Attribute("Path")?.Value;
                if (!string.IsNullOrEmpty(projectPath))
                {
                    string fullPath = Path.GetFullPath(Path.Combine(solutionDir, projectPath));
                    if (File.Exists(fullPath))
                    {
                        projects.Add(fullPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to parse solution file: {SolutionPath}", solutionPath);
        }

        return projects.Distinct().ToList();
    }

    private bool ShouldIncludeProject(
        string projectPath
    )
    {
        string projectName = Path.GetFileNameWithoutExtension(projectPath);

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

    private static bool ImplementsIGrainBase(
        INamedTypeSymbol symbol
    )
    {
        // Check all interfaces including inherited
        foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
        {
            if (iface.Name == "IGrainBase" ||
                iface.ToDisplayString().Contains("Orleans.IGrainBase") ||
                iface.ToDisplayString().Contains("Orleans.Runtime.IGrainBase"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGrainInterface(
        INamedTypeSymbol symbol
    )
    {
        // Check if interface inherits from grain interface types
        foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
        {
            string name = iface.Name;
            if (name == "IGrain" ||
                name == "IGrainWithStringKey" ||
                name == "IGrainWithGuidKey" ||
                name == "IGrainWithIntegerKey" ||
                name == "IGrainWithGuidCompoundKey" ||
                name == "IGrainWithIntegerCompoundKey" ||
                iface.ToDisplayString().Contains("Orleans.IGrain"))
            {
                return true;
            }
        }

        return false;
    }

    private static GrainClassification ClassifyGrain(
        INamedTypeSymbol symbol
    )
    {
        bool isStateless = HasAttribute(symbol, "StatelessWorker", "StatelessWorkerAttribute");
        bool isReentrant = HasAttribute(symbol, "Reentrant", "ReentrantAttribute")
                           || HasAttribute(symbol, "MayInterleave", "MayInterleaveAttribute")
                           || HasAttribute(symbol, "AlwaysInterleave", "AlwaysInterleaveAttribute");

        if (isStateless)
        {
            return GrainClassification.Stateless;
        }

        if (isReentrant)
        {
            return GrainClassification.Reentrant;
        }

        return GrainClassification.SingleActivation;
    }

    private static bool HasAttribute(
        ISymbol symbol,
        params string[] attributeNames
    )
    {
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            string? attrName = attr.AttributeClass?.Name;
            if (attrName != null && attributeNames.Any(n =>
                    attrName.Equals(n, StringComparison.Ordinal) ||
                    attrName.Equals(n + "Attribute", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private string GenerateCallGraphPage(
        GrainAnalysisResult result
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 2");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Grain Call Graph");
        sb.AppendLine();
        sb.AppendLine("This diagram shows the call relationships between Orleans grains in the Mississippi framework.");
        sb.AppendLine();
        sb.AppendLine($"**Grains:** {result.Grains.Count} | **Interfaces:** {result.Interfaces.Count} | **Call Edges:** {result.CallEdges.Count}");
        sb.AppendLine();

        if (result.WasTruncated)
        {
            sb.AppendLine(
                $"> **Note:** Graph truncated to {Options.MaxEdges} edges. Configure `orleans.maxEdges` to adjust."
            );
            sb.AppendLine();
        }

        sb.AppendLine("## Legend");
        sb.AppendLine();
        sb.AppendLine("- **[STATELESS]**: StatelessWorker grain (scales out automatically)");
        sb.AppendLine("- **[REENTRANT]**: Reentrant grain (allows concurrent calls)");
        sb.AppendLine("- **[SINGLE]**: Single-activation grain (default)");
        sb.AppendLine("- **[RO]**: Read-only method call");
        sb.AppendLine("- **[AWAIT]**: Awaited call");
        sb.AppendLine("- **[NOAWAIT]**: Non-awaited (fire-and-forget) call");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("graph LR");

        // Define grain nodes with classifications
        foreach (GrainInfo grain in result.Grains)
        {
            string nodeId = DeterministicWriter.SanitizeId(grain.Name);
            string tag = grain.Classification switch
            {
                GrainClassification.Stateless => "[STATELESS]",
                GrainClassification.Reentrant => "[REENTRANT]",
                _ => "[SINGLE]"
            };
            string style = grain.Classification switch
            {
                GrainClassification.Stateless => ":::stateless",
                GrainClassification.Reentrant => ":::reentrant",
                _ => ""
            };
            sb.AppendLine($"    {nodeId}[\"{grain.Name}<br/>{tag}\"]{style}");
        }

        sb.AppendLine();

        // Define call edges
        HashSet<string> grainNames = result.Grains.Select(g => g.Name).ToHashSet(StringComparer.Ordinal);

        foreach (GrainCallEdge edge in result.CallEdges)
        {
            // Find the target grain that implements this interface
            string targetGrain = edge.TargetInterface;
            if (targetGrain.StartsWith("I", StringComparison.Ordinal))
            {
                string possibleGrain = targetGrain[1..]; // Remove leading I
                if (grainNames.Contains(possibleGrain))
                {
                    targetGrain = possibleGrain;
                }
            }

            string sourceId = DeterministicWriter.SanitizeId(edge.SourceGrain);
            string targetId = DeterministicWriter.SanitizeId(targetGrain);

            if (sourceId == targetId)
            {
                continue;
            }

            string label = edge.MethodName;
            if (edge.IsReadOnly)
            {
                label += " [RO]";
            }

            label += edge.IsAwaited ? " [AWAIT]" : " [NOAWAIT]";

            sb.AppendLine($"    {sourceId} -->|\"{label}\"| {targetId}");
        }

        sb.AppendLine();
        sb.AppendLine("    classDef stateless fill:#9f9,stroke:#090");
        sb.AppendLine("    classDef reentrant fill:#9cf,stroke:#09c");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string GenerateCallMatrixPage(
        GrainAnalysisResult result
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 3");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Grain Call Matrix");
        sb.AppendLine();
        sb.AppendLine("Detailed view of grain-to-grain method calls.");
        sb.AppendLine();

        // Calls table
        sb.AppendLine("## All Calls");
        sb.AppendLine();
        sb.AppendLine("| Source Grain | Target Interface | Method | Read-Only | Awaited |");
        sb.AppendLine("|--------------|------------------|--------|-----------|---------|");

        foreach (GrainCallEdge edge in result.CallEdges)
        {
            string ro = edge.IsReadOnly ? "✅ RO" : "❌";
            string awaited = edge.IsAwaited ? "✅" : "⚠️ NOAWAIT";
            sb.AppendLine($"| {edge.SourceGrain} | {edge.TargetInterface} | {edge.MethodName} | {ro} | {awaited} |");
        }

        sb.AppendLine();

        // Top callers
        sb.AppendLine("## Top Callers");
        sb.AppendLine();
        sb.AppendLine("Grains that make the most outgoing calls:");
        sb.AppendLine();
        sb.AppendLine("| Grain | Outgoing Calls |");
        sb.AppendLine("|-------|----------------|");

        IOrderedEnumerable<IGrouping<string, GrainCallEdge>> topCallers = result.CallEdges
            .GroupBy(e => e.SourceGrain)
            .OrderByDescending(g => g.Count());

        foreach (IGrouping<string, GrainCallEdge> group in topCallers.Take(10))
        {
            sb.AppendLine($"| {group.Key} | {group.Count()} |");
        }

        sb.AppendLine();

        // Top callees
        sb.AppendLine("## Top Callees");
        sb.AppendLine();
        sb.AppendLine("Interfaces that receive the most calls:");
        sb.AppendLine();
        sb.AppendLine("| Interface | Incoming Calls |");
        sb.AppendLine("|-----------|----------------|");

        IOrderedEnumerable<IGrouping<string, GrainCallEdge>> topCallees = result.CallEdges
            .GroupBy(e => e.TargetInterface)
            .OrderByDescending(g => g.Count());

        foreach (IGrouping<string, GrainCallEdge> group in topCallees.Take(10))
        {
            sb.AppendLine($"| {group.Key} | {group.Count()} |");
        }

        sb.AppendLine();

        // Grain classifications
        sb.AppendLine("## Grain Classifications");
        sb.AppendLine();
        sb.AppendLine("| Grain | Classification | Namespace |");
        sb.AppendLine("|-------|----------------|-----------|");

        foreach (GrainInfo grain in result.Grains)
        {
            string classification = grain.Classification switch
            {
                GrainClassification.Stateless => "STATELESS",
                GrainClassification.Reentrant => "REENTRANT",
                _ => "SINGLE"
            };
            sb.AppendLine($"| {grain.Name} | {classification} | {grain.Namespace} |");
        }

        return sb.ToString();
    }

    private static string GenerateIndexPage(
        GrainAnalysisResult result
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 1");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Orleans Grain Analysis");
        sb.AppendLine();
        sb.AppendLine("This section contains Orleans grain call mappings for the Mississippi framework.");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("pie title Grain Classifications");

        int stateless = result.Grains.Count(g => g.Classification == GrainClassification.Stateless);
        int reentrant = result.Grains.Count(g => g.Classification == GrainClassification.Reentrant);
        int single = result.Grains.Count(g => g.Classification == GrainClassification.SingleActivation);

        if (stateless > 0)
        {
            sb.AppendLine($"    \"Stateless\" : {stateless}");
        }

        if (reentrant > 0)
        {
            sb.AppendLine($"    \"Reentrant\" : {reentrant}");
        }

        if (single > 0)
        {
            sb.AppendLine($"    \"Single Activation\" : {single}");
        }

        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Reports");
        sb.AppendLine();
        sb.AppendLine("- [Grain Call Graph](./grain-call-graph) - Visual diagram of grain interactions");
        sb.AppendLine("- [Grain Call Matrix](./grain-call-matrix) - Detailed call analysis table");
        sb.AppendLine();
        sb.AppendLine("## Grain Types");
        sb.AppendLine();
        sb.AppendLine("| Classification | Count | Description |");
        sb.AppendLine("|----------------|-------|-------------|");
        sb.AppendLine($"| Stateless | {stateless} | StatelessWorker grains that scale horizontally |");
        sb.AppendLine($"| Reentrant | {reentrant} | Grains allowing concurrent calls |");
        sb.AppendLine($"| Single | {single} | Default single-activation grains |");

        return sb.ToString();
    }

    private sealed record GrainAnalysisResult(
        List<GrainInfo> Grains,
        List<GrainInterfaceInfo> Interfaces,
        List<GrainCallEdge> CallEdges,
        bool WasTruncated = false
    );

    private sealed record GrainInfo(
        string Name,
        string Namespace,
        GrainClassification Classification
    );

    private sealed record GrainInterfaceInfo(
        string Name,
        string Namespace,
        List<GrainMethodInfo> Methods
    );

    private sealed record GrainMethodInfo(
        string Name,
        bool IsReadOnly
    );

    private sealed record GrainCallEdge(
        string SourceGrain,
        string TargetInterface,
        string MethodName,
        bool IsAwaited,
        bool IsReadOnly
    );

    private enum GrainClassification
    {
        SingleActivation,
        Stateless,
        Reentrant
    }
}
