using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;

namespace Mississippi.Inlet.Client.Generators;

internal static class SagaClientGeneratorHelper
{
    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string SagaStateInterfaceFullName =
        "Mississippi.EventSourcing.Sagas.Abstractions.ISagaState";

    public static List<SagaClientInfo> GetSagasFromCompilation(
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider
    )
    {
        List<SagaClientInfo> sagas = [];
        INamedTypeSymbol? sagaAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        INamedTypeSymbol? sagaStateSymbol = compilation.GetTypeByMetadataName(SagaStateInterfaceFullName);
        if (sagaAttrSymbol is null || sagaStateSymbol is null)
        {
            return sagas;
        }

        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.RootNamespaceProperty,
            out string? rootNamespace);
        optionsProvider.GlobalOptions.TryGetValue(
            TargetNamespaceResolver.AssemblyNameProperty,
            out string? assemblyName);
        string targetRootNamespace =
            TargetNamespaceResolver.GetTargetRootNamespace(rootNamespace, assemblyName, compilation);

        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindSagasInNamespace(
                referencedAssembly.GlobalNamespace,
                sagaAttrSymbol,
                sagaStateSymbol,
                sagas,
                targetRootNamespace);
        }

        return sagas;
    }

    private static void FindSagasInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol sagaAttrSymbol,
        INamedTypeSymbol sagaStateSymbol,
        List<SagaClientInfo> sagas,
        string targetRootNamespace
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            SagaClientInfo? info = TryGetSagaInfo(typeSymbol, sagaAttrSymbol, sagaStateSymbol, targetRootNamespace);
            if (info is not null)
            {
                sagas.Add(info);
            }
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindSagasInNamespace(childNs, sagaAttrSymbol, sagaStateSymbol, sagas, targetRootNamespace);
        }
    }

    private static SagaClientInfo? TryGetSagaInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaAttrSymbol,
        INamedTypeSymbol sagaStateSymbol,
        string targetRootNamespace
    )
    {
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sagaAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        if (!typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sagaStateSymbol)))
        {
            return null;
        }

        INamedTypeSymbol? inputTypeSymbol = attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "InputType")
            .Value
            .Value as INamedTypeSymbol;
        if (inputTypeSymbol is null)
        {
            return null;
        }

        string? routePrefix = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "RoutePrefix").Value.Value
            ?.ToString();
        if (string.IsNullOrWhiteSpace(routePrefix))
        {
            routePrefix = NamingConventions.ToKebabCase(RemoveSagaSuffix(typeSymbol.Name));
        }

        string? featureKey = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "FeatureKey").Value.Value
            ?.ToString();
        if (string.IsNullOrWhiteSpace(featureKey))
        {
            featureKey = NamingConventions.ToCamelCase(RemoveSagaSuffix(typeSymbol.Name));
        }

        return new SagaClientInfo(typeSymbol, inputTypeSymbol, routePrefix!, featureKey!, targetRootNamespace);
    }

    private static string RemoveSagaSuffix(
        string typeName
    ) =>
        typeName.EndsWith("SagaState", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "SagaState".Length)
            : typeName;

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

    private static string GetFeatureRootNamespace(
        string sagaNamespace,
        string sagaName,
        string targetRootNamespace
    )
    {
        if (!string.IsNullOrWhiteSpace(targetRootNamespace))
        {
            return $"{targetRootNamespace}.Features.{sagaName}Saga";
        }

        return $"{sagaNamespace}.Client.Features.{sagaName}Saga";
    }

    internal sealed class SagaClientInfo
    {
        public SagaClientInfo(
            INamedTypeSymbol sagaStateType,
            INamedTypeSymbol inputType,
            string routePrefix,
            string featureKey,
            string targetRootNamespace
        )
        {
            SagaStateType = sagaStateType;
            InputType = inputType;
            RoutePrefix = routePrefix;
            FeatureKey = featureKey;
            SagaStateTypeName = sagaStateType.Name;
            InputTypeName = inputType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            InputTypeNamespace = TypeAnalyzer.GetFullNamespace(inputType);
            SagaNamespace = TypeAnalyzer.GetFullNamespace(sagaStateType);
            SagaName = RemoveSagaSuffix(SagaStateTypeName);
            FeatureRootNamespace = GetFeatureRootNamespace(SagaNamespace, SagaName, targetRootNamespace);
            ActionsNamespace = FeatureRootNamespace + ".Actions";
            ActionEffectsNamespace = FeatureRootNamespace + ".ActionEffects";
            ReducersNamespace = FeatureRootNamespace + ".Reducers";
            StateNamespace = FeatureRootNamespace + ".State";
            DtosNamespace = FeatureRootNamespace + ".Dtos";
            MappersNamespace = FeatureRootNamespace + ".Mappers";
            IMethodSymbol? primaryConstructor =
                inputType.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
            IsInputPositionalRecord = inputType.IsRecord && (primaryConstructor?.Parameters.Length > 0);
            InputProperties = inputType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Where(p => !p.IsStatic)
                .Where(p => p.GetMethod is not null)
                .Select(p => new PropertyModel(p))
                .ToImmutableArray();
        }

        public string ActionEffectsNamespace { get; }

        public string ActionsNamespace { get; }

        public string DtosNamespace { get; }

        public string FeatureKey { get; }

        public string FeatureRootNamespace { get; }

        public ITypeSymbol InputType { get; }

        public ImmutableArray<PropertyModel> InputProperties { get; }

        public string InputTypeName { get; }

        public string InputTypeNamespace { get; }

        public bool IsInputPositionalRecord { get; }

        public string MappersNamespace { get; }

        public string ReducersNamespace { get; }

        public string RoutePrefix { get; }

        public string SagaName { get; }

        public string SagaNamespace { get; }

        public INamedTypeSymbol SagaStateType { get; }

        public string SagaStateTypeName { get; }

        public string StateNamespace { get; }
    }
}
