using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;

namespace Mississippi.Inlet.Client.Generators;

/// <summary>
///     Provides helper methods for analyzing saga metadata for client generators.
/// </summary>
internal static class SagaClientGeneratorHelper
{
    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string SagaStateInterfaceFullName =
        "Mississippi.EventSourcing.Sagas.Abstractions.ISagaState";

    /// <summary>
    ///     Collects saga metadata from the compilation for client code generation.
    /// </summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <param name="optionsProvider">The analyzer options provider.</param>
    /// <returns>The saga metadata for generation.</returns>
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

    /// <summary>
    ///     Holds resolved saga metadata for client generator outputs.
    /// </summary>
    internal sealed class SagaClientInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SagaClientInfo" /> class.
        /// </summary>
        /// <param name="sagaStateType">The saga state type.</param>
        /// <param name="inputType">The saga input type.</param>
        /// <param name="routePrefix">The saga route prefix.</param>
        /// <param name="featureKey">The feature key for the saga.</param>
        /// <param name="targetRootNamespace">The root namespace for generated output.</param>
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

        /// <summary>
        ///     Gets the action effects namespace.
        /// </summary>
        public string ActionEffectsNamespace { get; }

        /// <summary>
        ///     Gets the actions namespace.
        /// </summary>
        public string ActionsNamespace { get; }

        /// <summary>
        ///     Gets the DTO namespace.
        /// </summary>
        public string DtosNamespace { get; }

        /// <summary>
        ///     Gets the feature key for the saga.
        /// </summary>
        public string FeatureKey { get; }

        /// <summary>
        ///     Gets the feature root namespace.
        /// </summary>
        public string FeatureRootNamespace { get; }

        /// <summary>
        ///     Gets the input type symbol.
        /// </summary>
        public ITypeSymbol InputType { get; }

        /// <summary>
        ///     Gets the input properties.
        /// </summary>
        public ImmutableArray<PropertyModel> InputProperties { get; }

        /// <summary>
        ///     Gets the input type name.
        /// </summary>
        public string InputTypeName { get; }

        /// <summary>
        ///     Gets the input type namespace.
        /// </summary>
        public string InputTypeNamespace { get; }

        /// <summary>
        ///     Gets a value indicating whether the input is a positional record.
        /// </summary>
        public bool IsInputPositionalRecord { get; }

        /// <summary>
        ///     Gets the mappers namespace.
        /// </summary>
        public string MappersNamespace { get; }

        /// <summary>
        ///     Gets the reducers namespace.
        /// </summary>
        public string ReducersNamespace { get; }

        /// <summary>
        ///     Gets the route prefix.
        /// </summary>
        public string RoutePrefix { get; }

        /// <summary>
        ///     Gets the saga name.
        /// </summary>
        public string SagaName { get; }

        /// <summary>
        ///     Gets the saga namespace.
        /// </summary>
        public string SagaNamespace { get; }

        /// <summary>
        ///     Gets the saga state type symbol.
        /// </summary>
        public INamedTypeSymbol SagaStateType { get; }

        /// <summary>
        ///     Gets the saga state type name.
        /// </summary>
        public string SagaStateTypeName { get; }

        /// <summary>
        ///     Gets the state namespace.
        /// </summary>
        public string StateNamespace { get; }

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
    }
}
