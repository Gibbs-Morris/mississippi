using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Server.Generators;

/// <summary>
///     Generates domain registration wrappers for server projects.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainServerRegistrationGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string BrookNameAttributeFullName =
        "Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute";

    private static readonly char[] NamespaceSeparators = new[] { '.' };

    private static string BuildTypeName(
        string? namespacePrefix
    )
    {
        if (string.IsNullOrWhiteSpace(namespacePrefix))
        {
            return string.Empty;
        }

        string[] parts = namespacePrefix!.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts);
    }

    private static void FindCommandsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol generateAttrSymbol,
        HashSet<string> aggregateNames
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers()
                     .Where(typeSymbol => typeSymbol.GetAttributes()
                         .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAttrSymbol))))
        {
            string? aggregateName = NamingConventions.GetAggregateNameFromNamespace(
                typeSymbol.ContainingNamespace.ToDisplayString());
            if (aggregateName is not null && !string.IsNullOrWhiteSpace(aggregateName))
            {
                aggregateNames.Add(aggregateName);
            }
        }

        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            FindCommandsInNamespace(child, generateAttrSymbol, aggregateNames);
        }
    }

    private static void FindProjectionsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol projectionAttrSymbol,
        INamedTypeSymbol? brookNameAttrSymbol,
        List<(string Name, string? Path, string? BrookName, bool IsExplicitPath)> projectionInfos
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            AttributeData? genAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, projectionAttrSymbol));
            if (genAttr is null)
            {
                continue;
            }

            string projectionName = NamingConventions.RemoveSuffix(typeSymbol.Name, "Projection");
            if (string.IsNullOrWhiteSpace(projectionName))
            {
                continue;
            }

            // Read Path from [GenerateProjectionEndpoints(Path = "...")] named argument
            string? path = genAttr.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "Path").Value.Value?.ToString();
            bool isExplicitPath = !string.IsNullOrEmpty(path);
            if (!isExplicitPath)
            {
                path = NamingConventions.GetRoutePrefix(typeSymbol.Name);
            }

            string? brookName = null;
            if (brookNameAttrSymbol is not null)
            {
                AttributeData? brookAttr = typeSymbol.GetAttributes()
                    .FirstOrDefault(a =>
                        SymbolEqualityComparer.Default.Equals(a.AttributeClass, brookNameAttrSymbol));
                if (brookAttr?.ConstructorArguments.Length >= 3)
                {
                    string? appName = brookAttr.ConstructorArguments[0].Value as string;
                    string? moduleName = brookAttr.ConstructorArguments[1].Value as string;
                    string? name = brookAttr.ConstructorArguments[2].Value as string;
                    if (appName is not null && moduleName is not null && name is not null)
                    {
                        brookName = appName + "." + moduleName + "." + name;
                    }
                }
            }

            projectionInfos.Add((projectionName, path, brookName, isExplicitPath));
        }

        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionsInNamespace(child, projectionAttrSymbol, brookNameAttrSymbol, projectionInfos);
        }
    }

    private static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider
    )
    {
        string targetRootNamespace = ResolveTargetRootNamespace(compilation, optionsProvider);
        string? productNamespace = GetProductNamespace(targetRootNamespace, ".Server");
        if (string.IsNullOrWhiteSpace(productNamespace))
        {
            return;
        }

        string productTypeName = BuildTypeName(productNamespace);
        HashSet<string> aggregateNames = GetAggregateNames(compilation);
        List<(string Name, string? Path, string? BrookName, bool IsExplicitPath)> projectionInfos = GetProjectionInfos(compilation);
        if ((aggregateNames.Count == 0) && (projectionInfos.Count == 0))
        {
            return;
        }

        string registrationsNamespace = targetRootNamespace + ".Registrations";
        string className = productTypeName + "DomainServerRegistrations";
        string methodName = "Add" + productTypeName + "Domain";
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("Mississippi.Common.Abstractions.Builders");
        if (aggregateNames.Count > 0)
        {
            sb.AppendUsing(targetRootNamespace + ".Controllers.Aggregates.Mappers");
        }

        if (projectionInfos.Count > 0)
        {
            sb.AppendUsing(targetRootNamespace + ".Controllers.Projections.Mappers");
        }

        List<(string Path, string BrookName, bool IsExplicit)> brookMappings = projectionInfos
            .Where(p => p.Path is not null && p.BrookName is not null)
            .Select(p => (Path: p.Path!, BrookName: p.BrookName!, IsExplicit: p.IsExplicitPath))
            .OrderBy(m => m.Path, StringComparer.Ordinal)
            .ToList();
        if (brookMappings.Count > 0)
        {
            sb.AppendUsing("Mississippi.Inlet.Silo");
        }

        sb.AppendFileScopedNamespace(registrationsNamespace);
        sb.AppendLine();
        sb.AppendSummary("Extension methods for registering the domain server mappers.");
        sb.AppendGeneratedCodeAttribute("DomainServerRegistrationGenerator");
        sb.AppendLine($"public static class {className}");
        sb.OpenBrace();
        sb.AppendSummary("Adds domain mappers generated from the domain project.");
        sb.AppendLine("/// <param name=\"builder\">The Mississippi server builder.</param>");
        sb.AppendLine("/// <returns>The builder for chaining.</returns>");
        sb.AppendLine($"public static IMississippiServerBuilder {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine("this IMississippiServerBuilder builder");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(builder);");
        sb.AppendLine("builder.ConfigureServices(services =>");
        sb.OpenBrace();
        foreach (string aggregateName in aggregateNames.OrderBy(x => x, StringComparer.Ordinal))
        {
            sb.AppendLine($"services.Add{aggregateName}AggregateMappers();");
        }

        foreach ((string projectionName, _, _, _) in projectionInfos.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            sb.AppendLine($"services.Add{projectionName}ProjectionMappers();");
        }

        sb.CloseBrace();
        sb.AppendLine(");");
        if (brookMappings.Count > 0)
        {
            sb.AppendLine("builder.RegisterProjectionBrookMappings(registry =>");
            sb.OpenBrace();
            foreach ((string path, string brookName, bool isExplicit) in brookMappings)
            {
                sb.AppendLine($"registry.Register(\"{path}\", \"{brookName}\", isExplicit: {(isExplicit ? "true" : "false")});");
            }

            sb.CloseBrace();
            sb.AppendLine(");");
        }

        sb.AppendLine("return builder;");
        sb.CloseBrace();
        sb.CloseBrace();
        context.AddSource(className + ".g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static HashSet<string> GetAggregateNames(
        Compilation compilation
    )
    {
        HashSet<string> aggregateNames = new(StringComparer.Ordinal);
        INamedTypeSymbol? generateAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        if (generateAttrSymbol is null)
        {
            return aggregateNames;
        }

        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindCommandsInNamespace(referencedAssembly.GlobalNamespace, generateAttrSymbol, aggregateNames);
        }

        return aggregateNames;
    }

    private static string? GetProductNamespace(
        string targetRootNamespace,
        string suffix
    )
    {
        if (!targetRootNamespace.EndsWith(suffix, StringComparison.Ordinal))
        {
            return null;
        }

        return targetRootNamespace.Substring(0, targetRootNamespace.Length - suffix.Length);
    }

    private static List<(string Name, string? Path, string? BrookName, bool IsExplicitPath)> GetProjectionInfos(
        Compilation compilation
    )
    {
        List<(string Name, string? Path, string? BrookName, bool IsExplicitPath)> projectionInfos = new();
        INamedTypeSymbol? projectionAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        if (projectionAttrSymbol is null)
        {
            return projectionInfos;
        }

        INamedTypeSymbol? brookNameAttrSymbol =
            compilation.GetTypeByMetadataName(BrookNameAttributeFullName);
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                projectionAttrSymbol,
                brookNameAttrSymbol,
                projectionInfos);
        }

        return projectionInfos;
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

    private static string ResolveTargetRootNamespace(
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider
    )
    {
        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.RootNamespaceProperty,
            out string? rootNamespace);
        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.AssemblyNameProperty,
            out string? assemblyName);
        return TargetNamespaceResolver.GetTargetRootNamespace(rootNamespace, assemblyName, compilation);
    }

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)> source =
            context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        context.RegisterSourceOutput(
            source,
            static (
                spc,
                value
            ) => Generate(spc, value.Compilation, value.Options));
    }
}