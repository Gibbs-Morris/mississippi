using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
///     Generates MCP tool classes for aggregates marked with [GenerateMcpTools].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>A tools class with [McpServerToolType] for each aggregate.</item>
///         <item>One [McpServerTool] method per command marked with [GenerateCommand].</item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find aggregate types decorated with [GenerateMcpTools] and their
///         associated commands in the Commands sub-namespace.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class McpAggregateToolsGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateMcpToolsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpToolsAttribute";

    private const string GenerateMcpParameterDescriptionAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpParameterDescriptionAttribute";

    private const string GenerateMcpToolMetadataAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpToolMetadataAttribute";

    private static string BoolLiteral(
        bool value
    ) =>
        value ? "true" : "false";

    private static string EscapeForStringLiteral(
        string value
    ) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void FindAggregatesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol mcpToolsAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? mcpToolMetadataAttrSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol,
        List<McpAggregateInfo> aggregates,
        string targetRootNamespace
    )
    {
        aggregates.AddRange(
            namespaceSymbol
                .GetTypeMembers()
                .Select(typeSymbol => TryGetAggregateInfo(
                    typeSymbol,
                    mcpToolsAttrSymbol,
                    commandAttrSymbol,
                    mcpToolMetadataAttrSymbol,
                    mcpParamDescAttrSymbol,
                    targetRootNamespace))
                .Where(info => info is not null)!);

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatesInNamespace(
                childNs,
                mcpToolsAttrSymbol,
                commandAttrSymbol,
                mcpToolMetadataAttrSymbol,
                mcpParamDescAttrSymbol,
                aggregates,
                targetRootNamespace);
        }
    }

    private static List<McpCommandInfo> FindCommandsForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? mcpToolMetadataAttrSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol
    )
    {
        List<McpCommandInfo> commands = [];
        INamespaceSymbol? commandsNs = aggregateNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Commands");
        if (commandsNs is null)
        {
            return commands;
        }

        foreach (INamedTypeSymbol typeSymbol in commandsNs.GetTypeMembers())
        {
            AttributeData? attr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, commandAttrSymbol));
            if (attr is null)
            {
                continue;
            }

            (string? metadataTitle, string? metadataDescription, bool metadataDestructive, bool metadataReadOnly,
                    bool metadataIdempotent, bool metadataOpenWorld) =
                ReadToolMetadata(typeSymbol, mcpToolMetadataAttrSymbol);

            IPropertySymbol[] publicProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Where(p => !p.IsStatic)
                .Where(p => p.GetMethod is not null)
                .ToArray();

            Dictionary<string, string> parameterDescriptions =
                CollectParameterDescriptions(typeSymbol, mcpParamDescAttrSymbol);

            ImmutableArray<PropertyModel> properties = publicProperties
                .Select(p => new PropertyModel(p))
                .ToImmutableArray();

            IMethodSymbol? primaryConstructor =
                typeSymbol.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
            bool isPositionalRecord = typeSymbol.IsRecord && (primaryConstructor?.Parameters.Length > 0);

            commands.Add(
                new(
                    typeSymbol.Name,
                    TypeAnalyzer.GetFullNamespace(typeSymbol),
                    properties,
                    isPositionalRecord,
                    metadataTitle,
                    metadataDescription,
                    metadataDestructive,
                    metadataReadOnly,
                    metadataIdempotent,
                    metadataOpenWorld,
                    parameterDescriptions));
        }

        return commands;
    }

    private static Dictionary<string, string> CollectParameterDescriptions(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol
    )
    {
        Dictionary<string, string> parameterDescriptions = new();
        if (mcpParamDescAttrSymbol is null)
        {
            return parameterDescriptions;
        }

        IPropertySymbol[] publicProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic)
            .Where(p => p.GetMethod is not null)
            .ToArray();

        foreach (IPropertySymbol propSymbol in publicProperties)
        {
            AttributeData? paramDescAttr = propSymbol.GetAttributes()
                .FirstOrDefault(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpParamDescAttrSymbol));
            if (paramDescAttr is { ConstructorArguments.Length: > 0 })
            {
                string? desc = paramDescAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(desc))
                {
                    parameterDescriptions[propSymbol.Name] = desc!;
                }
            }
        }

        IMethodSymbol? primaryConstructor =
            typeSymbol.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
        bool isPositionalRecord = typeSymbol.IsRecord && (primaryConstructor?.Parameters.Length > 0);

        if (!isPositionalRecord || primaryConstructor is null)
        {
            return parameterDescriptions;
        }

        foreach (IParameterSymbol param in primaryConstructor.Parameters)
        {
            if (parameterDescriptions.ContainsKey(param.Name))
            {
                continue;
            }

            AttributeData? paramDescAttr = param.GetAttributes()
                .FirstOrDefault(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpParamDescAttrSymbol));
            if (paramDescAttr is { ConstructorArguments.Length: > 0 })
            {
                string? desc = paramDescAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(desc))
                {
                    parameterDescriptions[param.Name] = desc!;
                }
            }
        }

        return parameterDescriptions;
    }

    private static (string? Title, string? Description, bool Destructive, bool ReadOnly, bool Idempotent,
        bool OpenWorld) ReadToolMetadata(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? mcpToolMetadataAttrSymbol
    )
    {
        if (mcpToolMetadataAttrSymbol is null)
        {
            return (null, null, true, false, false, false);
        }

        AttributeData? metadataAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpToolMetadataAttrSymbol));
        if (metadataAttr is null)
        {
            return (null, null, true, false, false, false);
        }

        string? title = null;
        string? description = null;
        bool destructive = true;
        bool readOnly = false;
        bool idempotent = false;
        bool openWorld = false;

        foreach (KeyValuePair<string, TypedConstant> namedArg in metadataAttr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Title":
                    title = namedArg.Value.Value?.ToString();
                    break;
                case "Description":
                    description = namedArg.Value.Value?.ToString();
                    break;
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

        return (title, description, destructive, readOnly, idempotent, openWorld);
    }

    private static void GenerateCommandToolMethod(
        SourceBuilder sb,
        McpAggregateInfo aggregate,
        McpCommandInfo command
    )
    {
        string toolName = ToSnakeCase(aggregate.ToolPrefix + command.TypeName);
        string description = command.Description ??
                             $"Executes the {command.TypeName} command on the {aggregate.AggregateName} aggregate.";
        string methodName = command.TypeName + "Async";
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        foreach (string paramDoc in command.Properties.Select(prop =>
                     $"/// <param name=\"{NamingConventions.ToCamelCase(prop.Name)}\">The {ToHumanReadable(prop.Name)}.</param>"))
        {
            sb.AppendLine(paramDoc);
        }

        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>A message indicating the command result.</returns>");

        AppendMcpServerToolAttribute(
            sb,
            toolName,
            command.Title,
            command.Destructive,
            command.ReadOnly,
            command.Idempotent,
            command.OpenWorld);
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[Description(\"The entity identifier\")] string entityId,");

        // Generate parameters from command properties with optional custom descriptions
        for (int i = 0; i < command.Properties.Length; i++)
        {
            PropertyModel prop = command.Properties[i];
            string paramName = NamingConventions.ToCamelCase(prop.Name);
            string paramType = GetMcpParameterType(prop);
            string descriptionText = command.ParameterDescriptions.TryGetValue(prop.Name, out string? customParamDesc)
                ? EscapeForStringLiteral(customParamDesc)
                : ToHumanReadable(prop.Name);

            string defaultValue = prop.HasDefaultValue ? GetDefaultValueExpression(prop) : string.Empty;
            string trailingComma = ",";
            sb.AppendLine($"[Description(\"{descriptionText}\")] {paramType} {paramName}{defaultValue}{trailingComma}");
        }

        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Create command instance
        if (command.IsPositionalRecord)
        {
            // Positional record: new CommandType(param1, param2)
            string args = string.Join(", ", command.Properties.Select(p => NamingConventions.ToCamelCase(p.Name)));
            sb.AppendLine($"{command.TypeName} command = new({args});");
        }
        else
        {
            // Property-based record: new CommandType { Prop1 = val1, Prop2 = val2 }
            sb.Append($"{command.TypeName} command = new() ");
            sb.AppendLine("{");
            sb.IncreaseIndent();
            foreach (string assignment in command.Properties.Select(prop =>
                         $"{prop.Name} = {NamingConventions.ToCamelCase(prop.Name)},"))
            {
                sb.AppendLine(assignment);
            }

            sb.DecreaseIndent();
            sb.AppendLine("};");
        }

        sb.AppendLine($"IGenericAggregateGrain<{aggregate.AggregateTypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"AggregateGrainFactory.GetGenericAggregate<{aggregate.AggregateTypeName}>(entityId);");
        sb.DecreaseIndent();
        sb.AppendLine();
        sb.AppendLine("OperationResult result = await grain.ExecuteAsync(command, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("return result.Success");
        sb.IncreaseIndent();
        sb.AppendLine($"? $\"{command.TypeName} executed successfully on {{entityId}}.\"");
        sb.AppendLine(
            $": $\"Failed to execute {command.TypeName} on {{entityId}}: [{{result.ErrorCode}}] {{result.ErrorMessage}}\";");
        sb.DecreaseIndent();
        sb.CloseBrace();
    }

    private static void AppendMcpServerToolAttribute(
        SourceBuilder sb,
        string toolName,
        string? title,
        bool destructive,
        bool readOnly,
        bool idempotent,
        bool openWorld
    )
    {
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append("[McpServerTool(Name = \"").Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(", Title = \"").Append(EscapeForStringLiteral(title!)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = ").Append(BoolLiteral(destructive));
        toolAttrBuilder.Append(", ReadOnly = ").Append(BoolLiteral(readOnly));
        toolAttrBuilder.Append(", Idempotent = ").Append(BoolLiteral(idempotent));
        toolAttrBuilder.Append(", OpenWorld = ").Append(BoolLiteral(openWorld));
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
    }

    private static string GenerateToolsClass(
        McpAggregateInfo aggregate
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();

        // System usings
        sb.AppendUsing("System");
        sb.AppendUsing("System.ComponentModel");
        sb.AppendUsing("System.Text.Json");
        sb.AppendUsing("System.Threading");
        sb.AppendUsing("System.Threading.Tasks");
        sb.AppendLine();

        // MCP usings
        sb.AppendUsing("ModelContextProtocol.Server");
        sb.AppendLine();

        // Mississippi usings
        sb.AppendUsing("Mississippi.EventSourcing.Aggregates.Abstractions");
        sb.AppendLine();

        // Domain usings
        sb.AppendUsing(aggregate.AggregateNamespace);
        sb.AppendUsing(aggregate.CommandsNamespace);
        sb.AppendFileScopedNamespace(aggregate.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"MCP tools for {aggregate.AggregateName} aggregate commands.");
        sb.AppendGeneratedCodeAttribute("McpAggregateToolsGenerator");
        sb.AppendLine("[McpServerToolType]");
        sb.AppendLine($"public sealed class {aggregate.ToolsClassName}");
        sb.OpenBrace();

        // DI property
        sb.AppendSummary("Gets the aggregate grain factory.");
        sb.AppendLine("private IAggregateGrainFactory AggregateGrainFactory { get; }");
        sb.AppendLine();

        // Constructor
        sb.AppendSummary($"Initializes a new instance of the <see cref=\"{aggregate.ToolsClassName}\" /> class.");
        sb.AppendLine("/// <param name=\"aggregateGrainFactory\">Factory for resolving aggregate grains.</param>");
        sb.AppendLine($"public {aggregate.ToolsClassName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IAggregateGrainFactory aggregateGrainFactory");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("AggregateGrainFactory = aggregateGrainFactory;");
        sb.CloseBrace();

        // Generate one tool method per command
        foreach (McpCommandInfo command in aggregate.Commands)
        {
            sb.AppendLine();
            GenerateCommandToolMethod(sb, aggregate, command);
        }

        sb.CloseBrace();
        return sb.ToString();
    }

    private static List<McpAggregateInfo> GetAggregatesFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<McpAggregateInfo> aggregates = [];
        INamedTypeSymbol? mcpToolsAttrSymbol = compilation.GetTypeByMetadataName(GenerateMcpToolsAttributeFullName);
        INamedTypeSymbol? commandAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        if (mcpToolsAttrSymbol is null || commandAttrSymbol is null)
        {
            return aggregates;
        }

        INamedTypeSymbol? mcpToolMetadataAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpToolMetadataAttributeFullName);
        INamedTypeSymbol? mcpParamDescAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpParameterDescriptionAttributeFullName);
        foreach (IAssemblySymbol referencedAssembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            FindAggregatesInNamespace(
                referencedAssembly.GlobalNamespace,
                mcpToolsAttrSymbol,
                commandAttrSymbol,
                mcpToolMetadataAttrSymbol,
                mcpParamDescAttrSymbol,
                aggregates,
                targetRootNamespace);
        }

        return aggregates;
    }

    private static string GetDefaultValueExpression(
        PropertyModel prop
    )
    {
        // For numeric types, default to 0
        if (prop.SourceTypeName.Contains("decimal") ||
            prop.SourceTypeName.Contains("int") ||
            prop.SourceTypeName.Contains("long") ||
            prop.SourceTypeName.Contains("double") ||
            prop.SourceTypeName.Contains("float"))
        {
            return " = 0";
        }

        if (prop.SourceTypeName.Contains("string"))
        {
            return " = \"\"";
        }

        if (prop.SourceTypeName.Contains("bool"))
        {
            return " = false";
        }

        return string.Empty;
    }

    private static string GetMcpParameterType(
        PropertyModel prop
    )
    {
        // MCP tools use simple CLR types; map domain types to primitives
        string sourceType = prop.SourceTypeName;
        if (sourceType.Contains("?"))
        {
            return sourceType;
        }

        return sourceType;
    }

    private static string ToHumanReadable(
        string pascalCase
    )
    {
        if (string.IsNullOrEmpty(pascalCase))
        {
            return pascalCase;
        }

        StringBuilder sb = new();
        sb.Append(char.ToLowerInvariant(pascalCase[0]));
        for (int i = 1; i < pascalCase.Length; i++)
        {
            if (char.IsUpper(pascalCase[i]))
            {
                sb.Append(' ');
                sb.Append(char.ToLowerInvariant(pascalCase[i]));
            }
            else
            {
                sb.Append(pascalCase[i]);
            }
        }

        return sb.ToString();
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

    private static McpAggregateInfo? TryGetAggregateInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol mcpToolsAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? mcpToolMetadataAttrSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol,
        string targetRootNamespace
    )
    {
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpToolsAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        string baseName = typeSymbol.Name.EndsWith("Aggregate", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Aggregate".Length)
            : typeSymbol.Name;
        string? toolPrefix = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ToolPrefix").Value.Value?.ToString();
        if (string.IsNullOrEmpty(toolPrefix))
        {
            toolPrefix = string.Empty;
        }

        string? description = attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "Description")
            .Value.Value?.ToString();
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<McpCommandInfo> commands = containingNs is not null
            ? FindCommandsForAggregate(
                containingNs,
                commandAttrSymbol,
                mcpToolMetadataAttrSymbol,
                mcpParamDescAttrSymbol)
            : [];
        if (commands.Count == 0)
        {
            return null;
        }

        string aggregateNamespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        string commandsNamespace = aggregateNamespace + ".Commands";
        string outputNamespace = targetRootNamespace + ".McpTools";
        return new(
            baseName,
            typeSymbol.Name,
            aggregateNamespace,
            commandsNamespace,
            outputNamespace,
            toolPrefix!,
            description,
            commands);
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
        IncrementalValueProvider<List<McpAggregateInfo>> aggregatesProvider = compilationAndOptions.Select((
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
            return GetAggregatesFromCompilation(source.Compilation, targetRootNamespace);
        });
        context.RegisterSourceOutput(
            aggregatesProvider,
            static (
                spc,
                aggregates
            ) =>
            {
                foreach (McpAggregateInfo aggregate in aggregates)
                {
                    string toolsSource = GenerateToolsClass(aggregate);
                    spc.AddSource($"{aggregate.ToolsClassName}.g.cs", SourceText.From(toolsSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Holds metadata about an MCP-enabled aggregate.
    /// </summary>
    internal sealed class McpAggregateInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="McpAggregateInfo" /> class.
        /// </summary>
        /// <param name="aggregateName">The aggregate name without the "Aggregate" suffix.</param>
        /// <param name="aggregateTypeName">The full type name of the aggregate.</param>
        /// <param name="aggregateNamespace">The namespace containing the aggregate.</param>
        /// <param name="commandsNamespace">The namespace containing the aggregate commands.</param>
        /// <param name="outputNamespace">The target output namespace for generated tools.</param>
        /// <param name="toolPrefix">The prefix applied to tool names.</param>
        /// <param name="description">An optional description for the aggregate tools.</param>
        /// <param name="commands">The commands associated with this aggregate.</param>
        public McpAggregateInfo(
            string aggregateName,
            string aggregateTypeName,
            string aggregateNamespace,
            string commandsNamespace,
            string outputNamespace,
            string toolPrefix,
            string? description,
            List<McpCommandInfo> commands
        )
        {
            AggregateName = aggregateName;
            AggregateTypeName = aggregateTypeName;
            AggregateNamespace = aggregateNamespace;
            CommandsNamespace = commandsNamespace;
            OutputNamespace = outputNamespace;
            ToolPrefix = toolPrefix;
            Description = description;
            Commands = commands;
            ToolsClassName = aggregateName + "McpTools";
        }

        /// <summary>
        ///     Gets the aggregate name without the "Aggregate" suffix.
        /// </summary>
        public string AggregateName { get; }

        /// <summary>
        ///     Gets the namespace containing the aggregate.
        /// </summary>
        public string AggregateNamespace { get; }

        /// <summary>
        ///     Gets the full type name of the aggregate.
        /// </summary>
        public string AggregateTypeName { get; }

        /// <summary>
        ///     Gets the commands associated with this aggregate.
        /// </summary>
        public List<McpCommandInfo> Commands { get; }

        /// <summary>
        ///     Gets the namespace containing the aggregate commands.
        /// </summary>
        public string CommandsNamespace { get; }

        /// <summary>
        ///     Gets an optional description for the aggregate tools.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        ///     Gets the target output namespace for generated tools.
        /// </summary>
        public string OutputNamespace { get; }

        /// <summary>
        ///     Gets the prefix applied to tool names.
        /// </summary>
        public string ToolPrefix { get; }

        /// <summary>
        ///     Gets the generated tools class name.
        /// </summary>
        public string ToolsClassName { get; }
    }

    /// <summary>
    ///     Holds metadata about a command associated with an MCP-enabled aggregate.
    /// </summary>
    internal sealed class McpCommandInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="McpCommandInfo" /> class.
        /// </summary>
        /// <param name="typeName">The type name of the command.</param>
        /// <param name="namespace">The namespace containing the command.</param>
        /// <param name="properties">The public properties of the command.</param>
        /// <param name="isPositionalRecord">Whether the command is a positional record.</param>
        /// <param name="title">The optional tool title for display.</param>
        /// <param name="description">The optional tool description for LLM consumption.</param>
        /// <param name="destructive">Whether the tool performs destructive updates.</param>
        /// <param name="readOnly">Whether the tool only reads state.</param>
        /// <param name="idempotent">Whether repeated calls are safe.</param>
        /// <param name="openWorld">Whether the tool interacts with external entities.</param>
        /// <param name="parameterDescriptions">Custom descriptions keyed by property name.</param>
        public McpCommandInfo(
            string typeName,
            string @namespace,
            ImmutableArray<PropertyModel> properties,
            bool isPositionalRecord,
            string? title,
            string? description,
            bool destructive,
            bool readOnly,
            bool idempotent,
            bool openWorld,
            Dictionary<string, string> parameterDescriptions
        )
        {
            TypeName = typeName;
            Namespace = @namespace;
            Properties = properties;
            IsPositionalRecord = isPositionalRecord;
            Title = title;
            Description = description;
            Destructive = destructive;
            ReadOnly = readOnly;
            Idempotent = idempotent;
            OpenWorld = openWorld;
            ParameterDescriptions = parameterDescriptions;
        }

        /// <summary>
        ///     Gets the optional tool description for LLM consumption.
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
        ///     Gets a value indicating whether the command is a positional record.
        /// </summary>
        public bool IsPositionalRecord { get; }

        /// <summary>
        ///     Gets the namespace containing the command.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        ///     Gets a value indicating whether the tool interacts with external entities.
        /// </summary>
        public bool OpenWorld { get; }

        /// <summary>
        ///     Gets custom parameter descriptions keyed by property name.
        /// </summary>
        public Dictionary<string, string> ParameterDescriptions { get; }

        /// <summary>
        ///     Gets the public properties of the command.
        /// </summary>
        public ImmutableArray<PropertyModel> Properties { get; }

        /// <summary>
        ///     Gets a value indicating whether the tool only reads state.
        /// </summary>
        public bool ReadOnly { get; }

        /// <summary>
        ///     Gets the optional tool title for display.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        ///     Gets the type name of the command.
        /// </summary>
        public string TypeName { get; }
    }
}