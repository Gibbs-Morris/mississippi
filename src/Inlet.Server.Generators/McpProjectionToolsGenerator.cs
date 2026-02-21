using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Server.Generators;

/// <summary>
///     Generates MCP read tool classes for projections marked with [GenerateMcpReadTool].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>A tools class with [McpServerToolType] for each projection.</item>
///         <item>A read tool method that returns the projection state as JSON.</item>
///     </list>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class McpProjectionToolsGenerator : IIncrementalGenerator
{
    private const string GenerateMcpReadToolAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpReadToolAttribute";

    private static string BoolLiteral(
        bool value
    ) =>
        value ? "true" : "false";

    private static string EscapeForStringLiteral(
        string value
    ) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void FindProjectionsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol mcpReadToolAttrSymbol,
        List<McpProjectionInfo> projections,
        string targetRootNamespace
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            McpProjectionInfo? info = TryGetProjectionInfo(typeSymbol, mcpReadToolAttrSymbol, targetRootNamespace);
            if (info is not null)
            {
                projections.Add(info);
            }
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionsInNamespace(childNs, mcpReadToolAttrSymbol, projections, targetRootNamespace);
        }
    }

    private static void GenerateReadAtVersionToolMethod(
        SourceBuilder sb,
        McpProjectionInfo projection
    )
    {
        string baseDescription = projection.Description ??
                                 $"Gets the state of the {projection.ProjectionName} projection.";
        string description = DeriveAtVersionDescription(baseDescription);
        string toolName = projection.ToolName + "_at_version";
        string methodName = "Get" + projection.ProjectionName + "AtVersionAsync";
        string title = projection.Title is not null ? projection.Title + " At Version" : string.Empty;
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        sb.AppendLine("/// <param name=\"version\">The specific version to retrieve.</param>");
        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>A JSON representation of the projection state at the specified version.</returns>");

        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append("[McpServerTool(Name = \"").Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(", Title = \"").Append(EscapeForStringLiteral(title)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = false");
        toolAttrBuilder.Append(", ReadOnly = true");
        toolAttrBuilder.Append(", Idempotent = true");
        toolAttrBuilder.Append(", OpenWorld = false");
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[Description(\"The entity identifier\")] string entityId,");
        sb.AppendLine("[Description(\"The specific version (event position) to retrieve\")] long version,");
        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine($"IUxProjectionGrain<{projection.ProjectionTypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"UxProjectionGrainFactory.GetUxProjectionGrain<{projection.ProjectionTypeName}>(entityId);");
        sb.DecreaseIndent();
        sb.AppendLine();
        sb.AppendLine(
            $"{projection.ProjectionTypeName}? projection = await grain.GetAtVersionAsync(new BrookPosition(version), cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("if (projection is null)");
        sb.OpenBrace();
        sb.AppendLine(
            $"return $\"No {projection.ProjectionName} found for entity '{{entityId}}' at version {{version}}.\";");
        sb.CloseBrace();
        sb.AppendLine();
        sb.AppendLine("return JsonSerializer.Serialize(projection, JsonSerializerOptions.Web);");
        sb.CloseBrace();
    }

    private static void GenerateReadToolMethod(
        SourceBuilder sb,
        McpProjectionInfo projection
    )
    {
        string description = projection.Description ??
                             $"Gets the current state of the {projection.ProjectionName} projection.";
        string methodName = "Get" + projection.ProjectionName + "Async";
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>A JSON representation of the projection state.</returns>");

        // Build the [McpServerTool] attribute with behavioral annotations
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append("[McpServerTool(Name = \"").Append(projection.ToolName).Append('"');
        if (!string.IsNullOrEmpty(projection.Title))
        {
            toolAttrBuilder.Append(", Title = \"").Append(EscapeForStringLiteral(projection.Title!)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = ").Append(BoolLiteral(projection.Destructive));
        toolAttrBuilder.Append(", ReadOnly = ").Append(BoolLiteral(projection.ReadOnly));
        toolAttrBuilder.Append(", Idempotent = ").Append(BoolLiteral(projection.Idempotent));
        toolAttrBuilder.Append(", OpenWorld = ").Append(BoolLiteral(projection.OpenWorld));
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[Description(\"The entity identifier\")] string entityId,");
        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine($"IUxProjectionGrain<{projection.ProjectionTypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"UxProjectionGrainFactory.GetUxProjectionGrain<{projection.ProjectionTypeName}>(entityId);");
        sb.DecreaseIndent();
        sb.AppendLine();
        sb.AppendLine($"{projection.ProjectionTypeName}? projection = await grain.GetAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("if (projection is null)");
        sb.OpenBrace();
        sb.AppendLine($"return $\"No {projection.ProjectionName} found for entity '{{entityId}}'.\";");
        sb.CloseBrace();
        sb.AppendLine();
        sb.AppendLine("return JsonSerializer.Serialize(projection, JsonSerializerOptions.Web);");
        sb.CloseBrace();
    }

    private static void GenerateReadVersionToolMethod(
        SourceBuilder sb,
        McpProjectionInfo projection
    )
    {
        string description = $"Gets the latest version number for the {projection.ProjectionName} projection.";
        string toolName = projection.ToolName + "_version";
        string methodName = "Get" + projection.ProjectionName + "VersionAsync";
        string title = projection.Title is not null ? projection.Title + " Version" : string.Empty;
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>The latest version number, or a not-found message.</returns>");

        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append("[McpServerTool(Name = \"").Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(", Title = \"").Append(EscapeForStringLiteral(title)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = false");
        toolAttrBuilder.Append(", ReadOnly = true");
        toolAttrBuilder.Append(", Idempotent = true");
        toolAttrBuilder.Append(", OpenWorld = false");
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[Description(\"The entity identifier\")] string entityId,");
        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine($"IUxProjectionGrain<{projection.ProjectionTypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"UxProjectionGrainFactory.GetUxProjectionGrain<{projection.ProjectionTypeName}>(entityId);");
        sb.DecreaseIndent();
        sb.AppendLine();
        sb.AppendLine("BrookPosition version = await grain.GetLatestVersionAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("if (version.NotSet)");
        sb.OpenBrace();
        sb.AppendLine($"return $\"No events found for {projection.ProjectionName} entity '{{entityId}}'.\";");
        sb.CloseBrace();
        sb.AppendLine();
        sb.AppendLine("return version.Value.ToString();");
        sb.CloseBrace();
    }

    private static string DeriveAtVersionDescription(
        string baseDescription
    )
    {
        // Transform a "current state" description into an "at version" variant.
        // e.g. "Retrieves the current balance..." -> "Retrieves the balance... at a specific historical version."
        // If the base description contains "current", remove it.
        string result = baseDescription
            .Replace("current ", string.Empty)
            .Replace("Current ", string.Empty);

        // Remove trailing period for appending
        if (result.EndsWith(".", StringComparison.Ordinal))
        {
            result = result.Substring(0, result.Length - 1);
        }

        return result + " at a specific historical version.";
    }

    private static string GenerateToolsClass(
        McpProjectionInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();

        // System usings
        sb.AppendUsing("System.ComponentModel");
        sb.AppendUsing("System.Text.Json");
        sb.AppendUsing("System.Threading");
        sb.AppendUsing("System.Threading.Tasks");
        sb.AppendLine();

        // MCP usings
        sb.AppendUsing("ModelContextProtocol.Server");
        sb.AppendLine();

        // Mississippi usings
        sb.AppendUsing("Mississippi.EventSourcing.Brooks.Abstractions");
        sb.AppendUsing("Mississippi.EventSourcing.UxProjections.Abstractions");
        sb.AppendLine();

        // Domain usings
        sb.AppendUsing(projection.ProjectionNamespace);
        sb.AppendFileScopedNamespace(projection.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"MCP read tools for the {projection.ProjectionName} projection.");
        sb.AppendGeneratedCodeAttribute("McpProjectionToolsGenerator");
        sb.AppendLine("[McpServerToolType]");
        sb.AppendLine($"public sealed class {projection.ToolsClassName}");
        sb.OpenBrace();

        // DI property
        sb.AppendSummary("Gets the UX projection grain factory.");
        sb.AppendLine("private IUxProjectionGrainFactory UxProjectionGrainFactory { get; }");
        sb.AppendLine();

        // Constructor
        sb.AppendSummary($"Initializes a new instance of the <see cref=\"{projection.ToolsClassName}\" /> class.");
        sb.AppendLine(
            "/// <param name=\"uxProjectionGrainFactory\">Factory for resolving UX projection grains.</param>");
        sb.AppendLine($"public {projection.ToolsClassName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IUxProjectionGrainFactory uxProjectionGrainFactory");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("UxProjectionGrainFactory = uxProjectionGrainFactory;");
        sb.CloseBrace();
        sb.AppendLine();

        // Generate read tool method (current state)
        GenerateReadToolMethod(sb, projection);
        sb.AppendLine();

        // Generate read-at-version tool method (historical state)
        GenerateReadAtVersionToolMethod(sb, projection);
        sb.AppendLine();

        // Generate version tool method (latest version number)
        GenerateReadVersionToolMethod(sb, projection);
        sb.CloseBrace();
        return sb.ToString();
    }

    private static List<McpProjectionInfo> GetProjectionsFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<McpProjectionInfo> projections = [];
        INamedTypeSymbol? mcpReadToolAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpReadToolAttributeFullName);
        if (mcpReadToolAttrSymbol is null)
        {
            return projections;
        }

        foreach (IAssemblySymbol referencedAssembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                mcpReadToolAttrSymbol,
                projections,
                targetRootNamespace);
        }

        return projections;
    }

    private static string ToSnakeCase(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        StringBuilder sb = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static McpProjectionInfo? TryGetProjectionInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol mcpReadToolAttrSymbol,
        string targetRootNamespace
    )
    {
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpReadToolAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        string baseName = typeSymbol.Name.EndsWith("Projection", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Projection".Length)
            : typeSymbol.Name;
        string? toolName = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ToolName").Value.Value?.ToString();
        if (string.IsNullOrEmpty(toolName))
        {
            toolName = "get_" + ToSnakeCase(baseName);
        }

        string? description = attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "Description")
            .Value.Value?.ToString();
        string? title = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Title").Value.Value?.ToString();

        // Read behavioral annotation defaults from the attribute (projections default to read-only)
        bool destructive = false;
        bool readOnly = true;
        bool idempotent = true;
        bool openWorld = false;
        foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Destructive":
                    destructive = namedArg.Value.Value is true;
                    break;
                case "ReadOnly":
                    readOnly = namedArg.Value.Value is true;
                    break;
                case "Idempotent":
                    idempotent = namedArg.Value.Value is true;
                    break;
                case "OpenWorld":
                    openWorld = namedArg.Value.Value is true;
                    break;
            }
        }

        string projectionNamespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        string outputNamespace = targetRootNamespace + ".McpTools";
        return new(
            baseName,
            typeSymbol.Name,
            projectionNamespace,
            outputNamespace,
            toolName!,
            description,
            title,
            destructive,
            readOnly,
            idempotent,
            openWorld);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        IncrementalValueProvider<List<McpProjectionInfo>> projectionsProvider = compilationAndOptions.Select((
            source,
            _
        ) =>
        {
            source.Options.GlobalOptions.TryGetValue(
                TargetNamespaceResolver.RootNamespaceProperty,
                out string? rootNamespace);
            source.Options.GlobalOptions.TryGetValue(
                TargetNamespaceResolver.AssemblyNameProperty,
                out string? assemblyName);
            string targetRootNamespace = TargetNamespaceResolver.GetTargetRootNamespace(
                rootNamespace,
                assemblyName,
                source.Compilation);
            return GetProjectionsFromCompilation(source.Compilation, targetRootNamespace);
        });
        context.RegisterSourceOutput(
            projectionsProvider,
            static (
                spc,
                projections
            ) =>
            {
                foreach (McpProjectionInfo projection in projections)
                {
                    string toolsSource = GenerateToolsClass(projection);
                    spc.AddSource($"{projection.ToolsClassName}.g.cs", SourceText.From(toolsSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Holds metadata about an MCP-enabled projection.
    /// </summary>
    internal sealed class McpProjectionInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="McpProjectionInfo" /> class.
        /// </summary>
        /// <param name="projectionName">The projection name without the "Projection" suffix.</param>
        /// <param name="projectionTypeName">The full type name of the projection.</param>
        /// <param name="projectionNamespace">The namespace containing the projection.</param>
        /// <param name="outputNamespace">The target output namespace for generated tools.</param>
        /// <param name="toolName">The MCP tool name.</param>
        /// <param name="description">An optional description for the read tool.</param>
        /// <param name="title">An optional human-readable title for the tool.</param>
        /// <param name="destructive">Whether the tool performs destructive updates.</param>
        /// <param name="readOnly">Whether the tool only reads state.</param>
        /// <param name="idempotent">Whether repeated calls are safe.</param>
        /// <param name="openWorld">Whether the tool interacts with external entities.</param>
        public McpProjectionInfo(
            string projectionName,
            string projectionTypeName,
            string projectionNamespace,
            string outputNamespace,
            string toolName,
            string? description,
            string? title,
            bool destructive,
            bool readOnly,
            bool idempotent,
            bool openWorld
        )
        {
            ProjectionName = projectionName;
            ProjectionTypeName = projectionTypeName;
            ProjectionNamespace = projectionNamespace;
            OutputNamespace = outputNamespace;
            ToolName = toolName;
            Description = description;
            Title = title;
            Destructive = destructive;
            ReadOnly = readOnly;
            Idempotent = idempotent;
            OpenWorld = openWorld;
            ToolsClassName = projectionName + "McpTools";
        }

        /// <summary>
        ///     Gets an optional description for the read tool.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        ///     Gets a value indicating whether the tool performs destructive updates.
        /// </summary>
        public bool Destructive { get; }

        /// <summary>
        ///     Gets a value indicating whether repeated calls are safe.
        /// </summary>
        public bool Idempotent { get; }

        /// <summary>
        ///     Gets a value indicating whether the tool interacts with external entities.
        /// </summary>
        public bool OpenWorld { get; }

        /// <summary>
        ///     Gets the target output namespace for generated tools.
        /// </summary>
        public string OutputNamespace { get; }

        /// <summary>
        ///     Gets the projection name without the "Projection" suffix.
        /// </summary>
        public string ProjectionName { get; }

        /// <summary>
        ///     Gets the namespace containing the projection.
        /// </summary>
        public string ProjectionNamespace { get; }

        /// <summary>
        ///     Gets the full type name of the projection.
        /// </summary>
        public string ProjectionTypeName { get; }

        /// <summary>
        ///     Gets a value indicating whether the tool only reads state.
        /// </summary>
        public bool ReadOnly { get; }

        /// <summary>
        ///     Gets an optional human-readable title for the tool.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        ///     Gets the MCP tool name.
        /// </summary>
        public string ToolName { get; }

        /// <summary>
        ///     Gets the generated tools class name.
        /// </summary>
        public string ToolsClassName { get; }
    }
}