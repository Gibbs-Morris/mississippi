using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Sdk.Generators.Core.Analysis;

/// <summary>
///     Represents a command type for code generation.
/// </summary>
public sealed class CommandModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandModel" /> class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol representing the command.</param>
    /// <param name="route">The route segment from [GenerateCommand] attribute.</param>
    /// <param name="httpMethod">The HTTP method from [GenerateCommand] attribute.</param>
    /// <param name="dtoSuffix">The suffix for generated DTOs.</param>
    public CommandModel(
        INamedTypeSymbol typeSymbol,
        string route,
        string httpMethod,
        string dtoSuffix = "Dto"
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        TypeName = typeSymbol.Name;
        FullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Namespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        Route = route ?? throw new ArgumentNullException(nameof(route));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        DtoTypeName = TypeAnalyzer.GetDtoTypeName(typeSymbol, dtoSuffix);

        // Determine if this is a positional record (has primary constructor parameters)
        // Positional records have properties that correspond to constructor parameters
        IMethodSymbol? primaryConstructor =
            typeSymbol.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
        IsPositionalRecord = typeSymbol.IsRecord && (primaryConstructor?.Parameters.Length > 0);
        PositionalConstructorParameterCount = IsPositionalRecord ? primaryConstructor!.Parameters.Length : 0;

        // Extract properties - for commands, we want all public instance properties with getters
        Properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic)
            .Where(p => p.GetMethod is not null) // Must have getter
            .Select(p => new PropertyModel(p, dtoSuffix))
            .ToImmutableArray();
    }

    /// <summary>
    ///     Gets the DTO type name.
    /// </summary>
    public string DtoTypeName { get; }

    /// <summary>
    ///     Gets the full type name (with namespace).
    /// </summary>
    public string FullTypeName { get; }

    /// <summary>
    ///     Gets the HTTP method for this command endpoint.
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    ///     Gets a value indicating whether this is a positional record (uses primary constructor).
    /// </summary>
    public bool IsPositionalRecord { get; }

    /// <summary>
    ///     Gets the namespace of the command.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     Gets the number of positional constructor parameters for positional records.
    /// </summary>
    public int PositionalConstructorParameterCount { get; }

    /// <summary>
    ///     Gets the properties of the command.
    /// </summary>
    public ImmutableArray<PropertyModel> Properties { get; }

    /// <summary>
    ///     Gets the route segment from [GenerateCommand] attribute.
    /// </summary>
    public string Route { get; }

    /// <summary>
    ///     Gets the type name (without namespace).
    /// </summary>
    public string TypeName { get; }
}