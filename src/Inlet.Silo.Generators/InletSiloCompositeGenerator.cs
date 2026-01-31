using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Silo.Generators;

/// <summary>
///     Generates a composite silo registration that consolidates all Inlet silo setup.
/// </summary>
/// <remarks>
///     <para>
///         This generator triggers on the assembly-level <c>[GenerateInletSiloComposite]</c> attribute
///         and produces two extension methods:
///     </para>
///     <list type="bullet">
///         <item>
///             <c>Add{App}Silo(WebApplicationBuilder)</c> - configures observability, Aspire resources,
///             domain, event sourcing, and Orleans silo infrastructure
///         </item>
///         <item>
///             <c>Use{App}Silo(WebApplication)</c> - configures health check endpoints for Aspire
///         </item>
///     </list>
///     <para>
///         Example: For <c>[assembly: GenerateInletSiloComposite(AppName = "Spring")]</c>,
///         generates <c>SpringSiloRegistrations.AddSpringSilo()</c> and
///         <c>SpringSiloRegistrations.UseSpringSilo()</c> in the silo root namespace.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class InletSiloCompositeGenerator : IIncrementalGenerator
{
    private const string CompositeAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateInletSiloCompositeAttribute";

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
        List<ProjectionInfo> projections
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
        string registrationClassName = info.AppName + "SiloRegistrations";
        string addMethodName = "Add" + info.AppName + "Silo";
        string useMethodName = "Use" + info.AppName + "Silo";
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("Microsoft.AspNetCore.Builder");
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendLine();
        sb.AppendUsing("Mississippi.Sdk.Silo");

        // Add using directive for the Infrastructure namespace (where layered registrations live)
        sb.AppendLine($"using {info.TargetNamespace}.Infrastructure;");
        sb.AppendFileScopedNamespace(info.TargetNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Composite registration for all Inlet silo features in the {info.AppName} application.");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("///     <para>");
        sb.AppendLine(
            "///         This class is generated from the assembly-level <c>[GenerateInletSiloComposite]</c> attribute.");
        sb.AppendLine("///         It consolidates observability, Aspire resources, domain, event sourcing,");
        sb.AppendLine("///         and Orleans silo setup into two extension methods.");
        sb.AppendLine("///     </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendGeneratedCodeAttribute("InletSiloCompositeGenerator");
        sb.AppendLine($"internal static class {registrationClassName}");
        sb.OpenBrace();

        // AddXxxSilo extension method
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Adds all silo registrations for the {info.AppName} application.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"builder\">The Mississippi silo builder.</param>");
        sb.AppendLine("/// <returns>The builder for chaining.</returns>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("///     This method configures:");
        sb.AppendLine("///     <list type=\"bullet\">");
        sb.AppendLine("///         <item>Observability (OpenTelemetry tracing, metrics, logging)</item>");
        sb.AppendLine("///         <item>Aspire-managed Azure resources (Table, Blob, Cosmos)</item>");
        sb.AppendLine("///         <item>Domain (aggregates, projections, application services)</item>");
        sb.AppendLine("///         <item>Event sourcing infrastructure (Brooks + Snapshots + Cosmos)</item>");
        sb.AppendLine("///         <item>Orleans silo (Aqueduct + event sourcing)</item>");
        sb.AppendLine("///     </list>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine($"public static MississippiSiloBuilder {addMethodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("this MississippiSiloBuilder builder");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Observability
        sb.AppendLine("// Observability (OpenTelemetry tracing, metrics, logging)");
        sb.AppendLine($"builder.HostBuilder.Add{info.AppName}Observability();");
        sb.AppendLine();

        // Aspire resources
        sb.AppendLine("// Aspire-managed Azure resources (Table, Blob, Cosmos with Mississippi forwarding)");
        sb.AppendLine($"builder.HostBuilder.Add{info.AppName}AspireResources();");
        sb.AppendLine();

        // Domain
        sb.AppendLine("// Domain (aggregates, projections, application services)");
        sb.AppendLine($"builder.Services.Add{info.AppName}Domain();");
        sb.AppendLine();

        // Event sourcing
        sb.AppendLine("// Event sourcing infrastructure (Brooks + Snapshots + Cosmos)");
        sb.AppendLine($"builder.Services.Add{info.AppName}EventSourcing();");
        sb.AppendLine();

        // Orleans silo
        sb.AppendLine("// Orleans silo (Aqueduct + event sourcing)");
        sb.AppendLine($"builder.HostBuilder.Add{info.AppName}OrleansSilo();");
        sb.AppendLine();
        sb.AppendLine("builder.HasDomain = true;");
        sb.AppendLine("return builder;");
        sb.CloseBrace();
        sb.AppendLine();

        // UseXxxSilo extension method
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Applies endpoint mapping for the {info.AppName} silo application.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"app\">The web application.</param>");
        sb.AppendLine("/// <returns>The application for chaining.</returns>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("///     This method configures:");
        sb.AppendLine("///     <list type=\"bullet\">");
        sb.AppendLine("///         <item>Health check endpoint for Aspire orchestration</item>");
        sb.AppendLine("///     </list>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine($"public static WebApplication {useMethodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("this WebApplication app");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Health check
        sb.AppendLine("// Health check for Aspire orchestration");
        sb.AppendLine($"app.Map{info.AppName}HealthCheck();");
        sb.AppendLine();
        sb.AppendLine("return app;");
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
    ///     Discovers projection info from the compilation.
    /// </summary>
    private static List<ProjectionInfo> GetProjectionsFromCompilation(
        Compilation compilation
    )
    {
        List<ProjectionInfo> projections = [];
        INamedTypeSymbol? generateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        INamedTypeSymbol? pathAttrSymbol = compilation.GetTypeByMetadataName(ProjectionPathAttributeFullName);
        if (generateAttrSymbol is null || pathAttrSymbol is null)
        {
            return projections;
        }

        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                generateAttrSymbol,
                pathAttrSymbol,
                projections);
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
        foreach (IAssemblySymbol assemblySymbol in compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
                     .OfType<IAssemblySymbol>())
        {
            yield return assemblySymbol;
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
        string streamProviderName = "mississippi-streaming";
        string? ctorAppName = attr.ConstructorArguments.Select(argument => argument.Value)
            .OfType<string>()
            .FirstOrDefault();
        if (ctorAppName is not null)
        {
            appName = ctorAppName;
        }

        foreach (KeyValuePair<string, TypedConstant> kvp in attr.NamedArguments)
        {
            if ((kvp.Key == "AppName") && kvp.Value.Value is string appNameValue)
            {
                appName = appNameValue;
            }
            else if ((kvp.Key == "StreamProviderName") && kvp.Value.Value is string streamProviderNameValue)
            {
                streamProviderName = streamProviderNameValue;
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

        // Discover projections
        List<ProjectionInfo> projections = GetProjectionsFromCompilation(compilation);
        return new(appName!, streamProviderName, targetNamespace, aggregateNames, projections);
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
            string streamProviderName,
            string targetNamespace,
            HashSet<string> aggregateNames,
            List<ProjectionInfo> projections
        )
        {
            AppName = appName;
            StreamProviderName = streamProviderName;
            TargetNamespace = targetNamespace;
            AggregateNames = aggregateNames;
            Projections = projections;
        }

        public HashSet<string> AggregateNames { get; }

        public string AppName { get; }

        public List<ProjectionInfo> Projections { get; }

        public string StreamProviderName { get; }

        public string TargetNamespace { get; }
    }

    /// <summary>
    ///     Information about a projection to register.
    /// </summary>
    private sealed class ProjectionInfo
    {
        public ProjectionInfo(
            string sourceNamespace,
            string sourceTypeName,
            string path
        )
        {
            SourceNamespace = sourceNamespace;
            SourceTypeName = sourceTypeName;
            Path = path;
        }

        public string Path { get; }

        public string SourceNamespace { get; }

        public string SourceTypeName { get; }
    }
}