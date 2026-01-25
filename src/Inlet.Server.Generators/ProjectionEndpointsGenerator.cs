using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Emit;


namespace Mississippi.Inlet.Server.Generators;

/// <summary>
///     Generates server-side code for projections marked with [GenerateProjectionEndpoints].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>DTO record with mapped properties.</item>
///         <item>Mapper implementing IMapper&lt;TProjection, TDto&gt;.</item>
///         <item>Mapper registration extension method.</item>
///         <item>Controller inheriting from UxProjectionControllerBase.</item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find projection types decorated with [GenerateProjectionEndpoints].
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ProjectionEndpointsGenerator : IIncrementalGenerator
{
    private const string GenerateProjectionEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateProjectionEndpointsAttribute";

    private const string GeneratorName = "ProjectionEndpointsGenerator";

    private const string ProjectionPathAttributeFullName =
        "Mississippi.Inlet.Projection.Abstractions.ProjectionPathAttribute";

    private const string ProjectionSuffix = "Projection";

    /// <summary>
    ///     Derives the output namespace from the projection namespace.
    /// </summary>
    private static string DeriveOutputNamespace(
        string projectionNamespace
    )
    {
        // Convert e.g. "Contoso.Domain.Projections.BankAccountBalance"
        // to "Contoso.Server.Controllers.Projections"
        // This is a simplified derivation - in practice the consuming project defines this
        string[] parts = projectionNamespace.Split('.');
        if (parts.Length >= 2)
        {
            // Replace "Domain" with "Server.Controllers" if present
            int domainIndex = Array.FindIndex(parts, p => p == "Domain");
            if (domainIndex >= 0)
            {
                string[] serverPath = { "Server", "Controllers", "Projections" };
                return string.Join(".", parts.Take(domainIndex).Concat(serverPath));
            }
        }

        // Fallback: append Controllers.Projections
        return projectionNamespace + ".Controllers.Projections";
    }

    /// <summary>
    ///     Recursively finds projections in a namespace.
    /// </summary>
    private static void FindProjectionsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol generateAttrSymbol,
        INamedTypeSymbol projectionPathAttrSymbol,
        List<ProjectionInfo> projections
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            ProjectionInfo? info = TryGetProjectionInfo(typeSymbol, generateAttrSymbol, projectionPathAttrSymbol);
            if (info is not null)
            {
                projections.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionsInNamespace(childNs, generateAttrSymbol, projectionPathAttrSymbol, projections);
        }
    }

    /// <summary>
    ///     Generates all code for a projection.
    /// </summary>
    private static void GenerateCode(
        SourceProductionContext context,
        ProjectionInfo projection,
        HashSet<string> generatedNestedTypes
    )
    {
        // Generate DTO
        string dtoSource = GenerateDto(projection);
        context.AddSource($"{projection.Model.DtoTypeName}.g.cs", SourceText.From(dtoSource, Encoding.UTF8));

        // Generate DTOs for nested custom types (e.g., collection element types)
        foreach (PropertyModel prop in projection.Model.Properties)
        {
            if (prop.ElementTypeSymbol is INamedTypeSymbol elementType &&
                prop.ElementDtoTypeName is not null &&
                !generatedNestedTypes.Contains(prop.ElementDtoTypeName))
            {
                generatedNestedTypes.Add(prop.ElementDtoTypeName);
                string nestedDtoSource = GenerateNestedTypeDto(
                    elementType,
                    prop.ElementDtoTypeName,
                    projection.OutputNamespace);
                context.AddSource($"{prop.ElementDtoTypeName}.g.cs", SourceText.From(nestedDtoSource, Encoding.UTF8));

                // Skip mappers for enums - they can be cast directly
                if (elementType.TypeKind != TypeKind.Enum)
                {
                    // Generate mapper for nested type
                    string nestedMapperSource = GenerateNestedTypeMapper(
                        elementType,
                        prop.ElementDtoTypeName,
                        prop.ElementSourceTypeName!,
                        projection.OutputNamespace);
                    context.AddSource(
                        $"{prop.ElementDtoTypeName}Mapper.g.cs",
                        SourceText.From(nestedMapperSource, Encoding.UTF8));

                    // Generate enum DTOs for enum properties within the nested type
                    GenerateEnumDtosForNestedType(
                        context,
                        elementType,
                        projection.OutputNamespace,
                        generatedNestedTypes);
                }
            }
        }

        // Generate Mapper
        string mapperSource = GenerateMapper(projection);
        context.AddSource($"{GetMapperTypeName(projection)}.g.cs", SourceText.From(mapperSource, Encoding.UTF8));

        // Generate Mapper Registrations
        string registrationsSource = GenerateMapperRegistrations(projection);
        context.AddSource(
            $"{GetRegistrationsTypeName(projection)}.g.cs",
            SourceText.From(registrationsSource, Encoding.UTF8));

        // Generate Controller
        string controllerSource = GenerateController(projection);
        context.AddSource(
            $"{GetControllerTypeName(projection)}.g.cs",
            SourceText.From(controllerSource, Encoding.UTF8));
    }

    /// <summary>
    ///     Generates the controller class.
    /// </summary>
    private static string GenerateController(
        ProjectionInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.AspNetCore.Mvc");
        sb.AppendUsing("Microsoft.Extensions.Logging");
        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing("Mississippi.EventSourcing.UxProjections.Abstractions");
        sb.AppendUsing("Mississippi.EventSourcing.UxProjections.Api");
        sb.AppendUsing(projection.Model.Namespace);
        sb.AppendFileScopedNamespace(projection.OutputNamespace);
        sb.AppendLine();
        string controllerName = GetControllerTypeName(projection);
        string baseName = projection.Model.TypeName.Replace(ProjectionSuffix, string.Empty);
        sb.AppendSummary($"Controller for the {baseName} projection.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"[Route(\"api/projections/{projection.Model.ProjectionPath}/{{entityId}}\")]");
        sb.AppendLine($"public sealed partial class {controllerName}");
        sb.IncreaseIndent();
        sb.AppendLine($": UxProjectionControllerBase<{projection.Model.TypeName}, {projection.Model.DtoTypeName}>");
        sb.DecreaseIndent();
        sb.OpenBrace();

        // Constructor
        sb.AppendSummary($"Initializes a new instance of the {controllerName} class.");
        sb.AppendLine(
            "/// <param name=\"uxProjectionGrainFactory\">Factory for resolving UX projection grains.</param>");
        sb.AppendLine("/// <param name=\"mapper\">Mapper for projection to DTO conversion.</param>");
        sb.AppendLine("/// <param name=\"logger\">The logger for diagnostic output.</param>");
        sb.AppendLine($"public {controllerName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IUxProjectionGrainFactory uxProjectionGrainFactory,");
        sb.AppendLine($"IMapper<{projection.Model.TypeName}, {projection.Model.DtoTypeName}> mapper,");
        sb.AppendLine($"ILogger<{controllerName}> logger");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.IncreaseIndent();
        sb.AppendLine(": base(uxProjectionGrainFactory, mapper, logger)");
        sb.DecreaseIndent();
        sb.OpenBrace();
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates the DTO record.
    /// </summary>
    private static string GenerateDto(
        ProjectionInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("System.Collections.Immutable");
        sb.AppendUsing("System.Text.Json.Serialization");
        sb.AppendFileScopedNamespace(projection.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Response DTO for the {projection.Model.TypeName}.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"public sealed record {projection.Model.DtoTypeName}");
        sb.OpenBrace();
        foreach (PropertyModel prop in projection.Model.Properties)
        {
            sb.AppendSummary($"Gets the {prop.Name} value.");
            sb.AppendLine("[JsonRequired]");
            sb.AppendLine($"public required {prop.DtoTypeName} {prop.Name} {{ get; init; }}");
            sb.AppendLine();
        }

        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates enum DTOs for enum properties within a nested type.
    /// </summary>
    private static void GenerateEnumDtosForNestedType(
        SourceProductionContext context,
        INamedTypeSymbol sourceType,
        string outputNamespace,
        HashSet<string> generatedNestedTypes
    )
    {
        IEnumerable<ITypeSymbol> propTypes = sourceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => (p.DeclaredAccessibility == Accessibility.Public) && !p.IsStatic && p.GetMethod is not null)
            .Select(p => p.Type)
            .Select(propType => propType is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            } nullableType
                ? nullableType.TypeArguments[0]
                : propType);
        foreach (ITypeSymbol unwrappedType in propTypes)
        {
            if (unwrappedType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
            {
                string enumDtoName = enumType.Name + "Dto";
                if (generatedNestedTypes.Add(enumDtoName))
                {
                    string enumDtoSource = GenerateNestedEnumDto(enumType, enumDtoName, outputNamespace);
                    context.AddSource($"{enumDtoName}.g.cs", SourceText.From(enumDtoSource, Encoding.UTF8));
                }
            }
        }
    }

    /// <summary>
    ///     Generates the mapper class.
    /// </summary>
    private static string GenerateMapper(
        ProjectionInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        if (projection.Model.HasEnumerableMappedProperties)
        {
            sb.AppendUsing("System.Linq");
        }

        if (projection.Model.HasImmutableArrayMappedProperties)
        {
            sb.AppendUsing("System.Collections.Immutable");
        }

        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing(projection.Model.Namespace);
        sb.AppendFileScopedNamespace(projection.OutputNamespace + ".Mappers");
        sb.AppendLine();
        string mapperName = GetMapperTypeName(projection);
        sb.AppendSummary($"Maps {projection.Model.TypeName} to {projection.Model.DtoTypeName}.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine(
            $"internal sealed class {mapperName} : IMapper<{projection.Model.TypeName}, {projection.Model.DtoTypeName}>");
        sb.OpenBrace();

        // Constructor with injected mappers for nested types
        if (projection.Model.HasMappedProperties)
        {
            GenerateMapperConstructor(sb, projection);
            sb.AppendLine();
        }

        // Map method
        sb.AppendLine("/// <inheritdoc />");
        sb.AppendLine($"public {projection.Model.DtoTypeName} Map(");
        sb.IncreaseIndent();
        sb.AppendLine($"{projection.Model.TypeName} source");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(source);");
        sb.AppendLine("return new()");
        sb.OpenBrace();
        for (int i = 0; i < projection.Model.Properties.Length; i++)
        {
            PropertyModel prop = projection.Model.Properties[i];
            string comma = i < (projection.Model.Properties.Length - 1) ? "," : string.Empty;
            if (prop.RequiresEnumerableMapper)
            {
                // Collection with custom element type - use appropriate collection conversion
                string toCollection = prop.IsImmutableArray ? ".ToImmutableArray()" : ".ToList()";
                sb.AppendLine($"{prop.Name} = {prop.Name}Mapper.Map(source.{prop.Name}){toCollection}{comma}");
            }
            else if (prop.RequiresMapper)
            {
                // Single custom type
                sb.AppendLine($"{prop.Name} = {prop.Name}Mapper.Map(source.{prop.Name}){comma}");
            }
            else
            {
                // Direct assignment
                sb.AppendLine($"{prop.Name} = source.{prop.Name}{comma}");
            }
        }

        sb.CloseBrace();
        sb.AppendLine(";");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates constructor for mapper with injected dependencies.
    /// </summary>
    private static void GenerateMapperConstructor(
        SourceBuilder sb,
        ProjectionInfo projection
    )
    {
        sb.AppendSummary($"Initializes a new instance of the {GetMapperTypeName(projection)} class.");
        ImmutableArray<PropertyModel> mappedProps = projection.Model.Properties
            .Where(p => p.RequiresMapper)
            .ToImmutableArray();

        // Properties for injected mappers
        foreach (PropertyModel prop in mappedProps)
        {
            if (prop.RequiresEnumerableMapper)
            {
                sb.AppendLine(
                    $"private IEnumerableMapper<{prop.ElementSourceTypeName}, {prop.ElementDtoTypeName}> {prop.Name}Mapper {{ get; }}");
            }
            else
            {
                sb.AppendLine(
                    $"private IMapper<{prop.SourceTypeName}, {prop.DtoTypeName}> {prop.Name}Mapper {{ get; }}");
            }
        }

        sb.AppendLine();
        sb.Append($"public {GetMapperTypeName(projection)}(");
        sb.AppendLine();
        sb.IncreaseIndent();
        for (int i = 0; i < mappedProps.Length; i++)
        {
            PropertyModel prop = mappedProps[i];
            string comma = i < (mappedProps.Length - 1) ? "," : string.Empty;
            string paramName = ToCamelCase(prop.Name) + "Mapper";
            if (prop.RequiresEnumerableMapper)
            {
                sb.AppendLine(
                    $"IEnumerableMapper<{prop.ElementSourceTypeName}, {prop.ElementDtoTypeName}> {paramName}{comma}");
            }
            else
            {
                sb.AppendLine($"IMapper<{prop.SourceTypeName}, {prop.DtoTypeName}> {paramName}{comma}");
            }
        }

        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        for (int j = 0; j < mappedProps.Length; j++)
        {
            PropertyModel prop = mappedProps[j];
            string paramName = ToCamelCase(prop.Name) + "Mapper";
            sb.AppendLine($"{prop.Name}Mapper = {paramName};");
        }

        sb.CloseBrace();
    }

    /// <summary>
    ///     Generates the mapper registration extension.
    /// </summary>
    private static string GenerateMapperRegistrations(
        ProjectionInfo projection
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing(projection.Model.Namespace);
        sb.AppendFileScopedNamespace(projection.OutputNamespace + ".Mappers");
        sb.AppendLine();
        string registrationsName = GetRegistrationsTypeName(projection);
        string baseName = projection.Model.TypeName.Replace(ProjectionSuffix, string.Empty);
        sb.AppendSummary($"Service registration for {baseName} projection mappers.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"internal static class {registrationsName}");
        sb.OpenBrace();
        sb.AppendSummary($"Adds {baseName} projection mappers to the service collection.");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"public static IServiceCollection Add{baseName}ProjectionMappers(");
        sb.IncreaseIndent();
        sb.AppendLine("this IServiceCollection services");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Register nested type mappers first (required for enumerable mappers to resolve)
        HashSet<string> registeredNestedMappers = new();
        foreach (PropertyModel prop in projection.Model.Properties)
        {
            if (prop.RequiresMapper &&
                prop.ElementTypeSymbol is INamedTypeSymbol elementType &&
                (elementType.TypeKind != TypeKind.Enum) &&
                prop.ElementSourceTypeName is not null &&
                prop.ElementDtoTypeName is not null &&
                !registeredNestedMappers.Contains(prop.ElementSourceTypeName))
            {
                registeredNestedMappers.Add(prop.ElementSourceTypeName);
                string nestedMapperTypeName = $"{prop.ElementDtoTypeName}Mapper";
                sb.AppendLine(
                    $"services.AddMapper<{prop.ElementSourceTypeName}, {prop.ElementDtoTypeName}, {nestedMapperTypeName}>();");
            }
        }

        // Register IEnumerableMapper if any properties need it
        if (projection.Model.HasEnumerableMappedProperties)
        {
            sb.AppendLine("services.AddIEnumerableMapper();");
        }

        // Register the main projection mapper
        sb.AppendLine(
            $"services.AddMapper<{projection.Model.TypeName}, {projection.Model.DtoTypeName}, {GetMapperTypeName(projection)}>();");
        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates a DTO for a nested enum type.
    /// </summary>
    private static string GenerateNestedEnumDto(
        INamedTypeSymbol sourceType,
        string dtoName,
        string outputNamespace
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendFileScopedNamespace(outputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Response DTO for {sourceType.Name}.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"public enum {dtoName}");
        sb.OpenBrace();
        IEnumerable<IFieldSymbol> enumMembers = sourceType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue);
        foreach (IFieldSymbol member in enumMembers)
        {
            sb.AppendSummary($"{member.Name} value.");
            sb.AppendLine($"{member.Name} = {member.ConstantValue},");
            sb.AppendLine();
        }

        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates a DTO for a nested custom type.
    /// </summary>
    private static string GenerateNestedTypeDto(
        INamedTypeSymbol sourceType,
        string dtoName,
        string outputNamespace
    )
    {
        // If the source is an enum, generate an enum DTO
        if (sourceType.TypeKind == TypeKind.Enum)
        {
            return GenerateNestedEnumDto(sourceType, dtoName, outputNamespace);
        }

        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("System.Collections.Immutable");
        sb.AppendUsing("System.Text.Json.Serialization");
        sb.AppendFileScopedNamespace(outputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Response DTO for {sourceType.Name}.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"public sealed record {dtoName}");
        sb.OpenBrace();
        IPropertySymbol[] properties = sourceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => (p.DeclaredAccessibility == Accessibility.Public) && !p.IsStatic && p.GetMethod is not null)
            .ToArray();
        foreach (IPropertySymbol prop in properties)
        {
            string propDtoTypeName = TypeAnalyzer.GetDtoTypeName(prop.Type);
            sb.AppendSummary($"Gets the {prop.Name} value.");
            sb.AppendLine("[JsonRequired]");
            sb.AppendLine($"public required {propDtoTypeName} {prop.Name} {{ get; init; }}");
            sb.AppendLine();
        }

        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Generates a mapper for a nested custom type.
    /// </summary>
    private static string GenerateNestedTypeMapper(
        INamedTypeSymbol sourceType,
        string dtoName,
        string sourceTypeName,
        string outputNamespace
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("Mississippi.Common.Abstractions.Mapping");
        sb.AppendUsing(sourceType.ContainingNamespace.ToDisplayString());
        sb.AppendUsing(outputNamespace); // For DTO types like EnumDto
        sb.AppendFileScopedNamespace(outputNamespace + ".Mappers");
        sb.AppendLine();
        string mapperName = dtoName + "Mapper";
        sb.AppendSummary($"Maps {sourceTypeName} to {dtoName}.");
        sb.AppendGeneratedCodeAttribute(GeneratorName);
        sb.AppendLine($"internal sealed class {mapperName} : IMapper<{sourceTypeName}, {dtoName}>");
        sb.OpenBrace();

        // Map method
        sb.AppendLine("/// <inheritdoc />");
        sb.AppendLine($"public {dtoName} Map(");
        sb.IncreaseIndent();
        sb.AppendLine($"{sourceTypeName} source");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(source);");
        sb.AppendLine("return new()");
        sb.OpenBrace();
        IPropertySymbol[] properties = sourceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => (p.DeclaredAccessibility == Accessibility.Public) && !p.IsStatic && p.GetMethod is not null)
            .ToArray();
        for (int i = 0; i < properties.Length; i++)
        {
            IPropertySymbol prop = properties[i];
            string comma = i < (properties.Length - 1) ? "," : string.Empty;

            // Check if this property is a custom enum that needs casting
            if (TypeAnalyzer.IsEnumType(prop.Type) && !TypeAnalyzer.IsFrameworkType(prop.Type))
            {
                string enumTypeName = prop.Type.Name;
                string enumDtoTypeName = enumTypeName + "Dto";
                sb.AppendLine($"{prop.Name} = ({enumDtoTypeName})source.{prop.Name}{comma}");
            }
            else
            {
                sb.AppendLine($"{prop.Name} = source.{prop.Name}{comma}");
            }
        }

        sb.CloseBrace();
        sb.AppendLine(";");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Gets the controller type name.
    /// </summary>
    private static string GetControllerTypeName(
        ProjectionInfo projection
    )
    {
        string baseName = projection.Model.TypeName.Replace(ProjectionSuffix, string.Empty);
        return baseName + "Controller";
    }

    /// <summary>
    ///     Gets the mapper type name.
    /// </summary>
    private static string GetMapperTypeName(
        ProjectionInfo projection
    ) =>
        projection.Model.TypeName + "Mapper";

    /// <summary>
    ///     Gets projection information from the compilation, including referenced assemblies.
    /// </summary>
    private static List<ProjectionInfo> GetProjectionsFromCompilation(
        Compilation compilation
    )
    {
        List<ProjectionInfo> projections = new();

        // Get the attribute symbols
        INamedTypeSymbol? generateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateProjectionEndpointsAttributeFullName);
        INamedTypeSymbol? projectionPathAttrSymbol = compilation.GetTypeByMetadataName(ProjectionPathAttributeFullName);
        if (generateAttrSymbol is null || projectionPathAttrSymbol is null)
        {
            return projections;
        }

        // Scan all assemblies referenced by this compilation
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindProjectionsInNamespace(
                referencedAssembly.GlobalNamespace,
                generateAttrSymbol,
                projectionPathAttrSymbol,
                projections);
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
    ///     Gets the registrations type name.
    /// </summary>
    private static string GetRegistrationsTypeName(
        ProjectionInfo projection
    )
    {
        string baseName = projection.Model.TypeName.Replace(ProjectionSuffix, string.Empty);
        return baseName + "ProjectionMapperRegistrations";
    }

    /// <summary>
    ///     Converts PascalCase to camelCase.
    /// </summary>
    private static string ToCamelCase(
        string value
    )
    {
        if (string.IsNullOrEmpty(value) || (value.Length < 2))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    ///     Tries to get projection info from a type symbol.
    /// </summary>
    private static ProjectionInfo? TryGetProjectionInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol generateAttrSymbol,
        INamedTypeSymbol projectionPathAttrSymbol
    )
    {
        // Check for [GenerateProjectionEndpoints] attribute
        bool hasGenerateAttribute = typeSymbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAttrSymbol));
        if (!hasGenerateAttribute)
        {
            return null;
        }

        // Check for [ProjectionPath] attribute and get path
        AttributeData? projectionPathAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(attr =>
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, projectionPathAttrSymbol));
        if (projectionPathAttr is null)
        {
            return null;
        }

        // Get the path from constructor argument
        string? projectionPath = projectionPathAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(projectionPath))
        {
            return null;
        }

        // Create projection model
        ProjectionModel model = new(typeSymbol, projectionPath!);

        // Determine output namespace (replace Domain with Server, add Controllers.Projections)
        string outputNamespace = DeriveOutputNamespace(model.Namespace);
        return new(model, outputNamespace);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Use the compilation provider to scan both current and referenced types
        IncrementalValueProvider<List<ProjectionInfo>> projectionsProvider = context.CompilationProvider.Select((
            compilation,
            _
        ) => GetProjectionsFromCompilation(compilation));

        // Register source output
        context.RegisterSourceOutput(
            projectionsProvider,
            static (
                spc,
                projections
            ) =>
            {
                HashSet<string> generatedNestedTypes = new();
                foreach (ProjectionInfo projection in projections)
                {
                    GenerateCode(spc, projection, generatedNestedTypes);
                }
            });
    }

    /// <summary>
    ///     Holds projection information for generation.
    /// </summary>
    private sealed class ProjectionInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProjectionInfo" /> class.
        /// </summary>
        /// <param name="model">The projection model.</param>
        /// <param name="outputNamespace">The output namespace for generated code.</param>
        public ProjectionInfo(
            ProjectionModel model,
            string outputNamespace
        )
        {
            Model = model;
            OutputNamespace = outputNamespace;
        }

        /// <summary>
        ///     Gets the projection model.
        /// </summary>
        public ProjectionModel Model { get; }

        /// <summary>
        ///     Gets the output namespace.
        /// </summary>
        public string OutputNamespace { get; }
    }
}