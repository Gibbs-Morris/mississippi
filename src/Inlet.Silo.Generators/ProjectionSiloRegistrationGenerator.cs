using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Silo.Generators;

/// <summary>
///     Generates silo-side service registrations for projections marked with [GenerateProjectionEndpoints].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>Add{ProjectionName}() extension method that registers reducers and snapshot converters.</item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find projection types decorated with [GenerateProjectionEndpoints] and their
///         associated reducers in sibling namespaces.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ProjectionSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string EventReducerBaseFullName =
        "Mississippi.EventSourcing.Reducers.Abstractions.EventReducerBase`2";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    /// <summary>
    ///     Recursively finds projections in a namespace.
    /// </summary>
    private static void FindProjectionsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol projectionAttrSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        List<ProjectionRegistrationInfo> projections,
        string targetRootNamespace
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            ProjectionRegistrationInfo? info = TryGetProjectionInfo(
                typeSymbol,
                projectionAttrSymbol,
                reducerBaseSymbol,
                targetRootNamespace);
            if (info is not null)
            {
                projections.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionsInNamespace(childNs, projectionAttrSymbol, reducerBaseSymbol, projections, targetRootNamespace);
        }
    }

    /// <summary>
    ///     Finds reducers for a projection in the Reducers sub-namespace.
    /// </summary>
    private static List<ReducerInfo> FindReducersForProjection(
        INamespaceSymbol projectionNamespace,
        INamedTypeSymbol projectionSymbol,
        INamedTypeSymbol? reducerBaseSymbol
    )
    {
        List<ReducerInfo> reducers = [];
        if (reducerBaseSymbol is null)
        {
            return reducers;
        }

        // Look for Reducers sub-namespace
        INamespaceSymbol? reducersNs = projectionNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Reducers");
        if (reducersNs is null)
        {
            return reducers;
        }

        // Find all types that extend EventReducerBase<TEvent, TProjection>
        foreach (INamedTypeSymbol typeSymbol in reducersNs.GetTypeMembers())
        {
            INamedTypeSymbol? baseType = typeSymbol.BaseType;
            if (baseType is null || !baseType.IsGenericType)
            {
                continue;
            }

            // Check if it extends EventReducerBase<,>
            INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
            if (constructedFrom is null ||
                (constructedFrom.MetadataName != "EventReducerBase`2") ||
                (constructedFrom.ContainingNamespace.ToDisplayString() !=
                 "Mississippi.EventSourcing.Reducers.Abstractions"))
            {
                continue;
            }

            // Verify the second type argument is our projection
            if ((baseType.TypeArguments.Length != 2) ||
                !SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], projectionSymbol))
            {
                continue;
            }

            // Extract event type
            ITypeSymbol eventType = baseType.TypeArguments[0];
            reducers.Add(
                new(
                    typeSymbol.ToDisplayString(),
                    typeSymbol.Name,
                    eventType.ToDisplayString(),
                    eventType.Name,
                    TypeAnalyzer.GetFullNamespace(eventType)));
        }

        return reducers;
    }

    /// <summary>
    ///     Generates the registration extension method for a projection.
    /// </summary>
    private static string GenerateRegistration(
        ProjectionRegistrationInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendUsing("Mississippi.EventSourcing.Reducers");
        sb.AppendUsing("Mississippi.EventSourcing.Snapshots");
        sb.AppendUsing("Mississippi.EventSourcing.UxProjections");

        // Add using for projection namespace
        sb.AppendUsing(projection.Model.Namespace);

        // Add using for reducers namespace
        string reducersNamespace = projection.Model.Namespace + ".Reducers";
        sb.AppendUsing(reducersNamespace);

        // Add usings for event namespaces (they may come from aggregate events)
        foreach (string eventNamespace in projection.Reducers.Select(r => r.EventNamespace).Distinct())
        {
            if (!string.IsNullOrEmpty(eventNamespace) && (eventNamespace != projection.Model.Namespace))
            {
                sb.AppendUsing(eventNamespace);
            }
        }

        sb.AppendFileScopedNamespace(projection.OutputNamespace);
        sb.AppendLine();
        string registrationsName = $"{projection.Model.ProjectionName}ProjectionRegistrations";
        sb.AppendSummary($"Extension methods for registering {projection.Model.ProjectionName} projection services.");
        sb.AppendGeneratedCodeAttribute("ProjectionSiloRegistrationGenerator");
        sb.AppendLine($"public static class {registrationsName}");
        sb.OpenBrace();
        sb.AppendSummary($"Adds the {projection.Model.ProjectionName} projection services to the service collection.");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"public static IServiceCollection Add{projection.Model.ProjectionName}Projection(");
        sb.IncreaseIndent();
        sb.AppendLine("this IServiceCollection services");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Add UX projection infrastructure
        sb.AppendLine("// Add UX projection infrastructure");
        sb.AppendLine("services.AddUxProjections();");
        sb.AppendLine();

        // Register reducers
        sb.AppendLine($"// Register reducers for {projection.Model.TypeName}");
        foreach (ReducerInfo reducer in projection.Reducers)
        {
            sb.AppendLine(
                $"services.AddReducer<{reducer.EventTypeName}, {projection.Model.TypeName}, {reducer.TypeName}>();");
        }

        sb.AppendLine();

        // Add snapshot state converter
        sb.AppendLine("// Add snapshot state converter for projection");
        sb.AppendLine($"services.AddSnapshotStateConverter<{projection.Model.TypeName}>();");
        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Gets projection information from the compilation, including referenced assemblies.
    /// </summary>
    private static List<ProjectionRegistrationInfo> GetProjectionsFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<ProjectionRegistrationInfo> projections = [];

        // Get the attribute symbols
        INamedTypeSymbol? projectionAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        if (projectionAttrSymbol is null)
        {
            return projections;
        }

        // Get base type symbols for reducer detection
        INamedTypeSymbol? reducerBaseSymbol = compilation.GetTypeByMetadataName(EventReducerBaseFullName);

        // Scan all assemblies referenced by this compilation
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                projectionAttrSymbol,
                reducerBaseSymbol,
                projections,
                targetRootNamespace);
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
    ///     Tries to get projection info from a type symbol.
    /// </summary>
    private static ProjectionRegistrationInfo? TryGetProjectionInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol projectionAttrSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        string targetRootNamespace
    )
    {
        // Check for [GenerateProjectionEndpoints] attribute
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, projectionAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Build projection model
        ProjectionModel model = new(typeSymbol);

        // Find reducers
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<ReducerInfo> reducers = containingNs is not null
            ? FindReducersForProjection(containingNs, typeSymbol, reducerBaseSymbol)
            : [];

        // Only generate if there are reducers
        if (reducers.Count == 0)
        {
            return null;
        }

        // Output namespace: {ProductRoot}.Silo.Registrations
        string outputNamespace = NamingConventions.GetSiloRegistrationNamespace(model.Namespace, targetRootNamespace);
        return new(model, reducers, outputNamespace);
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
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)> compilationAndOptions =
            context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        // Use the compilation provider to scan referenced assemblies
        IncrementalValueProvider<List<ProjectionRegistrationInfo>> projectionsProvider =
            compilationAndOptions.Select((
                source,
                _
            ) =>
            {
                source.Options.GlobalOptions.TryGetValue(TargetNamespaceResolver.RootNamespaceProperty, out string? rootNamespace);
                source.Options.GlobalOptions.TryGetValue(TargetNamespaceResolver.AssemblyNameProperty, out string? assemblyName);
                string targetRootNamespace = TargetNamespaceResolver.GetTargetRootNamespace(rootNamespace, assemblyName, source.Compilation);
                return GetProjectionsFromCompilation(source.Compilation, targetRootNamespace);
            });

        // Register source output
        context.RegisterSourceOutput(
            projectionsProvider,
            static (
                spc,
                projections
            ) =>
            {
                foreach (ProjectionRegistrationInfo projection in projections)
                {
                    string registrationSource = GenerateRegistration(projection);
                    spc.AddSource(
                        $"{projection.Model.ProjectionName}ProjectionRegistrations.g.cs",
                        SourceText.From(registrationSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Information about a projection type with its reducers.
    /// </summary>
    private sealed class ProjectionRegistrationInfo
    {
        public ProjectionRegistrationInfo(
            ProjectionModel model,
            List<ReducerInfo> reducers,
            string outputNamespace
        )
        {
            Model = model;
            Reducers = reducers;
            OutputNamespace = outputNamespace;
        }

        public ProjectionModel Model { get; }

        public string OutputNamespace { get; }

        public List<ReducerInfo> Reducers { get; }
    }

    /// <summary>
    ///     Information about an event reducer.
    /// </summary>
    private sealed class ReducerInfo
    {
        public ReducerInfo(
            string fullTypeName,
            string typeName,
            string eventFullTypeName,
            string eventTypeName,
            string eventNamespace
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            EventFullTypeName = eventFullTypeName;
            EventTypeName = eventTypeName;
            EventNamespace = eventNamespace;
        }

        public string EventFullTypeName { get; }

        public string EventNamespace { get; }

        public string EventTypeName { get; }

        public string FullTypeName { get; }

        public string TypeName { get; }
    }
}