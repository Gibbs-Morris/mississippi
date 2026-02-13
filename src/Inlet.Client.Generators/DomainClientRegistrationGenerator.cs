using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Client.Generators;

/// <summary>
///     Generates client-side domain composition registrations that aggregate all generated feature registrations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainClientRegistrationGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeGenericFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute`1";

    private const string FeaturesSuffix = ".Features";

    private static void GatherFromNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? generateCommandAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> sagaNamesByDomain,
        HashSet<string> domainsWithProjections
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            ProcessTypeMember(
                typeSymbol,
                generateCommandAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                generateProjectionAttribute,
                aggregateNamesByDomain,
                sagaNamesByDomain,
                domainsWithProjections);
        }

        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            GatherFromNamespace(
                child,
                generateCommandAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                generateProjectionAttribute,
                aggregateNamesByDomain,
                sagaNamesByDomain,
                domainsWithProjections);
        }
    }

    private static void ProcessTypeMember(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateCommandAttribute,
        INamedTypeSymbol? generateSagaAttribute,
        INamedTypeSymbol? generateSagaGenericAttribute,
        INamedTypeSymbol? generateProjectionAttribute,
        Dictionary<string, HashSet<string>> aggregateNamesByDomain,
        Dictionary<string, HashSet<string>> sagaNamesByDomain,
        HashSet<string> domainsWithProjections
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

        AddAggregateNameIfPresent(
            typeSymbol,
            generateCommandAttribute,
            containingNamespace,
            domainRoot,
            aggregateNamesByDomain);
        AddSagaNameIfPresent(typeSymbol, generateSagaAttribute, generateSagaGenericAttribute, domainRoot, sagaNamesByDomain);
        AddProjectionDomainIfPresent(typeSymbol, generateProjectionAttribute, domainRoot, domainsWithProjections);
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

    private static void AddProjectionDomainIfPresent(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateProjectionAttribute,
        string domainRoot,
        HashSet<string> domainsWithProjections
    )
    {
        if (GeneratorSymbolAnalysis.ContainsAttribute(typeSymbol, generateProjectionAttribute))
        {
            domainsWithProjections.Add(domainRoot);
        }
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
        string[] usingNamespaces = models
            .SelectMany(m => m.AggregateNames.Select(name => $"{m.FeatureNamespace}.{name}Aggregate"))
            .Concat(models.SelectMany(m => m.SagaNames.Select(name => $"{m.FeatureNamespace}.{name}Saga")))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ns => ns, StringComparer.Ordinal)
            .ToArray();
        foreach (string ns in usingNamespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        if (usingNamespaces.Length > 0)
        {
            sb.AppendLine();
        }

        string outputNamespace = targetRootNamespace.EndsWith(FeaturesSuffix, StringComparison.Ordinal)
            ? targetRootNamespace
            : targetRootNamespace + FeaturesSuffix;
        sb.AppendLine($"namespace {outputNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("///     Extension methods for registering complete client domain feature sets.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[System.CodeDom.Compiler.GeneratedCode(\"DomainClientRegistrationGenerator\", \"1.0.0\")]");
        sb.AppendLine("public static class DomainFeatureRegistrations");
        sb.AppendLine("{");
        foreach (DomainRegistrationModel model in models.OrderBy(m => m.DomainMethodName, StringComparer.Ordinal))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    ///     Adds all generated client features for the {model.DomainRoot} domain.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
            sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
            sb.AppendLine(
                $"    public static IServiceCollection {model.DomainMethodName}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(services);");
            foreach (string aggregate in model.AggregateNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{aggregate}AggregateFeature();");
            }

            foreach (string saga in model.SagaNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine($"        services.Add{saga}SagaFeature();");
            }

            if (model.IncludesProjections)
            {
                sb.AppendLine("        services.AddProjectionsFeature();");
            }

            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static DomainRegistrationModel[] GetDomainRegistrations(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        INamedTypeSymbol? generateCommandAttribute =
            compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        INamedTypeSymbol? generateSagaAttribute =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        INamedTypeSymbol? generateSagaGenericAttribute =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeGenericFullName);
        INamedTypeSymbol? generateProjectionAttribute =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        Dictionary<string, HashSet<string>> aggregateNamesByDomain = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> sagaNamesByDomain = new(StringComparer.Ordinal);
        HashSet<string> domainsWithProjections = new(StringComparer.Ordinal);
        foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            GatherFromNamespace(
                assembly.GlobalNamespace,
                generateCommandAttribute,
                generateSagaAttribute,
                generateSagaGenericAttribute,
                generateProjectionAttribute,
                aggregateNamesByDomain,
                sagaNamesByDomain,
                domainsWithProjections);
        }

        HashSet<string> domains = new(StringComparer.Ordinal);
        foreach (string domain in aggregateNamesByDomain.Keys)
        {
            domains.Add(domain);
        }

        foreach (string domain in sagaNamesByDomain.Keys)
        {
            domains.Add(domain);
        }

        foreach (string domain in domainsWithProjections)
        {
            domains.Add(domain);
        }

        string featureNamespace = targetRootNamespace.EndsWith(FeaturesSuffix, StringComparison.Ordinal)
            ? targetRootNamespace
            : targetRootNamespace + FeaturesSuffix;
        return domains.Select(domain =>
            {
                string[] aggregateNames = aggregateNamesByDomain.TryGetValue(domain, out HashSet<string>? aggregates)
                    ? aggregates.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                string[] sagaNames = sagaNamesByDomain.TryGetValue(domain, out HashSet<string>? sagas)
                    ? sagas.OrderBy(n => n, StringComparer.Ordinal).ToArray()
                    : [];
                return new DomainRegistrationModel(
                    NamingConventions.GetDomainRegistrationMethodName(domain) + "Client",
                    domain,
                    featureNamespace,
                    domainsWithProjections.Contains(domain),
                    aggregateNames,
                    sagaNames);
            })
            .Where(model =>
                (model.AggregateNames.Count > 0) || (model.SagaNames.Count > 0) || model.IncludesProjections)
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
                IReadOnlyList<DomainRegistrationModel> domains = GetDomainRegistrations(
                    source.Compilation,
                    targetRootNamespace);
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
                spc.AddSource("DomainFeatureRegistrations.g.cs", SourceText.From(source, Encoding.UTF8));
            });
    }

    private sealed class DomainRegistrationModel
    {
        public DomainRegistrationModel(
            string domainMethodName,
            string domainRoot,
            string featureNamespace,
            bool includesProjections,
            IReadOnlyList<string> aggregateNames,
            IReadOnlyList<string> sagaNames
        )
        {
            DomainMethodName = domainMethodName;
            DomainRoot = domainRoot;
            FeatureNamespace = featureNamespace;
            IncludesProjections = includesProjections;
            AggregateNames = aggregateNames;
            SagaNames = sagaNames;
        }

        public IReadOnlyList<string> AggregateNames { get; }

        public string DomainMethodName { get; }

        public string DomainRoot { get; }

        public string FeatureNamespace { get; }

        public bool IncludesProjections { get; }

        public IReadOnlyList<string> SagaNames { get; }
    }
}