using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Runtime.Generators;

/// <summary>
///     Generates silo-side domain composition registrations that aggregate all generated aggregate, saga,
///     and projection registrations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string AggregateBuilderExtensionsTypeFullName =
        "Mississippi.DomainModeling.Runtime.Builders.AggregateBuilderExtensions";

    private const string GenerateAggregateEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpointsAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeGenericFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute`1";

    private const string MississippiRuntimeBuilderTypeFullName = "Mississippi.Sdk.Runtime.MississippiRuntimeBuilder";

    private const string ProjectionBuilderExtensionsTypeFullName =
        "Mississippi.DomainModeling.Runtime.Builders.ProjectionBuilderExtensions";

    private const string SagaBuilderExtensionsTypeFullName =
        "Mississippi.DomainModeling.Runtime.Builders.SagaBuilderExtensions";

    private static void AddAggregateNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain
    )
    {
        if (!GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateAggregateAttribute))
        {
            return;
        }

        string aggregateName = typeSymbol.Name.EndsWith("Aggregate", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Aggregate".Length)
            : typeSymbol.Name;
        if (!aggregateNamesByDomain.TryGetValue(domainRoot, out HashSet<string>? aggregateNames))
        {
            aggregateNames = [];
            aggregateNamesByDomain[domainRoot] = aggregateNames;
        }

        aggregateNames.Add(aggregateName);
    }

    private static void AddProjectionInfoIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateProjectionAttribute,
        string domainRoot,
        Dictionary<string, Dictionary<string, string>> projectionsByDomain
    )
    {
        if (!GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateProjectionAttribute))
        {
            return;
        }

        string projectionName = typeSymbol.Name.EndsWith("Projection", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Projection".Length)
            : typeSymbol.Name;
        if (!projectionsByDomain.TryGetValue(domainRoot, out Dictionary<string, string>? projections))
        {
            projections = new(StringComparer.Ordinal);
            projectionsByDomain[domainRoot] = projections;
        }

        projections[projectionName] = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static void AddSagaNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> sagaNamesByDomain
    )
    {
        if (!GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateSagaAttribute) &&
            !GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateSagaGenericAttribute))
        {
            return;
        }

        string sagaName = NamingConventions.GetSagaName(typeSymbol.Name);
        if (!sagaNamesByDomain.TryGetValue(domainRoot, out HashSet<string>? sagaNames))
        {
            sagaNames = [];
            sagaNamesByDomain[domainRoot] = sagaNames;
        }

        sagaNames.Add(sagaName);
    }

    private static void GatherFromNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, Dictionary<string, string>> projectionsByDomain,
        Dictionary<string, HashSet<string>> sagaNamesByDomain
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            ProcessTypeMember(
                typeSymbol,
                generateAggregateAttribute,
                generateProjectionAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                aggregateNamesByDomain,
                projectionsByDomain,
                sagaNamesByDomain);
        }

        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            GatherFromNamespace(
                child,
                generateAggregateAttribute,
                generateProjectionAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                aggregateNamesByDomain,
                projectionsByDomain,
                sagaNamesByDomain);
        }
    }

    private static string GenerateRegistrationsSource(
        IReadOnlyList<DomainRegistrationModel> models,
        string targetRootNamespace
    )
    {
        StringBuilder sb = new();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using Mississippi.DomainModeling.Abstractions.Builders;");
        sb.AppendLine("using Mississippi.Sdk.Runtime;");
        sb.AppendLine();
        string outputNamespace = targetRootNamespace.EndsWith(".Registrations", StringComparison.Ordinal)
            ? targetRootNamespace
            : targetRootNamespace + ".Registrations";
        sb.AppendLine($"namespace {outputNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("///     Extension methods for registering complete runtime domain feature sets.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[System.CodeDom.Compiler.GeneratedCode(\"DomainSiloRegistrationGenerator\", \"1.0.0\")]");
        sb.AppendLine("public static class DomainRuntimeRegistrations");
        sb.AppendLine("{");
        foreach (DomainRegistrationModel model in models.OrderBy(m => m.DomainMethodName, StringComparer.Ordinal))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    ///     Adds all generated runtime registrations for the {model.DomainRoot} domain.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"runtime\">The Mississippi runtime builder.</param>");
            sb.AppendLine($"    public static void {model.DomainMethodName}(this MississippiRuntimeBuilder runtime)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(runtime);");

            // Aggregate sub-builder
            if (model.AggregateNames.Count > 0)
            {
                sb.AppendLine("        runtime.Aggregates(aggregates =>");
                sb.AppendLine("        {");
                foreach (string aggregate in model.AggregateNames.OrderBy(n => n, StringComparer.Ordinal))
                {
                    sb.AppendLine($"            aggregates.Add{aggregate}Aggregate();");
                }

                sb.AppendLine("        });");
            }

            // Projection sub-builder
            if (model.ProjectionNames.Count > 0)
            {
                sb.AppendLine("        runtime.Projections(projections =>");
                sb.AppendLine("        {");
                foreach (string projection in model.ProjectionNames.OrderBy(n => n, StringComparer.Ordinal))
                {
                    sb.AppendLine($"            projections.Add{projection}Projection();");
                }

                sb.AppendLine("        });");
                foreach (ProjectionRegistrationModel projection in model.Projections.OrderBy(
                             p => p.Name,
                             StringComparer.Ordinal))
                {
                    sb.AppendLine($"        runtime.RegisterProjectionMetadata<{projection.TypeName}>();");
                }
            }

            // Saga sub-builder
            if (model.SagaNames.Count > 0)
            {
                sb.AppendLine("        runtime.Sagas(sagas =>");
                sb.AppendLine("        {");
                foreach (string saga in model.SagaNames.OrderBy(n => n, StringComparer.Ordinal))
                {
                    sb.AppendLine($"            sagas.Add{saga}Saga();");
                }

                sb.AppendLine("        });");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static DomainRegistrationModel[] GetDomainRegistrations(
        Compilation compilation
    )
    {
        INamedTypeSymbol? generateAggregateAttribute =
            compilation.GetTypeByMetadataName(GenerateAggregateEndpointsAttributeFullName);
        INamedTypeSymbol? generateProjectionAttribute =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        INamedTypeSymbol? generateSagaAttribute =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        INamedTypeSymbol? generateSagaGenericAttribute =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeGenericFullName);
        Dictionary<string, HashSet<string>> aggregateNamesByDomain = new(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, string>> projectionsByDomain = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> sagaNamesByDomain = new(StringComparer.Ordinal);
        foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            GatherFromNamespace(
                assembly.GlobalNamespace,
                generateAggregateAttribute,
                generateProjectionAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                aggregateNamesByDomain,
                projectionsByDomain,
                sagaNamesByDomain);
        }

        HashSet<string> domains = new(StringComparer.Ordinal);
        foreach (string domain in aggregateNamesByDomain.Keys)
        {
            domains.Add(domain);
        }

        foreach (string domain in projectionsByDomain.Keys)
        {
            domains.Add(domain);
        }

        foreach (string domain in sagaNamesByDomain.Keys)
        {
            domains.Add(domain);
        }

        return domains.Select(domain =>
            {
                string[] aggregateNames = aggregateNamesByDomain.TryGetValue(domain, out HashSet<string>? aggregates)
                    ? aggregates.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                ProjectionRegistrationModel[] projections =
                    projectionsByDomain.TryGetValue(domain, out Dictionary<string, string>? projectionMap)
                        ? projectionMap.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => new ProjectionRegistrationModel(pair.Key, pair.Value))
                            .ToArray()
                        : [];
                string[] projectionNames = projections.Select(projection => projection.Name).ToArray();
                string[] sagaNames = sagaNamesByDomain.TryGetValue(domain, out HashSet<string>? sagas)
                    ? sagas.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                return new DomainRegistrationModel(
                    NamingConventions.GetDomainRegistrationMethodName(domain) + "Runtime",
                    domain,
                    aggregateNames,
                    projections,
                    projectionNames,
                    sagaNames);
            })
            .Where(model => (model.AggregateNames.Count > 0) ||
                            (model.ProjectionNames.Count > 0) ||
                            (model.SagaNames.Count > 0))
            .OrderBy(model => model.DomainMethodName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasRegistrationDependencies(
        Compilation compilation
    ) =>
        compilation.GetTypeByMetadataName(AggregateBuilderExtensionsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(MississippiRuntimeBuilderTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(ProjectionBuilderExtensionsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(SagaBuilderExtensionsTypeFullName) is not null;

    private static void ProcessTypeMember(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, Dictionary<string, string>> projectionsByDomain,
        Dictionary<string, HashSet<string>> sagaNamesByDomain
    )
    {
        string containingNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
        if (string.IsNullOrWhiteSpace(containingNamespace))
        {
            return;
        }

        string domainRoot = NamingConventions.GetDomainRootNamespace(containingNamespace);
        if (string.IsNullOrWhiteSpace(domainRoot))
        {
            return;
        }

        AddAggregateNameIfPresent(typeSymbol, generateAggregateAttribute, domainRoot, aggregateNamesByDomain);
        AddProjectionInfoIfPresent(typeSymbol, generateProjectionAttribute, domainRoot, projectionsByDomain);
        AddSagaNameIfPresent(
            typeSymbol,
            generateSagaAttribute,
            generateSagaGenericAttribute,
            domainRoot,
            sagaNamesByDomain);
    }

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        IncrementalValueProvider<(IReadOnlyList<DomainRegistrationModel> Domains, string TargetRootNamespace)>
            domainsProvider = compilationAndOptions.Select((
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
                if (!HasRegistrationDependencies(source.Compilation))
                {
                    return ((IReadOnlyList<DomainRegistrationModel>)[], targetRootNamespace);
                }

                IReadOnlyList<DomainRegistrationModel> domains = GetDomainRegistrations(source.Compilation);
                return (domains, targetRootNamespace);
            });
        context.RegisterSourceOutput(
            domainsProvider,
            static (
                spc,
                result
            ) =>
            {
                if (result.Domains.Count == 0)
                {
                    return;
                }

                string source = GenerateRegistrationsSource(result.Domains, result.TargetRootNamespace);
                spc.AddSource("DomainRuntimeRegistrations.g.cs", SourceText.From(source, Encoding.UTF8));
            });
    }

    private sealed class DomainRegistrationModel
    {
        public DomainRegistrationModel(
            string domainMethodName,
            string domainRoot,
            IReadOnlyList<string> aggregateNames,
            IReadOnlyList<ProjectionRegistrationModel> projections,
            IReadOnlyList<string> projectionNames,
            IReadOnlyList<string> sagaNames
        )
        {
            DomainMethodName = domainMethodName;
            DomainRoot = domainRoot;
            AggregateNames = aggregateNames;
            Projections = projections;
            ProjectionNames = projectionNames;
            SagaNames = sagaNames;
        }

        public IReadOnlyList<string> AggregateNames { get; }

        public string DomainMethodName { get; }

        public string DomainRoot { get; }

        public IReadOnlyList<string> ProjectionNames { get; }

        public IReadOnlyList<ProjectionRegistrationModel> Projections { get; }

        public IReadOnlyList<string> SagaNames { get; }
    }

    private sealed class ProjectionRegistrationModel
    {
        public ProjectionRegistrationModel(
            string name,
            string typeName
        )
        {
            Name = name;
            TypeName = typeName;
        }

        public string Name { get; }

        public string TypeName { get; }
    }
}