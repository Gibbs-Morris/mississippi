using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Client.Generators;

/// <summary>
///     Generates a composite client registration that consolidates all Inlet client setup.
/// </summary>
/// <remarks>
///     <para>
///         This generator triggers on the assembly-level <c>[GenerateInletClientComposite]</c> attribute
///         and produces a single registration method that:
///     </para>
///     <list type="bullet">
///         <item>Calls all generated <c>Add{Aggregate}AggregateFeature()</c> methods</item>
///         <item>Registers Reservoir Blazor built-ins</item>
///         <item>Configures Inlet client with SignalR and explicit projection DTO registrations</item>
///     </list>
///     <para>
///         Example: For <c>[assembly: GenerateInletClientComposite(AppName = "Spring")]</c>,
///         generates <c>SpringInletRegistrations.AddSpringInlet()</c> in the client root namespace.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class InletClientCompositeGenerator : IIncrementalGenerator
{
    private const string CompositeAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateInletClientCompositeAttribute";

    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string ProjectionPathAttributeFullName = "Mississippi.Inlet.Abstractions.ProjectionPathAttribute";

    /// <summary>
    ///     Recursively finds commands in a namespace.
    /// </summary>
    private static void FindCommandsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol generateAttrSymbol,
        List<string> commandNamespaces
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            AttributeData? attr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generateAttrSymbol));
            if (attr is not null)
            {
                commandNamespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
            }
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindCommandsInNamespace(childNs, generateAttrSymbol, commandNamespaces);
        }
    }

    /// <summary>
    ///     Recursively finds projections in a namespace.
    /// </summary>
    private static void FindProjectionsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol generateAttrSymbol,
        INamedTypeSymbol projectionPathAttrSymbol,
        List<ProjectionDtoInfo> projections
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            // Check for [GenerateProjectionEndpoints] attribute
            bool hasGenerateAttribute = typeSymbol.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAttrSymbol));
            if (!hasGenerateAttribute)
            {
                continue;
            }

            // Check for [ProjectionPath] attribute and get path
            AttributeData? projectionPathAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, projectionPathAttrSymbol));
            if (projectionPathAttr is null)
            {
                continue;
            }

            // Get the path from constructor argument
            string? projectionPath = projectionPathAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (!string.IsNullOrEmpty(projectionPath))
            {
                projections.Add(
                    new(typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.Name, projectionPath!));
            }
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionsInNamespace(childNs, generateAttrSymbol, projectionPathAttrSymbol, projections);
        }
    }

    /// <summary>
    ///     Generates the composite registration class.
    /// </summary>
    private static void GenerateCompositeRegistration(
        SourceProductionContext context,
        CompositeInfo info
    )
    {
        string registrationClassName = info.AppName + "InletRegistrations";
        string methodName = "Add" + info.AppName + "Inlet";
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendLine();
        sb.AppendUsing("Mississippi.Inlet.Client");
        sb.AppendUsing("Mississippi.Reservoir.Blazor.BuiltIn");

        // Add using directives for each aggregate feature namespace
        foreach (string featureNamespace in info.AggregateFeatureNamespaces.OrderBy(n => n))
        {
            sb.AppendLine($"using {featureNamespace};");
        }

        // Add using directives for each projection DTO namespace
        HashSet<string> dtoNamespaces = new(StringComparer.Ordinal);
        foreach (ProjectionDtoInfo dto in info.ProjectionDtos)
        {
            dtoNamespaces.Add(dto.ClientDtoNamespace);
        }

        foreach (string dtoNamespace in dtoNamespaces.OrderBy(n => n))
        {
            sb.AppendLine($"using {dtoNamespace};");
        }

        sb.AppendFileScopedNamespace(info.TargetNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Composite registration for all Inlet client features in the {info.AppName} application.");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("///     <para>");
        sb.AppendLine(
            "///         This class is generated from the assembly-level <c>[GenerateInletClientComposite]</c> attribute.");
        sb.AppendLine("///         It consolidates all aggregate feature registrations, Reservoir built-ins,");
        sb.AppendLine("///         and Inlet SignalR setup into a single extension method.");
        sb.AppendLine("///     </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendGeneratedCodeAttribute("InletClientCompositeGenerator");
        sb.AppendLine($"internal static class {registrationClassName}");
        sb.OpenBrace();

        // Extension method
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Adds all Inlet client features for the {info.AppName} application.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"public static IServiceCollection {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("this IServiceCollection services");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Aggregate features section
        if (info.AggregateNames.Count > 0)
        {
            sb.AppendLine("// Aggregate features (auto-discovered from domain)");
            foreach (string aggregateName in info.AggregateNames.OrderBy(n => n))
            {
                sb.AppendLine($"services.Add{aggregateName}AggregateFeature();");
            }

            sb.AppendLine();
        }

        // Reservoir built-ins
        sb.AppendLine("// Built-in Reservoir features (navigation, lifecycle)");
        sb.AppendLine("services.AddReservoirBlazorBuiltIns();");
        sb.AppendLine();

        // Inlet client + SignalR
        sb.AppendLine("// Inlet client with SignalR for real-time projection updates");
        sb.AppendLine("services.AddInletClient();");

        // Emit explicit projection DTO registrations instead of runtime scanning
        if (info.ProjectionDtos.Count > 0)
        {
            sb.AppendLine("services.AddInletBlazorSignalR(signalR => signalR");
            sb.IncreaseIndent();
            sb.AppendLine($".WithHubPath(\"{info.HubPath}\")");
            sb.AppendLine(".RegisterProjectionDtos(registry =>");
            sb.AppendLine("{");
            sb.IncreaseIndent();
            foreach (ProjectionDtoInfo dto in info.ProjectionDtos.OrderBy(d => d.Path))
            {
                sb.AppendLine($"registry.Register(\"{dto.Path}\", typeof({dto.ClientDtoTypeName}));");
            }

            sb.DecreaseIndent();
            sb.AppendLine("}));");
            sb.DecreaseIndent();
        }
        else
        {
            // No projections discovered - just configure hub path
            sb.AppendLine("services.AddInletBlazorSignalR(signalR => signalR");
            sb.IncreaseIndent();
            sb.AppendLine($".WithHubPath(\"{info.HubPath}\"));");
            sb.DecreaseIndent();
        }

        sb.AppendLine();
        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        context.AddSource($"{registrationClassName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    /// <summary>
    ///     Gets aggregate names from command namespaces.
    /// </summary>
    private static HashSet<string> GetAggregateNamesFromCommands(
        List<string> commandNamespaces
    )
    {
        HashSet<string> aggregateNames = new(StringComparer.Ordinal);
        foreach (string ns in commandNamespaces)
        {
            string? aggregateName = TargetNamespaceResolver.ExtractAggregateName(ns);
            if (!string.IsNullOrEmpty(aggregateName))
            {
                aggregateNames.Add(aggregateName!);
            }
        }

        return aggregateNames;
    }

    /// <summary>
    ///     Gets command namespaces from the compilation.
    /// </summary>
    private static List<string> GetCommandNamespacesFromCompilation(
        Compilation compilation
    )
    {
        List<string> namespaces = new();
        INamedTypeSymbol? generateAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        if (generateAttrSymbol is null)
        {
            return namespaces;
        }

        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindCommandsInNamespace(referencedAssembly.GlobalNamespace, generateAttrSymbol, namespaces);
        }

        return namespaces;
    }

    /// <summary>
    ///     Computes feature namespaces from aggregate names and target root namespace.
    /// </summary>
    private static HashSet<string> GetFeatureNamespaces(
        HashSet<string> aggregateNames,
        string targetRootNamespace
    )
    {
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        foreach (string aggregateName in aggregateNames)
        {
            // Feature namespace pattern: {TargetRoot}.Features.{Aggregate}Aggregate
            namespaces.Add($"{targetRootNamespace}.Features.{aggregateName}Aggregate");
        }

        return namespaces;
    }

    /// <summary>
    ///     Discovers projection DTOs from the compilation.
    /// </summary>
    private static List<ProjectionDtoInfo> GetProjectionDtosFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<ProjectionDtoInfo> projections = [];
        INamedTypeSymbol? generateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        INamedTypeSymbol? pathAttrSymbol = compilation.GetTypeByMetadataName(ProjectionPathAttributeFullName);
        if (generateAttrSymbol is null || pathAttrSymbol is null)
        {
            return projections;
        }

        List<ProjectionDtoInfo> rawProjections = [];
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                generateAttrSymbol,
                pathAttrSymbol,
                rawProjections);
        }

        // Convert to client DTO info with computed namespaces and type names
        foreach (ProjectionDtoInfo raw in rawProjections)
        {
            string clientNamespace = NamingConventions.GetClientNamespace(raw.SourceNamespace, targetRootNamespace);
            string dtoTypeName = NamingConventions.GetDtoName(raw.SourceTypeName);
            projections.Add(new(raw.SourceNamespace, raw.SourceTypeName, raw.Path, clientNamespace, dtoTypeName));
        }

        return projections;
    }

    /// <summary>
    ///     Gets all referenced assemblies from the compilation.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> GetReferencedAssemblies(
        Compilation compilation
    )
    {
        yield return compilation.Assembly;
        foreach (MetadataReference reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                yield return assemblySymbol;
            }
        }
    }

    /// <summary>
    ///     Tries to get composite info from assembly attributes.
    /// </summary>
    private static CompositeInfo? TryGetCompositeInfo(
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider
    )
    {
        INamedTypeSymbol? compositeAttrSymbol = compilation.GetTypeByMetadataName(CompositeAttributeFullName);
        if (compositeAttrSymbol is null)
        {
            return null;
        }

        // Look for assembly-level attribute
        AttributeData? attr = compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, compositeAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Extract AppName (required)
        string? appName = null;
        string hubPath = "/hubs/inlet";
        foreach (TypedConstant arg in attr.ConstructorArguments)
        {
            if (arg.Value is string s)
            {
                appName = s;
                break;
            }
        }

        foreach (KeyValuePair<string, TypedConstant> kvp in attr.NamedArguments)
        {
            if ((kvp.Key == "AppName") && kvp.Value.Value is string appNameValue)
            {
                appName = appNameValue;
            }
            else if ((kvp.Key == "HubPath") && kvp.Value.Value is string hubPathValue)
            {
                hubPath = hubPathValue;
            }
        }

        if (string.IsNullOrEmpty(appName))
        {
            return null;
        }

        // Get target namespace
        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.RootNamespaceProperty,
            out string? rootNamespace);
        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.AssemblyNameProperty,
            out string? assemblyName);
        string targetNamespace = TargetNamespaceResolver.GetTargetRootNamespace(
            rootNamespace,
            assemblyName,
            compilation);

        // Discover aggregates
        List<string> commandNamespaces = GetCommandNamespacesFromCompilation(compilation);
        HashSet<string> aggregateNames = GetAggregateNamesFromCommands(commandNamespaces);
        HashSet<string> featureNamespaces = GetFeatureNamespaces(aggregateNames, targetNamespace);

        // Discover projections
        List<ProjectionDtoInfo> projectionDtos = GetProjectionDtosFromCompilation(compilation, targetNamespace);
        return new(appName!, hubPath, targetNamespace, aggregateNames, featureNamespaces, projectionDtos);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        IncrementalValueProvider<CompositeInfo?> compositeInfoProvider = compilationAndOptions.Select((
            source,
            _
        ) => TryGetCompositeInfo(source.Compilation, source.Options));
        context.RegisterSourceOutput(
            compositeInfoProvider,
            static (
                spc,
                info
            ) =>
            {
                if (info is not null)
                {
                    GenerateCompositeRegistration(spc, info);
                }
            });
    }

    /// <summary>
    ///     Information about the composite registration to generate.
    /// </summary>
    private sealed class CompositeInfo
    {
        public CompositeInfo(
            string appName,
            string hubPath,
            string targetNamespace,
            HashSet<string> aggregateNames,
            HashSet<string> aggregateFeatureNamespaces,
            List<ProjectionDtoInfo> projectionDtos
        )
        {
            AppName = appName;
            HubPath = hubPath;
            TargetNamespace = targetNamespace;
            AggregateNames = aggregateNames;
            AggregateFeatureNamespaces = aggregateFeatureNamespaces;
            ProjectionDtos = projectionDtos;
        }

        public HashSet<string> AggregateFeatureNamespaces { get; }

        public HashSet<string> AggregateNames { get; }

        public string AppName { get; }

        public string HubPath { get; }

        public List<ProjectionDtoInfo> ProjectionDtos { get; }

        public string TargetNamespace { get; }
    }

    /// <summary>
    ///     Information about a projection DTO to register.
    /// </summary>
    private sealed class ProjectionDtoInfo
    {
        public ProjectionDtoInfo(
            string sourceNamespace,
            string sourceTypeName,
            string path
        )
        {
            SourceNamespace = sourceNamespace;
            SourceTypeName = sourceTypeName;
            Path = path;
            ClientDtoNamespace = string.Empty;
            ClientDtoTypeName = string.Empty;
        }

        public ProjectionDtoInfo(
            string sourceNamespace,
            string sourceTypeName,
            string path,
            string clientDtoNamespace,
            string clientDtoTypeName
        )
        {
            SourceNamespace = sourceNamespace;
            SourceTypeName = sourceTypeName;
            Path = path;
            ClientDtoNamespace = clientDtoNamespace;
            ClientDtoTypeName = clientDtoTypeName;
        }

        public string ClientDtoNamespace { get; }

        public string ClientDtoTypeName { get; }

        public string Path { get; }

        public string SourceNamespace { get; }

        public string SourceTypeName { get; }
    }
}