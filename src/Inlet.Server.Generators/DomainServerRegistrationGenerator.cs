using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Server.Generators;

/// <summary>
///     Generates server-side domain composition registrations that aggregate all generated mapper registrations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainServerRegistrationGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string ProjectionPathAttributeFullName = "Mississippi.Inlet.Abstractions.ProjectionPathAttribute";

    private static void GatherFromNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? generateCommandAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? projectionPathAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> projectionNamesByDomain
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            ProcessTypeMember(
                typeSymbol,
                generateCommandAttribute,
                generateProjectionAttribute,
                projectionPathAttribute,
                aggregateNamesByDomain,
                projectionNamesByDomain);
        }

        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            GatherFromNamespace(
                child,
                generateCommandAttribute,
                generateProjectionAttribute,
                projectionPathAttribute,
                aggregateNamesByDomain,
                projectionNamesByDomain);
        }
    }

    private static void ProcessTypeMember(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateCommandAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? projectionPathAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> projectionNamesByDomain
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

        AddAggregateNameIfPresent(typeSymbol, generateCommandAttribute, containingNamespace, domainRoot, aggregateNamesByDomain);
        AddProjectionNameIfPresent(
            typeSymbol,
            generateProjectionAttribute,
            projectionPathAttribute,
            domainRoot,
            projectionNamesByDomain);
    }

    private static void AddAggregateNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateCommandAttribute,
        string containingNamespace,
        string domainRoot,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain
    )
    {
        if (!GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateCommandAttribute))
        {
            return;
        }

        string? aggregateName = TargetNamespaceResolver.ExtractAggregateName(containingNamespace);
        if (string.IsNullOrEmpty(aggregateName))
        {
            return;
        }

        if (!aggregateNamesByDomain.TryGetValue(domainRoot, out HashSet<string>? aggregateNames))
        {
            aggregateNames = [];
            aggregateNamesByDomain[domainRoot] = aggregateNames;
        }

        aggregateNames.Add(aggregateName!);
    }

    private static void AddProjectionNameIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateProjectionAttribute,
        INamedTypeSymbol? projectionPathAttribute,
        string domainRoot,
        Dictionary<string, HashSet<string>> projectionNamesByDomain
    )
    {
        if (!GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateProjectionAttribute) ||
            !GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, projectionPathAttribute))
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

    private static string GenerateRegistrationsSource(
        IReadOnlyList<DomainRegistrationModel> models,
        string targetRootNamespace
    )
    {
        StringBuilder sb = new();
        bool includesAggregateMappers = models.Any(model => model.AggregateNames.Count > 0);
        bool includesProjectionMappers = models.Any(model => model.ProjectionNames.Count > 0);
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        if (includesAggregateMappers)
        {
            sb.AppendLine($"using {targetRootNamespace}.Controllers.Aggregates.Mappers;");
        }

        if (includesProjectionMappers)
        {
            sb.AppendLine($"using {targetRootNamespace}.Controllers.Projections.Mappers;");
        }

        if (includesAggregateMappers || includesProjectionMappers)
        {
            sb.AppendLine();
        }

        string outputNamespace = targetRootNamespace + ".Controllers.Mappers";
        sb.AppendLine($"namespace {outputNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("///     Extension methods for registering complete server domain mapping feature sets.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[System.CodeDom.Compiler.GeneratedCode(\"DomainServerRegistrationGenerator\", \"1.0.0\")]");
        sb.AppendLine("public static class DomainServerRegistrations");
        sb.AppendLine("{");
        foreach (DomainRegistrationModel model in models.OrderBy(m => m.DomainMethodName, StringComparer.Ordinal))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    ///     Adds all generated server registrations for the {model.DomainRoot} domain.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
            sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
            sb.AppendLine(
                $"    public static IServiceCollection {model.DomainMethodName}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(services);");
            foreach (string aggregate in model.AggregateNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{aggregate}AggregateMappers();");
            }

            foreach (string projection in model.ProjectionNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{projection}ProjectionMappers();");
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
        INamedTypeSymbol? generateCommandAttribute =
            compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        INamedTypeSymbol? generateProjectionAttribute =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        INamedTypeSymbol? projectionPathAttribute = compilation.GetTypeByMetadataName(ProjectionPathAttributeFullName);
        Dictionary<string, HashSet<string>> aggregateNamesByDomain = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> projectionNamesByDomain = new(StringComparer.Ordinal);
        foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            GatherFromNamespace(
                assembly.GlobalNamespace,
                generateCommandAttribute,
                generateProjectionAttribute,
                projectionPathAttribute,
                aggregateNamesByDomain,
                projectionNamesByDomain);
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

        return domains.Select(domain =>
            {
                string[] aggregateNames = aggregateNamesByDomain.TryGetValue(domain, out HashSet<string>? aggregates)
                    ? aggregates.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                string[] projectionNames = projectionNamesByDomain.TryGetValue(domain, out HashSet<string>? projections)
                    ? projections.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                return new DomainRegistrationModel(
                    NamingConventions.GetDomainRegistrationMethodName(domain) + "Server",
                    domain,
                    aggregateNames,
                    projectionNames);
            })
            .Where(model => (model.AggregateNames.Count > 0) || (model.ProjectionNames.Count > 0))
            .OrderBy(model => model.DomainMethodName, StringComparer.Ordinal)
            .ToArray();
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
                spc.AddSource("DomainServerRegistrations.g.cs", SourceText.From(source, Encoding.UTF8));
            });
    }

    private sealed class DomainRegistrationModel
    {
        public DomainRegistrationModel(
            string domainMethodName,
            string domainRoot,
            IReadOnlyList<string> aggregateNames,
            IReadOnlyList<string> projectionNames
        )
        {
            DomainMethodName = domainMethodName;
            DomainRoot = domainRoot;
            AggregateNames = aggregateNames;
            ProjectionNames = projectionNames;
        }

        public IReadOnlyList<string> AggregateNames { get; }

        public string DomainMethodName { get; }

        public string DomainRoot { get; }

        public IReadOnlyList<string> ProjectionNames { get; }
    }
}