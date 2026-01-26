using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Diagnostics;
using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Server.Generators;

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

    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    /// <summary>
    ///     Recursively finds aggregates in a namespace.
    /// </summary>
    private static void FindAggregatesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
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
                targetRootNamespace);
            if (info is not null)
            {
                aggregates.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatesInNamespace(childNs, aggregateAttrSymbol, commandAttrSymbol, aggregates, targetRootNamespace);
        }
    }

    /// <summary>
    ///     Finds commands in the Commands sub-namespace of an aggregate.
    /// </summary>
    private static List<CommandModel> FindCommandsForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol commandAttrSymbol,
        AuthorizationInfo aggregateDefaultAuth
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

            // Extract authorization properties
            AuthorizationInfo auth = ExtractCommandAuthorization(attr, aggregateDefaultAuth);
            commands.Add(new(typeSymbol, route!, httpMethod, auth));
        }

        return commands;
    }

    /// <summary>
    ///     Extracts authorization configuration from a command attribute,
    ///     falling back to aggregate defaults where not specified.
    /// </summary>
    private static AuthorizationInfo ExtractCommandAuthorization(
        AttributeData commandAttr,
        AuthorizationInfo aggregateDefaultAuth
    )
    {
        bool allowAnonymous = TypeAnalyzer.GetBooleanProperty(commandAttr, "AllowAnonymous", false);

        // If AllowAnonymous is set, that takes precedence
        if (allowAnonymous)
        {
            return new AuthorizationInfo(true, false, null, null);
        }

        // Get command-level authorization (overrides aggregate defaults)
        string? authorizeRoles = TypeAnalyzer.GetStringProperty(commandAttr, "AuthorizeRoles");
        string? authorizePolicy = TypeAnalyzer.GetStringProperty(commandAttr, "AuthorizePolicy");
        bool? requiresAuth = TypeAnalyzer.GetNullableBooleanProperty(commandAttr, "RequiresAuthentication");

        // Fall back to aggregate defaults if not set at command level
        if (string.IsNullOrEmpty(authorizeRoles))
        {
            authorizeRoles = aggregateDefaultAuth.AuthorizeRoles;
        }

        if (string.IsNullOrEmpty(authorizePolicy))
        {
            authorizePolicy = aggregateDefaultAuth.AuthorizePolicy;
        }

        requiresAuth ??= aggregateDefaultAuth.RequiresAuthentication;

        return new AuthorizationInfo(false, requiresAuth == true, authorizeRoles, authorizePolicy);
    }

    /// <summary>
    ///     Extracts default authorization configuration from an aggregate attribute.
    /// </summary>
    private static AuthorizationInfo ExtractAggregateDefaultAuthorization(
        AttributeData aggregateAttr
    )
    {
        string? defaultRoles = TypeAnalyzer.GetStringProperty(aggregateAttr, "DefaultAuthorizeRoles");
        string? defaultPolicy = TypeAnalyzer.GetStringProperty(aggregateAttr, "DefaultAuthorizePolicy");
        bool defaultRequiresAuth = TypeAnalyzer.GetBooleanProperty(aggregateAttr, "DefaultRequiresAuthentication", false);

        return new AuthorizationInfo(false, defaultRequiresAuth, defaultRoles, defaultPolicy);
    }

    /// <summary>
    ///     Emits authorization attribute(s) based on the authorization configuration.
    /// </summary>
    private static void EmitAuthorizationAttribute(
        SourceBuilder sb,
        AuthorizationInfo auth
    )
    {
        if (auth.AllowAnonymous)
        {
            sb.AppendLine("[AllowAnonymous]");
            return;
        }

        if (!string.IsNullOrEmpty(auth.AuthorizePolicy))
        {
            sb.AppendLine($"[Authorize(Policy = \"{auth.AuthorizePolicy}\")]");
            return;
        }

        if (!string.IsNullOrEmpty(auth.AuthorizeRoles))
        {
            sb.AppendLine($"[Authorize(Roles = \"{auth.AuthorizeRoles}\")]");
            return;
        }

        if (auth.RequiresAuthentication)
        {
            sb.AppendLine("[Authorize]");
        }
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

        // Emit authorization attributes
        EmitAuthorizationAttribute(sb, command.Authorization);

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

        // Add authorization using if any command has authorization
        bool hasAnyAuth = aggregate.Commands.Any(c => c.Authorization.HasAuthorization);
        if (hasAnyAuth)
        {
            sb.AppendUsing("Microsoft.AspNetCore.Authorization");
        }

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
        Compilation compilation,
        string targetRootNamespace
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
        string targetRootNamespace
    )
    {
        // Check for [GenerateAggregateEndpoints] attribute
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, aggregateAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Check if controller generation is opted out
        if (!TypeAnalyzer.GetBooleanProperty(attr, "GenerateController"))
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

        // Extract aggregate-level default authorization
        AuthorizationInfo aggregateDefaultAuth = ExtractAggregateDefaultAuthorization(attr);

        // Build aggregate model
        AggregateModel model = new(typeSymbol, routePrefix!, featureKey);

        // Find commands in the Commands sub-namespace
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<CommandModel> commands = containingNs is not null
            ? FindCommandsForAggregate(containingNs, commandAttrSymbol, aggregateDefaultAuth)
            : [];

        // Only generate controller if there are commands
        if (commands.Count == 0)
        {
            return null;
        }

        // Use the Commands namespace to derive output namespace (same as DTOs)
        string commandsNamespace = model.Namespace + ".Commands";
        string outputNamespace = NamingConventions.GetServerCommandDtoNamespace(commandsNamespace, targetRootNamespace);
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
        // Combine compilation with options provider
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        // Use the compilation provider to scan referenced assemblies and extract security settings
        IncrementalValueProvider<(List<AggregateInfo> Aggregates, SecurityEnforcementSettings? SecuritySettings)>
            aggregatesWithSecurityProvider = compilationAndOptions.Select((
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
                List<AggregateInfo> aggregates = GetAggregatesFromCompilation(source.Compilation, targetRootNamespace);
                SecurityEnforcementSettings? securitySettings =
                    AuthorizationValidator.GetSecurityEnforcementSettings(source.Compilation);
                return (aggregates, securitySettings);
            });

        // Register source output
        context.RegisterSourceOutput(
            aggregatesWithSecurityProvider,
            static (
                spc,
                data
            ) =>
            {
                foreach (AggregateInfo aggregate in data.Aggregates)
                {
                    // Validate authorization and report diagnostics
                    ValidateAndReportDiagnostics(spc, aggregate, data.SecuritySettings);

                    // Generate the controller
                    string controllerSource = GenerateController(aggregate);
                    spc.AddSource(
                        $"{aggregate.Model.ControllerTypeName}.g.cs",
                        SourceText.From(controllerSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Validates command authorization and reports diagnostics.
    /// </summary>
    private static void ValidateAndReportDiagnostics(
        SourceProductionContext context,
        AggregateInfo aggregate,
        SecurityEnforcementSettings? securitySettings
    )
    {
        if (securitySettings is null)
        {
            return;
        }

        foreach (CommandModel command in aggregate.Commands)
        {
            // Skip if this type is exempt
            if (securitySettings.IsTypeExempt(command.FullTypeName))
            {
                continue;
            }

            AuthorizationInfo auth = command.Authorization;
            bool hasAuthorization = auth.HasAuthorization;

            if (auth.AllowAnonymous)
            {
                if (securitySettings.TreatAnonymousAsError)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            GeneratorDiagnostics.UnsecuredCommandEndpoint,
                            command.Symbol.Locations.FirstOrDefault(),
                            command.TypeName,
                            aggregate.Model.TypeName));
                }
                else
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            GeneratorDiagnostics.AnonymousEndpointAllowed,
                            command.Symbol.Locations.FirstOrDefault(),
                            command.TypeName));
                }
            }
            else if (!hasAuthorization)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        GeneratorDiagnostics.UnsecuredCommandEndpoint,
                        command.Symbol.Locations.FirstOrDefault(),
                        command.TypeName,
                        aggregate.Model.TypeName));
            }
        }
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