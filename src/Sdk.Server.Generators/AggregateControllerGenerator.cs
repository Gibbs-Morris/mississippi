using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Sdk.Generators.Core.Analysis;
using Mississippi.Sdk.Generators.Core.Emit;
using Mississippi.Sdk.Generators.Core.Naming;


namespace Mississippi.Sdk.Server.Generators;

/// <summary>
///     Generates server-side aggregate controllers for aggregates marked with [GenerateAggregateEndpoints].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>Controller class inheriting from AggregateControllerBase&lt;TAggregate&gt;.</item>
///         <item>Action methods for each command marked with [GenerateCommand].</item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find aggregate types decorated with [GenerateAggregateEndpoints] and their
///         associated commands in the Commands sub-namespace.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AggregateControllerGenerator : IIncrementalGenerator
{
    private const string GenerateAggregateEndpointsAttributeFullName =
        "Mississippi.Sdk.Generators.Abstractions.GenerateAggregateEndpointsAttribute";

    private const string GenerateCommandAttributeFullName =
        "Mississippi.Sdk.Generators.Abstractions.GenerateCommandAttribute";

    /// <summary>
    ///     Recursively finds aggregates in a namespace.
    /// </summary>
    private static void FindAggregatesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
        List<AggregateInfo> aggregates
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            AggregateInfo? info = TryGetAggregateInfo(typeSymbol, aggregateAttrSymbol, commandAttrSymbol);
            if (info is not null)
            {
                aggregates.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatesInNamespace(childNs, aggregateAttrSymbol, commandAttrSymbol, aggregates);
        }
    }

    /// <summary>
    ///     Finds commands in the Commands sub-namespace of an aggregate.
    /// </summary>
    private static List<CommandModel> FindCommandsForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol commandAttrSymbol
    )
    {
        List<CommandModel> commands = [];

        // Look for Commands sub-namespace
        INamespaceSymbol? commandsNs = aggregateNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Commands");
        if (commandsNs is null)
        {
            return commands;
        }

        // Find all types with [GenerateCommand]
        foreach (INamedTypeSymbol typeSymbol in commandsNs.GetTypeMembers())
        {
            AttributeData? attr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, commandAttrSymbol));
            if (attr is null)
            {
                continue;
            }

            // Get Route from named argument, fallback to kebab-case of type name
            string? route = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Route").Value.Value?.ToString();
            if (string.IsNullOrEmpty(route))
            {
                route = NamingConventions.ToKebabCase(typeSymbol.Name);
            }

            // Get HttpMethod from named argument, default to POST
            string httpMethod = attr.NamedArguments
                                    .FirstOrDefault(kvp => kvp.Key == "HttpMethod")
                                    .Value.Value?.ToString() ??
                                "POST";
            commands.Add(new(typeSymbol, route!, httpMethod));
        }

        return commands;
    }

    /// <summary>
    ///     Generates an action method for a command.
    /// </summary>
    private static void GenerateActionMethod(
        SourceBuilder sb,
        CommandModel command
    )
    {
        string methodName = command.TypeName + "Async";
        string httpAttribute = command.HttpMethod.ToUpperInvariant() switch
        {
            "GET" => "HttpGet",
            "PUT" => "HttpPut",
            "DELETE" => "HttpDelete",
            "PATCH" => "HttpPatch",
            var _ => "HttpPost",
        };
        sb.AppendSummary($"Executes the {command.TypeName} command.");
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        sb.AppendLine("/// <param name=\"request\">The command request.</param>");
        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>The operation result.</returns>");
        sb.AppendLine($"[{httpAttribute}(\"{command.Route}\")]");
        sb.AppendLine($"public Task<ActionResult<OperationResult>> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[FromRoute] string entityId,");
        sb.AppendLine($"[FromBody] {command.DtoTypeName} request,");
        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(request);");
        sb.AppendLine(
            $"return ExecuteAsync(entityId, {command.TypeName}Mapper.Map(request), ExecuteCommandAsync, cancellationToken);");
        sb.CloseBrace();
    }

    /// <summary>
    ///     Generates the constructor.
    /// </summary>
    private static void GenerateConstructor(
        SourceBuilder sb,
        AggregateInfo aggregate
    )
    {
        sb.AppendSummary(
            $"Initializes a new instance of the <see cref=\"{aggregate.Model.ControllerTypeName}\" /> class.");
        sb.AppendLine("/// <param name=\"aggregateGrainFactory\">Factory for resolving aggregate grains.</param>");
        aggregate.Commands.ToList()
            .ForEach(command =>
            {
                string paramName = NamingConventions.ToCamelCase(command.TypeName) + "Mapper";
                sb.AppendLine($"/// <param name=\"{paramName}\">Mapper for {command.TypeName} DTOs.</param>");
            });
        sb.AppendLine("/// <param name=\"logger\">The logger for diagnostic output.</param>");
        sb.AppendLine($"public {aggregate.Model.ControllerTypeName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IAggregateGrainFactory aggregateGrainFactory,");
        aggregate.Commands.ToList()
            .ForEach(command =>
            {
                string mapperType = $"IMapper<{command.DtoTypeName}, {command.TypeName}>";
                string paramName = NamingConventions.ToCamelCase(command.TypeName) + "Mapper";
                sb.AppendLine($"{mapperType} {paramName},");
            });
        sb.AppendLine($"ILogger<{aggregate.Model.ControllerTypeName}> logger");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.IncreaseIndent();
        sb.AppendLine(": base(logger)");
        sb.DecreaseIndent();
        sb.OpenBrace();
        sb.AppendLine("AggregateGrainFactory = aggregateGrainFactory;");
        aggregate.Commands.ToList()
            .ForEach(command =>
            {
                string paramName = NamingConventions.ToCamelCase(command.TypeName) + "Mapper";
                sb.AppendLine($"{command.TypeName}Mapper = {paramName};");
            });
        sb.CloseBrace();
    }

    /// <summary>
    ///     Generates the aggregate controller.
    /// </summary>
    private static string GenerateController(
        AggregateInfo aggregate
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("System.Threading");
        sb.AppendUsing("System.Threading.Tasks");
        sb.AppendUsing("Microsoft.AspNetCore.Mvc");
        sb.AppendUsing("Microsoft.Extensions.Logging");
        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing("Mississippi.EventSourcing.Aggregates.Abstractions");
        sb.AppendUsing("Mississippi.EventSourcing.Aggregates.Api");
        sb.AppendUsing(aggregate.Model.Namespace);

        // Add using for commands namespace
        string commandsNamespace = aggregate.Model.Namespace + ".Commands";
        sb.AppendUsing(commandsNamespace);
        sb.AppendFileScopedNamespace(aggregate.OutputNamespace);
        sb.AppendLine();

        // Controller class
        sb.AppendSummary($"Controller for {aggregate.Model.AggregateName} aggregate commands.");
        sb.AppendGeneratedCodeAttribute("AggregateControllerGenerator");
        sb.AppendLine($"[Route(\"api/aggregates/{aggregate.Model.RoutePrefix}/{{entityId}}\")]");
        sb.AppendLine(
            $"public sealed class {aggregate.Model.ControllerTypeName} : AggregateControllerBase<{aggregate.Model.TypeName}>");
        sb.OpenBrace();

        // Constructor
        GenerateConstructor(sb, aggregate);
        sb.AppendLine();

        // Properties for dependencies
        sb.AppendLine("private IAggregateGrainFactory AggregateGrainFactory { get; }");
        sb.AppendLine();
        foreach (CommandModel command in aggregate.Commands)
        {
            string mapperType = $"IMapper<{command.DtoTypeName}, {command.TypeName}>";
            sb.AppendLine($"private {mapperType} {command.TypeName}Mapper {{ get; }}");
            sb.AppendLine();
        }

        // Action methods for each command
        foreach (CommandModel command in aggregate.Commands)
        {
            GenerateActionMethod(sb, command);
            sb.AppendLine();
        }

        // ExecuteCommandAsync helper
        GenerateExecuteCommandMethod(sb, aggregate);
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates the ExecuteCommandAsync helper method.
    /// </summary>
    private static void GenerateExecuteCommandMethod(
        SourceBuilder sb,
        AggregateInfo aggregate
    )
    {
        sb.AppendLine("private Task<OperationResult> ExecuteCommandAsync<TCommand>(");
        sb.IncreaseIndent();
        sb.AppendLine("string entityId,");
        sb.AppendLine("TCommand command,");
        sb.AppendLine("CancellationToken cancellationToken");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.IncreaseIndent();
        sb.AppendLine("where TCommand : class");
        sb.DecreaseIndent();
        sb.OpenBrace();
        sb.AppendLine($"IGenericAggregateGrain<{aggregate.Model.TypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"AggregateGrainFactory.GetGenericAggregate<{aggregate.Model.TypeName}>(entityId);");
        sb.DecreaseIndent();
        sb.AppendLine("return grain.ExecuteAsync(command, cancellationToken);");
        sb.CloseBrace();
    }

    /// <summary>
    ///     Gets aggregate information from the compilation, including referenced assemblies.
    /// </summary>
    private static List<AggregateInfo> GetAggregatesFromCompilation(
        Compilation compilation
    )
    {
        List<AggregateInfo> aggregates = [];

        // Get the attribute symbols
        INamedTypeSymbol? aggregateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateAggregateEndpointsAttributeFullName);
        INamedTypeSymbol? commandAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        if (aggregateAttrSymbol is null || commandAttrSymbol is null)
        {
            return aggregates;
        }

        // Scan all assemblies referenced by this compilation
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindAggregatesInNamespace(
                referencedAssembly.GlobalNamespace,
                aggregateAttrSymbol,
                commandAttrSymbol,
                aggregates);
        }

        return aggregates;
    }

    /// <summary>
    ///     Gets all referenced assemblies from the compilation.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> GetReferencedAssemblies(
        Compilation compilation
    )
    {
        // Include the current assembly
        yield return compilation.Assembly;

        // Include all referenced assemblies
        foreach (MetadataReference reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                yield return assemblySymbol;
            }
        }
    }

    /// <summary>
    ///     Tries to get aggregate info from a type symbol.
    /// </summary>
    private static AggregateInfo? TryGetAggregateInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol commandAttrSymbol
    )
    {
        // Check for [GenerateAggregateEndpoints] attribute
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, aggregateAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Get RoutePrefix from named argument, fallback to kebab-case of type name
        string? routePrefix = attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "RoutePrefix")
            .Value.Value?.ToString();

        // Remove "Aggregate" suffix for route and feature key
        string baseName = typeSymbol.Name.EndsWith("Aggregate", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Aggregate".Length)
            : typeSymbol.Name;
        if (string.IsNullOrEmpty(routePrefix))
        {
            routePrefix = NamingConventions.ToKebabCase(baseName);
        }

        // Get FeatureKey from named argument, fallback to camelCase
        string featureKey =
            attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "FeatureKey").Value.Value?.ToString() ??
            NamingConventions.ToCamelCase(baseName);

        // Build aggregate model
        AggregateModel model = new(typeSymbol, routePrefix!, featureKey);

        // Find commands in the Commands sub-namespace
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<CommandModel> commands = containingNs is not null
            ? FindCommandsForAggregate(containingNs, commandAttrSymbol)
            : [];

        // Only generate controller if there are commands
        if (commands.Count == 0)
        {
            return null;
        }

        // Use the Commands namespace to derive output namespace (same as DTOs)
        string commandsNamespace = model.Namespace + ".Commands";
        string outputNamespace = NamingConventions.GetServerCommandDtoNamespace(commandsNamespace);
        return new(model, commands, outputNamespace);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Use the compilation provider to scan referenced assemblies
        IncrementalValueProvider<List<AggregateInfo>> aggregatesProvider = context.CompilationProvider.Select((
            compilation,
            _
        ) => GetAggregatesFromCompilation(compilation));

        // Register source output
        context.RegisterSourceOutput(
            aggregatesProvider,
            static (
                spc,
                aggregates
            ) =>
            {
                foreach (AggregateInfo aggregate in aggregates)
                {
                    string controllerSource = GenerateController(aggregate);
                    spc.AddSource(
                        $"{aggregate.Model.ControllerTypeName}.g.cs",
                        SourceText.From(controllerSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Information about an aggregate type with its commands.
    /// </summary>
    private sealed class AggregateInfo
    {
        public AggregateInfo(
            AggregateModel model,
            List<CommandModel> commands,
            string outputNamespace
        )
        {
            Model = model;
            Commands = commands;
            OutputNamespace = outputNamespace;
        }

        public List<CommandModel> Commands { get; }

        public AggregateModel Model { get; }

        public string OutputNamespace { get; }
    }
}