using System;

using Microsoft.CodeAnalysis;


namespace Mississippi.Sdk.Generators.Core.Analysis;

/// <summary>
///     Represents an aggregate type marked with [GenerateAggregateEndpoints] for code generation.
/// </summary>
public sealed class AggregateModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateModel" /> class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol representing the aggregate.</param>
    /// <param name="routePrefix">The route prefix from the attribute or default.</param>
    /// <param name="featureKey">The feature key from the attribute or default.</param>
    public AggregateModel(
        INamedTypeSymbol typeSymbol,
        string routePrefix,
        string featureKey
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        TypeName = typeSymbol.Name;
        FullTypeName = typeSymbol.ToDisplayString();
        Namespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        RoutePrefix = routePrefix ?? throw new ArgumentNullException(nameof(routePrefix));
        FeatureKey = featureKey ?? throw new ArgumentNullException(nameof(featureKey));

        // Derive aggregate name without "Aggregate" suffix
        AggregateName = TypeName.EndsWith("Aggregate", StringComparison.Ordinal)
            ? TypeName.Substring(0, TypeName.Length - "Aggregate".Length)
            : TypeName;

        // Controller name follows pattern: {AggregateName}Controller
        ControllerTypeName = AggregateName + "Controller";
    }

    /// <summary>
    ///     Gets the aggregate name without the "Aggregate" suffix.
    /// </summary>
    public string AggregateName { get; }

    /// <summary>
    ///     Gets the controller type name.
    /// </summary>
    public string ControllerTypeName { get; }

    /// <summary>
    ///     Gets the feature key for client-side state management.
    /// </summary>
    public string FeatureKey { get; }

    /// <summary>
    ///     Gets the full type name (with namespace).
    /// </summary>
    public string FullTypeName { get; }

    /// <summary>
    ///     Gets the namespace of the aggregate.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     Gets the route prefix for the aggregate controller.
    /// </summary>
    public string RoutePrefix { get; }

    /// <summary>
    ///     Gets the type name (without namespace).
    /// </summary>
    public string TypeName { get; }
}