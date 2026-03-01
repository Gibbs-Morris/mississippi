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


namespace Mississippi.Inlet.Gateway.Generators;

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
        "Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpointsAttribute";

    private const string GenerateAllowAnonymousAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateAllowAnonymousAttribute";

    private const string GenerateAuthorizationAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateAuthorizationAttribute";

    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    /// <summary>
    ///     Recursively finds aggregates in a namespace.
    /// </summary>
    private static void FindAggregatesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? generateAuthorizationAttribute,
        INamedTypeSymbol? generateAllowAnonymousAttribute,
        List<AggregateInfo> aggregates,
        string targetRootNamespace
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            AggregateInfo? info = TryGetAggregateInfo(
                typeSymbol,
                aggregateAttrSymbol,
                commandAttrSymbol,
                generateAuthorizationAttribute,
                generateAllowAnonymousAttribute,
                targetRootNamespace);
            if (info is not null)
            {
                aggregates.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatesInNamespace(
                childNs,
                aggregateAttrSymbol,
                commandAttrSymbol,
                generateAuthorizationAttribute,
                generateAllowAnonymousAttribute,
                aggregates,
                targetRootNamespace);
        }
    }

    /// <summary>
    ///     Finds commands in the Commands sub-namespace of an aggregate.
    /// </summary>
    private static List<CommandInfo> FindCommandsForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? generateAuthorizationAttribute,
        INamedTypeSymbol? generateAllowAnonymousAttribute,
        List<Diagnostic> diagnostics
    )
    {
        List<CommandInfo> commands = [];

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
            CommandModel commandModel = new(typeSymbol, route!, httpMethod);
            GeneratedApiAuthorizationModel authorization = GeneratedApiAuthorizationAnalysis.Analyze(
                typeSymbol,
                generateAuthorizationAttribute,
                generateAllowAnonymousAttribute,
                true);
            diagnostics.AddRange(authorization.Diagnostics);
            commands.Add(new(commandModel, authorization));
        }

        return commands;
    }

    /// <summary>
    ///     Generates an action method for a command.
    /// </summary>
    private static void GenerateActionMethod(
        SourceBuilder sb,
        CommandInfo command
    )
    {
        string methodName = command.Model.TypeName + "Async";
        string httpAttribute = command.Model.HttpMethod.ToUpperInvariant() switch
        {
            "GET" => "HttpGet",
            "PUT" => "HttpPut",
            "DELETE" => "HttpDelete",
            "PATCH" => "HttpPatch",
            var _ => "HttpPost",
        };
        sb.AppendSummary($"Executes the {command.Model.TypeName} command.");
        sb.AppendLine("/// <param name=\"entityId\">The entity identifier.</param>");
        sb.AppendLine("/// <param name=\"request\">The command request.</param>");
        sb.AppendLine("/// <param name=\"cancellationToken\">Cancellation token.</param>");
        sb.AppendLine("/// <returns>The operation result.</returns>");
        GeneratedApiAuthorizationAnalysis.AppendAuthorizationAttributes(sb, command.Authorization);
        sb.AppendLine($"[{httpAttribute}(\"{command.Model.Route}\")]");
        sb.AppendLine($"public Task<ActionResult<OperationResult>> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("[FromRoute] string entityId,");
        sb.AppendLine($"[FromBody] {command.Model.DtoTypeName} request,");
        sb.AppendLine("CancellationToken cancellationToken = default");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(request);");
        sb.AppendLine(
            $"return ExecuteAsync(entityId, {command.Model.TypeName}Mapper.Map(request), ExecuteCommandAsync, cancellationToken);");
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
                string paramName = NamingConventions.ToCamelCase(command.Model.TypeName) + "Mapper";
                sb.AppendLine($"/// <param name=\"{paramName}\">Mapper for {command.Model.TypeName} DTOs.</param>");
            });
        sb.AppendLine("/// <param name=\"logger\">The logger for diagnostic output.</param>");
        sb.AppendLine($"public {aggregate.Model.ControllerTypeName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IAggregateGrainFactory aggregateGrainFactory,");
        aggregate.Commands.ToList()
            .ForEach(command =>
            {
                string mapperType = $"IMapper<{command.Model.DtoTypeName}, {command.Model.TypeName}>";
                string paramName = NamingConventions.ToCamelCase(command.Model.TypeName) + "Mapper";
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
                string paramName = NamingConventions.ToCamelCase(command.Model.TypeName) + "Mapper";
                sb.AppendLine($"{command.Model.TypeName}Mapper = {paramName};");
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
        if (aggregate.Authorization.HasAnyAuthorizationMetadata ||
            aggregate.Commands.Any(command => command.Authorization.HasAnyAuthorizationMetadata))
        {
            sb.AppendUsing("Microsoft.AspNetCore.Authorization");
        }

        sb.AppendUsing("Microsoft.AspNetCore.Mvc");
        sb.AppendUsing("Microsoft.Extensions.Logging");
        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing("Mississippi.DomainModeling.Abstractions");
        sb.AppendUsing("Mississippi.DomainModeling.Gateway");
        sb.AppendUsing(aggregate.Model.Namespace);

        // Add using for commands namespace
        string commandsNamespace = aggregate.Model.Namespace + ".Commands";
        sb.AppendUsing(commandsNamespace);
        sb.AppendFileScopedNamespace(aggregate.OutputNamespace);
        sb.AppendLine();

        // Controller class
        sb.AppendSummary($"Controller for {aggregate.Model.AggregateName} aggregate commands.");
        sb.AppendGeneratedCodeAttribute("AggregateControllerGenerator");
        GeneratedApiAuthorizationAnalysis.AppendAuthorizationAttributes(sb, aggregate.Authorization);
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
        foreach (CommandModel command in aggregate.Commands.Select(command => command.Model))
        {
            string mapperType = $"IMapper<{command.DtoTypeName}, {command.TypeName}>";
            sb.AppendLine($"private {mapperType} {command.TypeName}Mapper {{ get; }}");
            sb.AppendLine();
        }

        // Action methods for each command
        foreach (CommandInfo command in aggregate.Commands)
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
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<AggregateInfo> aggregates = [];

        // Get the attribute symbols
        INamedTypeSymbol? aggregateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateAggregateEndpointsAttributeFullName);
        INamedTypeSymbol? commandAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        INamedTypeSymbol? generateAuthorizationAttribute =
            compilation.GetTypeByMetadataName(GenerateAuthorizationAttributeFullName);
        INamedTypeSymbol? generateAllowAnonymousAttribute =
            compilation.GetTypeByMetadataName(GenerateAllowAnonymousAttributeFullName);
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
                generateAuthorizationAttribute,
                generateAllowAnonymousAttribute,
                aggregates,
                targetRootNamespace);
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
        INamedTypeSymbol commandAttrSymbol,
        INamedTypeSymbol? generateAuthorizationAttribute,
        INamedTypeSymbol? generateAllowAnonymousAttribute,
        string targetRootNamespace
    )
    {
        List<Diagnostic> diagnostics = [];

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
        GeneratedApiAuthorizationModel aggregateAuthorization = GeneratedApiAuthorizationAnalysis.Analyze(
            typeSymbol,
            generateAuthorizationAttribute,
            generateAllowAnonymousAttribute,
            true);
        diagnostics.AddRange(aggregateAuthorization.Diagnostics);

        // Find commands in the Commands sub-namespace
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<CommandInfo> commands = containingNs is not null
            ? FindCommandsForAggregate(
                containingNs,
                commandAttrSymbol,
                generateAuthorizationAttribute,
                generateAllowAnonymousAttribute,
                diagnostics)
            : [];

        // Only generate controller if there are commands
        if (commands.Count == 0)
        {
            return null;
        }

        // Use the Commands namespace to derive output namespace (same as DTOs)
        string commandsNamespace = model.Namespace + ".Commands";
        string outputNamespace = NamingConventions.GetServerCommandDtoNamespace(commandsNamespace, targetRootNamespace);
        return new(model, commands, outputNamespace, aggregateAuthorization, diagnostics.ToImmutableArray());
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Combine compilation with options provider
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        // Use the compilation provider to scan referenced assemblies
        IncrementalValueProvider<List<AggregateInfo>> aggregatesProvider = compilationAndOptions.Select((
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
                    foreach (Diagnostic diagnostic in aggregate.Diagnostics)
                    {
                        spc.ReportDiagnostic(diagnostic);
                    }

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
            List<CommandInfo> commands,
            string outputNamespace,
            GeneratedApiAuthorizationModel authorization,
            ImmutableArray<Diagnostic> diagnostics
        )
        {
            Model = model;
            Commands = commands;
            OutputNamespace = outputNamespace;
            Authorization = authorization;
            Diagnostics = diagnostics;
        }

        public GeneratedApiAuthorizationModel Authorization { get; }

        public List<CommandInfo> Commands { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public AggregateModel Model { get; }

        public string OutputNamespace { get; }
    }

    private sealed class CommandInfo
    {
        public CommandInfo(
            CommandModel model,
            GeneratedApiAuthorizationModel authorization
        )
        {
            Model = model;
            Authorization = authorization;
        }

        public GeneratedApiAuthorizationModel Authorization { get; }

        public CommandModel Model { get; }
    }
}