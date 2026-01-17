using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
///     Report that generates class diagrams from source code via Roslyn analysis.
/// </summary>
public sealed class ClassDiagramReport : IReport
{
    private ILogger<ClassDiagramReport> Logger { get; }

    private ClassDiagramOptions Options { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClassDiagramReport" /> class.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public ClassDiagramReport(
        IOptions<ClassDiagramOptions> options,
        ILogger<ClassDiagramReport> logger
    )
    {
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the name of this report.
    /// </summary>
    public string Name => "ClassDiagrams";

    /// <summary>
    ///     Gets the description of this report.
    /// </summary>
    public string Description => "Generates class diagrams from source code via Roslyn analysis";

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
        string classesDir = Path.Combine(context.OutputDir, "classes");
        Directory.CreateDirectory(classesDir);

        // Discover projects from solutions
        List<ProjectTypes> allProjects = new();

        foreach (string solutionPath in context.Solutions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<string> projectPaths = DiscoverProjects(solutionPath, context.RepoRoot);

            foreach (string projectPath in projectPaths)
            {
                if (!ShouldIncludeProject(projectPath))
                {
                    continue;
                }

                ProjectTypes? projectTypes = await AnalyzeProjectAsync(projectPath, cancellationToken);
                if (projectTypes != null && projectTypes.Types.Count > 0)
                {
                    allProjects.Add(projectTypes);
                }
            }
        }

        // Sort for determinism
        allProjects = allProjects.OrderBy(p => p.ProjectName, StringComparer.Ordinal).ToList();

        // Generate per-project pages
        foreach (ProjectTypes project in allProjects)
        {
            string fileName = $"{project.ProjectName}.mdx";
            string content = GenerateProjectPage(project);
            context.Writer.WriteFile(Path.Combine(classesDir, fileName), content);

            Logger.LogInformation(
                "Generated class diagram for {Project}: {TypeCount} types",
                project.ProjectName,
                project.Types.Count
            );
        }

        // Generate index page
        string indexContent = GenerateIndexPage(allProjects);
        context.Writer.WriteFile(Path.Combine(classesDir, "index.mdx"), indexContent);
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

        return projects.Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    private bool ShouldIncludeProject(
        string projectPath
    )
    {
        string projectName = Path.GetFileNameWithoutExtension(projectPath);

        // Skip test projects
        if (projectName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ||
            projectName.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

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

    private async Task<ProjectTypes?> AnalyzeProjectAsync(
        string projectPath,
        CancellationToken cancellationToken
    )
    {
        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        string projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;

        try
        {
            // Find all C# files in project directory
            string[] csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            if (csFiles.Length == 0)
            {
                return null;
            }

            // Parse all files
            List<SyntaxTree> syntaxTrees = new();
            foreach (string csFile in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string code = await File.ReadAllTextAsync(csFile, cancellationToken);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code, path: csFile);
                syntaxTrees.Add(tree);
            }

            // Create compilation for semantic analysis
            MetadataReference[] references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                projectName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            // Extract type information
            List<TypeInfo> types = new();
            List<TypeRelationship> relationships = new();

            foreach (SyntaxTree tree in syntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                SyntaxNode root = await tree.GetRootAsync(cancellationToken);

                IEnumerable<TypeDeclarationSyntax> typeDeclarations = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>();

                foreach (TypeDeclarationSyntax typeDecl in typeDeclarations)
                {
                    INamedTypeSymbol? symbol = model.GetDeclaredSymbol(typeDecl);
                    if (symbol == null)
                    {
                        continue;
                    }

                    // Apply namespace filter
                    if (!ShouldIncludeNamespace(symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty))
                    {
                        continue;
                    }

                    // Apply public-only filter
                    if (Options.PublicOnly && symbol.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    string kind = typeDecl switch
                    {
                        ClassDeclarationSyntax => symbol.IsAbstract ? "abstract class" : "class",
                        InterfaceDeclarationSyntax => "interface",
                        RecordDeclarationSyntax r => r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                            ? "record struct"
                            : "record",
                        StructDeclarationSyntax => "struct",
                        _ => "class"
                    };

                    types.Add(new TypeInfo(
                        symbol.Name,
                        symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                        kind,
                        symbol.DeclaredAccessibility == Accessibility.Public
                    ));

                    // Find inheritance relationships
                    if (symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object)
                    {
                        relationships.Add(new TypeRelationship(
                            symbol.Name,
                            symbol.BaseType.Name,
                            "inherits"
                        ));
                    }

                    foreach (INamedTypeSymbol iface in symbol.Interfaces)
                    {
                        relationships.Add(new TypeRelationship(
                            symbol.Name,
                            iface.Name,
                            "implements"
                        ));
                    }

                    // Find composition relationships (fields/properties)
                    foreach (ISymbol member in symbol.GetMembers())
                    {
                        ITypeSymbol? memberType = member switch
                        {
                            IFieldSymbol field => field.Type,
                            IPropertySymbol prop => prop.Type,
                            _ => null
                        };

                        if (memberType is INamedTypeSymbol namedType &&
                            !namedType.IsValueType &&
                            namedType.SpecialType == SpecialType.None)
                        {
                            relationships.Add(new TypeRelationship(
                                symbol.Name,
                                namedType.Name,
                                "has"
                            ));
                        }
                    }
                }
            }

            // Apply max types limit
            if (types.Count > Options.MaxTypesPerDiagram)
            {
                types = types.Take(Options.MaxTypesPerDiagram).ToList();
            }

            // Filter relationships to only include known types
            HashSet<string> typeNames = types.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
            relationships = relationships
                .Where(r => typeNames.Contains(r.From) && typeNames.Contains(r.To))
                .Where(r => r.From != r.To) // Remove self-references
                .Distinct()
                .OrderBy(r => r.From, StringComparer.Ordinal)
                .ThenBy(r => r.To, StringComparer.Ordinal)
                .ToList();

            return new ProjectTypes(
                projectName,
                types.OrderBy(t => t.Name, StringComparer.Ordinal).ToList(),
                relationships
            );
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to analyze project: {ProjectPath}", projectPath);
            return null;
        }
    }

    private bool ShouldIncludeNamespace(
        string ns
    )
    {
        if (string.IsNullOrEmpty(ns))
        {
            return true;
        }

        // Apply include filter
        if (Options.IncludeNamespaces.Count > 0)
        {
            bool included = Options.IncludeNamespaces.Any(
                p => ns.Contains(p, StringComparison.OrdinalIgnoreCase)
            );
            if (!included)
            {
                return false;
            }
        }

        // Apply exclude filter
        if (Options.ExcludeNamespaces.Count > 0)
        {
            bool excluded = Options.ExcludeNamespaces.Any(
                p => ns.Contains(p, StringComparison.OrdinalIgnoreCase)
            );
            if (excluded)
            {
                return false;
            }
        }

        return true;
    }

    private string GenerateProjectPage(
        ProjectTypes project
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 2");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {project.ProjectName} Classes");
        sb.AppendLine();
        sb.AppendLine($"Class diagram for the {project.ProjectName} project.");
        sb.AppendLine();
        sb.AppendLine($"**Types:** {project.Types.Count} | **Relationships:** {project.Relationships.Count}");
        sb.AppendLine();

        if (project.Types.Count == Options.MaxTypesPerDiagram)
        {
            sb.AppendLine(
                $"> **Note:** Diagram truncated to {Options.MaxTypesPerDiagram} types. Configure `classDiagrams.maxTypesPerDiagram` to adjust."
            );
            sb.AppendLine();
        }

        sb.AppendLine("```mermaid");
        sb.AppendLine("classDiagram");

        // Define classes
        foreach (TypeInfo type in project.Types)
        {
            string nodeId = DeterministicWriter.SanitizeId(type.Name);
            if (type.Kind == "interface")
            {
                sb.AppendLine($"    class {nodeId} {{");
                sb.AppendLine($"        <<interface>>");
                sb.AppendLine($"    }}");
            }
            else if (type.Kind.Contains("abstract"))
            {
                sb.AppendLine($"    class {nodeId} {{");
                sb.AppendLine($"        <<abstract>>");
                sb.AppendLine($"    }}");
            }
            else if (type.Kind.Contains("record"))
            {
                sb.AppendLine($"    class {nodeId} {{");
                sb.AppendLine($"        <<record>>");
                sb.AppendLine($"    }}");
            }
            else
            {
                sb.AppendLine($"    class {nodeId}");
            }
        }

        sb.AppendLine();

        // Define relationships
        foreach (TypeRelationship rel in project.Relationships)
        {
            string fromId = DeterministicWriter.SanitizeId(rel.From);
            string toId = DeterministicWriter.SanitizeId(rel.To);

            string arrow = rel.RelationType switch
            {
                "inherits" => "<|--",
                "implements" => "<|..",
                "has" => "*--",
                _ => "-->"
            };

            sb.AppendLine($"    {toId} {arrow} {fromId}");
        }

        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Type List");
        sb.AppendLine();
        sb.AppendLine("| Type | Kind | Namespace |");
        sb.AppendLine("|------|------|-----------|");

        foreach (TypeInfo type in project.Types)
        {
            sb.AppendLine($"| {type.Name} | {type.Kind} | {type.Namespace} |");
        }

        return sb.ToString();
    }

    private static string GenerateIndexPage(
        List<ProjectTypes> projects
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine("sidebar_position: 1");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Class Diagrams");
        sb.AppendLine();
        sb.AppendLine("This section contains class diagrams generated from the Mississippi source code.");
        sb.AppendLine();
        sb.AppendLine("## Projects");
        sb.AppendLine();
        sb.AppendLine("| Project | Types | Relationships |");
        sb.AppendLine("|---------|-------|---------------|");

        foreach (ProjectTypes project in projects)
        {
            string link = $"./{project.ProjectName}";
            sb.AppendLine($"| [{project.ProjectName}]({link}) | {project.Types.Count} | {project.Relationships.Count} |");
        }

        sb.AppendLine();
        sb.AppendLine("## How These Are Generated");
        sb.AppendLine();
        sb.AppendLine("Class diagrams are generated using Roslyn source-based analysis:");
        sb.AppendLine();
        sb.AppendLine("- **Inheritance**: Shows base class relationships");
        sb.AppendLine("- **Implementation**: Shows interface implementations");
        sb.AppendLine("- **Composition**: Shows field/property type relationships");

        return sb.ToString();
    }

    private sealed record ProjectTypes(
        string ProjectName,
        List<TypeInfo> Types,
        List<TypeRelationship> Relationships
    );

    private sealed record TypeInfo(
        string Name,
        string Namespace,
        string Kind,
        bool IsPublic
    );

    private sealed record TypeRelationship(
        string From,
        string To,
        string RelationType
    );
}
