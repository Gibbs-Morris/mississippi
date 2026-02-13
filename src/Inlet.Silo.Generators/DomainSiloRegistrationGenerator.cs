using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Silo.Generators;

/// <summary>
///     Generates silo-side domain composition registrations that aggregate all generated aggregate, saga,
///     and projection registrations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string AggregateRegistrationsTypeFullName =
        "Mississippi.EventSourcing.Aggregates.AggregateRegistrations";

    private const string GenerateAggregateEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpointsAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeGenericFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute`1";

    private const string ReducerRegistrationsTypeFullName = "Mississippi.EventSourcing.Reducers.ReducerRegistrations";

    private const string SagaRegistrationsTypeFullName = "Mississippi.EventSourcing.Sagas.SagaRegistrations";

    private const string SnapshotRegistrationsTypeFullName =
        "Mississippi.EventSourcing.Snapshots.SnapshotRegistrations";

    private const string UxProjectionRegistrationsTypeFullName =
        "Mississippi.EventSourcing.UxProjections.UxProjectionRegistrations";

    private static bool ContainsAttribute(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? attributeSymbol
    ) =>
        attributeSymbol is not null &&
        typeSymbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));

    private static void GatherFromNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> projectionNamesByDomain,
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
                projectionNamesByDomain,
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
                projectionNamesByDomain,
                sagaNamesByDomain);
        }
    }

    private static void ProcessTypeMember(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> projectionNamesByDomain,
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
        AddProjectionNameIfPresent(typeSymbol, generateProjectionAttribute, domainRoot, projectionNamesByDomain);
        AddSagaNameIfPresent(typeSymbol, generateSagaAttribute, generateSagaGenericAttribute, domainRoot, sagaNamesByDomain);
    }

    private static void AddAggregateNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateAggregateAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain
    )
    {
        if (!ContainsAttribute(typeSymbol, generateAggregateAttribute))
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

    private static void AddProjectionNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateProjectionAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> projectionNamesByDomain
    )
    {
        if (!ContainsAttribute(typeSymbol, generateProjectionAttribute))
        {
            return;
        }

        string projectionName = typeSymbol.Name.EndsWith("Projection", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Projection".Length)
            : typeSymbol.Name;
        if (!projectionNamesByDomain.TryGetValue(domainRoot, out HashSet<string>? projectionNames))
        {
            projectionNames = [];
            projectionNamesByDomain[domainRoot] = projectionNames;
        }

        projectionNames.Add(projectionName);
    }

    private static void AddSagaNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> sagaNamesByDomain
    )
    {
        if (!ContainsAttribute(typeSymbol, generateSagaAttribute) &&
            !ContainsAttribute(typeSymbol, generateSagaGenericAttribute))
        {
            return;
        }

        string sagaName = GetSagaName(typeSymbol.Name);
        if (!sagaNamesByDomain.TryGetValue(domainRoot, out HashSet<string>? sagaNames))
        {
            sagaNames = [];
            sagaNamesByDomain[domainRoot] = sagaNames;
        }

        sagaNames.Add(sagaName);
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
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        string outputNamespace = targetRootNamespace.EndsWith(".Registrations", StringComparison.Ordinal)
            ? targetRootNamespace
            : targetRootNamespace + ".Registrations";
        sb.AppendLine($"namespace {outputNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("///     Extension methods for registering complete silo domain feature sets.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[System.CodeDom.Compiler.GeneratedCode(\"DomainSiloRegistrationGenerator\", \"1.0.0\")]");
        sb.AppendLine("public static class DomainSiloRegistrations");
        sb.AppendLine("{");
        foreach (DomainRegistrationModel model in models.OrderBy(m => m.DomainMethodName, StringComparer.Ordinal))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    ///     Adds all generated silo registrations for the {model.DomainRoot} domain.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
            sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
            sb.AppendLine(
                $"    public static IServiceCollection {model.DomainMethodName}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(services);");
            foreach (string aggregate in model.AggregateNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{aggregate}Aggregate();");
            }

            foreach (string saga in model.SagaNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{saga}Saga();");
            }

            foreach (string projection in model.ProjectionNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{projection}Projection();");
            }

            sb.AppendLine("        return services;");
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
        Dictionary<string, HashSet<string>> projectionNamesByDomain = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> sagaNamesByDomain = new(StringComparer.Ordinal);
        foreach (IAssemblySymbol assembly in GetReferencedAssemblies(compilation))
        {
            GatherFromNamespace(
                assembly.GlobalNamespace,
                generateAggregateAttribute,
                generateProjectionAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                aggregateNamesByDomain,
                projectionNamesByDomain,
                sagaNamesByDomain);
        }

        HashSet<string> domains = new(StringComparer.Ordinal);
        foreach (string domain in aggregateNamesByDomain.Keys)
        {
            domains.Add(domain);
        }

        foreach (string domain in projectionNamesByDomain.Keys)
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
                string[] projectionNames = projectionNamesByDomain.TryGetValue(domain, out HashSet<string>? projections)
                    ? projections.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                string[] sagaNames = sagaNamesByDomain.TryGetValue(domain, out HashSet<string>? sagas)
                    ? sagas.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                return new DomainRegistrationModel(
                    NamingConventions.GetDomainRegistrationMethodName(domain) + "Silo",
                    domain,
                    aggregateNames,
                    projectionNames,
                    sagaNames);
            })
            .Where(model => (model.AggregateNames.Count > 0) ||
                            (model.ProjectionNames.Count > 0) ||
                            (model.SagaNames.Count > 0))
            .OrderBy(model => model.DomainMethodName, StringComparer.Ordinal)
            .ToArray();
    }

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

    private static string GetSagaName(
        string typeName
    )
    {
        if (typeName.EndsWith("SagaState", StringComparison.Ordinal))
        {
            return typeName.Substring(0, typeName.Length - "SagaState".Length);
        }

        if (typeName.EndsWith("State", StringComparison.Ordinal))
        {
            return typeName.Substring(0, typeName.Length - "State".Length);
        }

        return typeName;
    }

    private static bool HasRegistrationDependencies(
        Compilation compilation
    ) =>
        compilation.GetTypeByMetadataName(AggregateRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(ReducerRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(SnapshotRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(UxProjectionRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(SagaRegistrationsTypeFullName) is not null;

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
                spc.AddSource("DomainSiloRegistrations.g.cs", SourceText.From(source, Encoding.UTF8));
            });
    }

    private sealed class DomainRegistrationModel
    {
        public DomainRegistrationModel(
            string domainMethodName,
            string domainRoot,
            IReadOnlyList<string> aggregateNames,
            IReadOnlyList<string> projectionNames,
            IReadOnlyList<string> sagaNames
        )
        {
            DomainMethodName = domainMethodName;
            DomainRoot = domainRoot;
            AggregateNames = aggregateNames;
            ProjectionNames = projectionNames;
            SagaNames = sagaNames;
        }

        public IReadOnlyList<string> AggregateNames { get; }

        public string DomainMethodName { get; }

        public string DomainRoot { get; }

        public IReadOnlyList<string> ProjectionNames { get; }

        public IReadOnlyList<string> SagaNames { get; }
    }
}