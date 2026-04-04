using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Gateway.Generators;

/// <summary>
///     Generates MCP tool classes for sagas marked with [GenerateMcpSagaTools].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>A tools class with [McpServerToolType] for each saga.</item>
///         <item>A start tool method that initiates the saga with input parameters.</item>
///         <item>A status tool method that reads the current saga state as JSON.</item>
///         <item>A runtime-status tool method that reads metadata-only recovery status as JSON.</item>
///         <item>A resume tool method that requests a manual saga resume and returns typed JSON.</item>
///     </list>
///     <para>
///         The generator scans referenced assemblies to find saga state types decorated with
///         both [GenerateSagaEndpoints] and [GenerateMcpSagaTools], then generates MCP tool
///         classes with descriptions derived from a single source definition.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class McpSagaToolsGenerator : IIncrementalGenerator
{
    private const string CancellationTokenDefaultParameter = "CancellationToken cancellationToken = default";

    private const string CancellationTokenParamComment =
        "/// <param name=\"cancellationToken\">Cancellation token.</param>";

    private const string GenerateMcpParameterDescriptionAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpParameterDescriptionAttribute";

    private const string GenerateMcpSagaToolsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpSagaToolsAttribute";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeGenericFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute`1";

    private const string McpServerToolAttributePrefix = "[McpServerTool(Name = \"";

    private const string OpenWorldFalseFragment = ", OpenWorld = false";

    private const string SagaStateInterfaceFullName = "Mississippi.DomainModeling.Abstractions.ISagaState";

    private const string TitleFragment = ", Title = \"";

    private static void AddDescriptionIfPresent(
        ISymbol symbol,
        string key,
        INamedTypeSymbol mcpParamDescAttrSymbol,
        Dictionary<string, string> parameterDescriptions
    )
    {
        AttributeData? attribute = symbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpParamDescAttrSymbol));
        if (attribute is not { ConstructorArguments.Length: > 0 })
        {
            return;
        }

        string? description = attribute.ConstructorArguments[0].Value?.ToString();
        if (!string.IsNullOrEmpty(description))
        {
            parameterDescriptions[key] = description!;
        }
    }

    private static void AddDescriptionsFromPrimaryConstructor(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol mcpParamDescAttrSymbol,
        Dictionary<string, string> parameterDescriptions
    )
    {
        IMethodSymbol? primaryConstructor =
            typeSymbol.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
        if (!typeSymbol.IsRecord || primaryConstructor is null || (primaryConstructor.Parameters.Length == 0))
        {
            return;
        }

        foreach (IParameterSymbol parameter in primaryConstructor.Parameters)
        {
            if (HasDescriptionForParameter(parameterDescriptions, parameter.Name))
            {
                continue;
            }

            AddDescriptionIfPresent(parameter, parameter.Name, mcpParamDescAttrSymbol, parameterDescriptions);
        }
    }

    private static void AddDescriptionsFromProperties(
        IEnumerable<IPropertySymbol> publicProperties,
        INamedTypeSymbol mcpParamDescAttrSymbol,
        Dictionary<string, string> parameterDescriptions
    )
    {
        foreach (IPropertySymbol propertySymbol in publicProperties)
        {
            AddDescriptionIfPresent(propertySymbol, propertySymbol.Name, mcpParamDescAttrSymbol, parameterDescriptions);
        }
    }

    private static Dictionary<string, string> CollectParameterDescriptions(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol
    )
    {
        Dictionary<string, string> parameterDescriptions = new();
        if (mcpParamDescAttrSymbol is null)
        {
            return parameterDescriptions;
        }

        IPropertySymbol[] publicProperties = GetPublicReadableInstanceProperties(typeSymbol);
        AddDescriptionsFromProperties(publicProperties, mcpParamDescAttrSymbol, parameterDescriptions);
        AddDescriptionsFromPrimaryConstructor(typeSymbol, mcpParamDescAttrSymbol, parameterDescriptions);
        return parameterDescriptions;
    }

    private static string EscapeForStringLiteral(
        string value
    ) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void FindSagasInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? sagaEndpointsAttrSymbol,
        INamedTypeSymbol? sagaEndpointsGenericAttrSymbol,
        INamedTypeSymbol mcpSagaToolsAttrSymbol,
        INamedTypeSymbol sagaStateSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol,
        List<McpSagaInfo> sagas,
        string targetRootNamespace
    )
    {
        sagas.AddRange(
            namespaceSymbol.GetTypeMembers()
                .Select(typeSymbol => TryGetSagaInfo(
                    typeSymbol,
                    sagaEndpointsAttrSymbol,
                    sagaEndpointsGenericAttrSymbol,
                    mcpSagaToolsAttrSymbol,
                    sagaStateSymbol,
                    mcpParamDescAttrSymbol,
                    targetRootNamespace))
                .Where(info => info is not null)!);
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindSagasInNamespace(
                childNs,
                sagaEndpointsAttrSymbol,
                sagaEndpointsGenericAttrSymbol,
                mcpSagaToolsAttrSymbol,
                sagaStateSymbol,
                mcpParamDescAttrSymbol,
                sagas,
                targetRootNamespace);
        }
    }

    private static void GenerateResumeToolMethod(
        SourceBuilder sb,
        McpSagaInfo saga
    )
    {
        string baseDescription = saga.Description ?? $"the {saga.SagaName} saga";
        string trimmed = baseDescription.TrimEnd('.');
        if ((trimmed.Length > 0) && char.IsUpper(trimmed[0]))
        {
            trimmed = char.ToLowerInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        string description = $"Requests a manual resume for {trimmed}.";
        string toolName = saga.ToolPrefix + "_resume";
        string methodName = "Resume" + saga.SagaName + "Async";
        string title = saga.Title is not null ? saga.Title + " Resume" : string.Empty;
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"sagaId\">The saga identifier returned when the saga was started.</param>");
        sb.AppendLine(CancellationTokenParamComment);
        sb.AppendLine("/// <returns>A JSON representation of the typed manual-resume response.</returns>");
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append(McpServerToolAttributePrefix).Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(TitleFragment).Append(EscapeForStringLiteral(title)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = true");
        toolAttrBuilder.Append(", ReadOnly = false");
        toolAttrBuilder.Append(", Idempotent = false");
        toolAttrBuilder.Append(OpenWorldFalseFragment);
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine(
            "[Description(\"The saga identifier (GUID) returned when the saga was started\")] string sagaId,");
        sb.AppendLine(CancellationTokenDefaultParameter);
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine(
            "SagaResumeResponse? response = await SagaRecoveryService.ResumeAsync(sagaId, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("return JsonSerializer.Serialize(response, JsonSerializerOptions.Web);");
        sb.CloseBrace();
    }

    private static void GenerateRuntimeStatusToolMethod(
        SourceBuilder sb,
        McpSagaInfo saga
    )
    {
        string baseDescription = saga.Description ?? $"the {saga.SagaName} saga";
        string trimmed = baseDescription.TrimEnd('.');
        if ((trimmed.Length > 0) && char.IsUpper(trimmed[0]))
        {
            trimmed = char.ToLowerInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        string description = $"Gets the metadata-only runtime recovery status of {trimmed}.";
        string toolName = saga.ToolPrefix + "_runtime_status";
        string methodName = "Get" + saga.SagaName + "RuntimeStatusAsync";
        string title = saga.Title is not null ? saga.Title + " Runtime Status" : string.Empty;
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"sagaId\">The saga identifier returned when the saga was started.</param>");
        sb.AppendLine(CancellationTokenParamComment);
        sb.AppendLine("/// <returns>A JSON representation of the saga runtime status.</returns>");
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append(McpServerToolAttributePrefix).Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(TitleFragment).Append(EscapeForStringLiteral(title)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = false");
        toolAttrBuilder.Append(", ReadOnly = true");
        toolAttrBuilder.Append(", Idempotent = true");
        toolAttrBuilder.Append(OpenWorldFalseFragment);
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine(
            "[Description(\"The saga identifier (GUID) returned when the saga was started\")] string sagaId,");
        sb.AppendLine(CancellationTokenDefaultParameter);
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine(
            "SagaRuntimeStatus? status = await SagaRecoveryService.GetRuntimeStatusAsync(sagaId, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("return JsonSerializer.Serialize(status, JsonSerializerOptions.Web);");
        sb.CloseBrace();
    }

    private static void GenerateStartToolMethod(
        SourceBuilder sb,
        McpSagaInfo saga
    )
    {
        string description = saga.Description is not null ? saga.Description : $"Starts the {saga.SagaName} saga.";
        string toolName = saga.ToolPrefix;
        string methodName = saga.SagaName + "Async";
        sb.AppendSummary(description);
        foreach (string propName in saga.InputProperties.Select(prop => prop.Name))
        {
            string paramName = NamingConventions.ToCamelCase(propName);
            string paramDoc = saga.ParameterDescriptions.TryGetValue(propName, out string? customDoc)
                ? customDoc
                : ToHumanReadable(propName);
            sb.AppendLine($"/// <param name=\"{paramName}\">{EscapeForStringLiteral(paramDoc)}</param>");
        }

        sb.AppendLine(CancellationTokenParamComment);
        sb.AppendLine(
            "/// <returns>A message indicating whether the saga was started, including the saga identifier.</returns>");
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append(McpServerToolAttributePrefix).Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(saga.Title))
        {
            toolAttrBuilder.Append(TitleFragment).Append(EscapeForStringLiteral(saga.Title!)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = true");
        toolAttrBuilder.Append(", ReadOnly = false");
        toolAttrBuilder.Append(", Idempotent = false");
        toolAttrBuilder.Append(OpenWorldFalseFragment);
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();

        // Generate parameters from input type properties
        for (int i = 0; i < saga.InputProperties.Length; i++)
        {
            PropertyModel prop = saga.InputProperties[i];
            string paramName = NamingConventions.ToCamelCase(prop.Name);
            string paramType = prop.SourceTypeName;
            string descriptionText = saga.ParameterDescriptions.TryGetValue(prop.Name, out string? customParamDesc)
                ? EscapeForStringLiteral(customParamDesc)
                : ToHumanReadable(prop.Name);
            sb.AppendLine($"[Description(\"{descriptionText}\")] {paramType} {paramName},");
        }

        sb.AppendLine(CancellationTokenDefaultParameter);
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("Guid sagaId = Guid.NewGuid();");
        sb.AppendLine();

        // Create the input instance
        if (saga.IsInputPositionalRecord)
        {
            string args = string.Join(", ", saga.InputProperties.Select(p => NamingConventions.ToCamelCase(p.Name)));
            sb.AppendLine($"{saga.InputTypeName} input = new({args});");
        }
        else
        {
            sb.AppendLine($"{saga.InputTypeName} input = new()");
            sb.OpenBrace();
            foreach (string propName in saga.InputProperties.Select(prop => prop.Name))
            {
                sb.AppendLine($"{propName} = {NamingConventions.ToCamelCase(propName)},");
            }

            sb.DecreaseIndent();
            sb.AppendLine("};");
        }

        sb.AppendLine();
        sb.AppendLine($"StartSagaCommand<{saga.InputTypeName}> command = new()");
        sb.OpenBrace();
        sb.AppendLine("SagaId = sagaId,");
        sb.AppendLine("Input = input,");
        sb.DecreaseIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"IGenericAggregateGrain<{saga.SagaStateTypeName}> grain =");
        sb.IncreaseIndent();
        sb.AppendLine($"AggregateGrainFactory.GetGenericAggregate<{saga.SagaStateTypeName}>(sagaId.ToString());");
        sb.DecreaseIndent();
        sb.AppendLine();
        sb.AppendLine("OperationResult result = await grain.ExecuteAsync(command, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("return result.Success");
        sb.IncreaseIndent();
        sb.AppendLine($"? $\"{saga.SagaName} started successfully. Saga ID: {{sagaId}}\"");
        sb.AppendLine($": $\"Failed to start {saga.SagaName}: [{{result.ErrorCode}}] {{result.ErrorMessage}}\";");
        sb.DecreaseIndent();
        sb.CloseBrace();
    }

    private static void GenerateStatusToolMethod(
        SourceBuilder sb,
        McpSagaInfo saga
    )
    {
        string baseDescription = saga.Description ?? $"the {saga.SagaName} saga";
        string trimmed = baseDescription.TrimEnd('.');
        if ((trimmed.Length > 0) && char.IsUpper(trimmed[0]))
        {
            trimmed = char.ToLowerInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        string description = $"Gets the current status and state of {trimmed}.";
        string toolName = saga.ToolPrefix + "_status";
        string methodName = "Get" + saga.SagaName + "StatusAsync";
        string title = saga.Title is not null ? saga.Title + " Status" : string.Empty;
        sb.AppendSummary(description);
        sb.AppendLine("/// <param name=\"sagaId\">The saga identifier returned when the saga was started.</param>");
        sb.AppendLine(CancellationTokenParamComment);
        sb.AppendLine("/// <returns>A JSON representation of the saga state.</returns>");
        StringBuilder toolAttrBuilder = new();
        toolAttrBuilder.Append(McpServerToolAttributePrefix).Append(toolName).Append('"');
        if (!string.IsNullOrEmpty(title))
        {
            toolAttrBuilder.Append(TitleFragment).Append(EscapeForStringLiteral(title)).Append('"');
        }

        toolAttrBuilder.Append(", Destructive = false");
        toolAttrBuilder.Append(", ReadOnly = true");
        toolAttrBuilder.Append(", Idempotent = true");
        toolAttrBuilder.Append(OpenWorldFalseFragment);
        toolAttrBuilder.Append(")]");
        sb.AppendLine(toolAttrBuilder.ToString());
        sb.AppendLine($"[Description(\"{EscapeForStringLiteral(description)}\")]");
        sb.AppendLine($"public async Task<string> {methodName}(");
        sb.IncreaseIndent();
        sb.AppendLine(
            "[Description(\"The saga identifier (GUID) returned when the saga was started\")] string sagaId,");
        sb.AppendLine(CancellationTokenDefaultParameter);
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine(
            $"{saga.SagaStateTypeName}? state = await SagaRecoveryService.GetStateAsync(sagaId, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("if (state is null)");
        sb.OpenBrace();
        sb.AppendLine($"return $\"No {saga.SagaName} saga found with ID '{{sagaId}}'.\";");
        sb.CloseBrace();
        sb.AppendLine();
        sb.AppendLine("return JsonSerializer.Serialize(state, JsonSerializerOptions.Web);");
        sb.CloseBrace();
    }

    private static string GenerateToolsClass(
        McpSagaInfo saga
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();

        // System usings
        sb.AppendUsing("System");
        sb.AppendUsing("System.ComponentModel");
        sb.AppendUsing("System.Text.Json");
        sb.AppendUsing("System.Threading");
        sb.AppendUsing("System.Threading.Tasks");
        sb.AppendLine();

        // MCP usings
        sb.AppendUsing("ModelContextProtocol.Server");
        sb.AppendLine();

        // Mississippi usings
        sb.AppendUsing("Mississippi.DomainModeling.Abstractions");
        sb.AppendLine();

        // Domain usings
        sb.AppendUsing(saga.SagaNamespace);
        if (!string.Equals(saga.InputTypeNamespace, saga.SagaNamespace, StringComparison.Ordinal))
        {
            sb.AppendUsing(saga.InputTypeNamespace);
        }

        sb.AppendFileScopedNamespace(saga.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"MCP tools for the {saga.SagaName} saga.");
        sb.AppendGeneratedCodeAttribute("McpSagaToolsGenerator");
        sb.AppendLine("[McpServerToolType]");
        sb.AppendLine($"public sealed class {saga.ToolsClassName}");
        sb.OpenBrace();

        // DI property
        sb.AppendSummary("Gets the aggregate grain factory.");
        sb.AppendLine("private IAggregateGrainFactory AggregateGrainFactory { get; }");
        sb.AppendSummary(
            "Gets the saga recovery service used for raw status, runtime-status, and manual-resume operations.");
        sb.AppendLine($"private ISagaRecoveryService<{saga.SagaStateTypeName}> SagaRecoveryService {{ get; }}");
        sb.AppendLine();

        // Constructor
        sb.AppendSummary($"Initializes a new instance of the <see cref=\"{saga.ToolsClassName}\" /> class.");
        sb.AppendLine("/// <param name=\"aggregateGrainFactory\">Factory for resolving saga grains.</param>");
        sb.AppendLine(
            "/// <param name=\"sagaRecoveryService\">Service for raw-status, runtime-status, and manual-resume saga operations.</param>");
        sb.AppendLine($"public {saga.ToolsClassName}(");
        sb.IncreaseIndent();
        sb.AppendLine("IAggregateGrainFactory aggregateGrainFactory,");
        sb.AppendLine($"ISagaRecoveryService<{saga.SagaStateTypeName}> sagaRecoveryService");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        sb.AppendLine("AggregateGrainFactory = aggregateGrainFactory;");
        sb.AppendLine("SagaRecoveryService = sagaRecoveryService;");
        sb.CloseBrace();
        sb.AppendLine();

        // Generate start tool method
        GenerateStartToolMethod(sb, saga);
        sb.AppendLine();

        // Generate status tool method
        GenerateStatusToolMethod(sb, saga);
        sb.AppendLine();

        // Generate runtime-status tool method
        GenerateRuntimeStatusToolMethod(sb, saga);
        sb.AppendLine();

        // Generate resume tool method
        GenerateResumeToolMethod(sb, saga);
        sb.CloseBrace();
        return sb.ToString();
    }

    private static IPropertySymbol[] GetPublicReadableInstanceProperties(
        INamedTypeSymbol typeSymbol
    ) =>
        typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic)
            .Where(p => p.GetMethod is not null)
            .ToArray();

    private static List<McpSagaInfo> GetSagasFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<McpSagaInfo> sagas = [];
        INamedTypeSymbol? mcpSagaToolsAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpSagaToolsAttributeFullName);
        if (mcpSagaToolsAttrSymbol is null)
        {
            return sagas;
        }

        INamedTypeSymbol? sagaEndpointsAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        INamedTypeSymbol? sagaEndpointsGenericAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeGenericFullName);
        INamedTypeSymbol? sagaStateSymbol = compilation.GetTypeByMetadataName(SagaStateInterfaceFullName);
        if ((sagaEndpointsAttrSymbol is null && sagaEndpointsGenericAttrSymbol is null) || sagaStateSymbol is null)
        {
            return sagas;
        }

        INamedTypeSymbol? mcpParamDescAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpParameterDescriptionAttributeFullName);
        foreach (IAssemblySymbol referencedAssembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
        {
            FindSagasInNamespace(
                referencedAssembly.GlobalNamespace,
                sagaEndpointsAttrSymbol,
                sagaEndpointsGenericAttrSymbol,
                mcpSagaToolsAttrSymbol,
                sagaStateSymbol,
                mcpParamDescAttrSymbol,
                sagas,
                targetRootNamespace);
        }

        return sagas;
    }

    private static bool HasDescriptionForParameter(
        Dictionary<string, string> parameterDescriptions,
        string parameterName
    ) =>
        parameterDescriptions.ContainsKey(parameterName) ||
        parameterDescriptions.ContainsKey(ToPascalCase(parameterName));

    private static bool MatchesSagaEndpointsAttribute(
        AttributeData attr,
        INamedTypeSymbol? sagaAttrSymbol,
        INamedTypeSymbol? sagaAttrGenericSymbol
    )
    {
        if (attr.AttributeClass is null)
        {
            return false;
        }

        if (sagaAttrSymbol is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, sagaAttrSymbol))
        {
            return true;
        }

        return sagaAttrGenericSymbol is not null &&
               SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition, sagaAttrGenericSymbol);
    }

    private static string RemoveSagaSuffix(
        string typeName
    ) =>
        typeName.EndsWith("SagaState", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "SagaState".Length)
            : typeName;

    private static string ToHumanReadable(
        string pascalCase
    )
    {
        if (string.IsNullOrEmpty(pascalCase))
        {
            return pascalCase;
        }

        StringBuilder sb = new();
        sb.Append(char.ToLowerInvariant(pascalCase[0]));
        for (int i = 1; i < pascalCase.Length; i++)
        {
            if (char.IsUpper(pascalCase[i]))
            {
                sb.Append(' ');
                sb.Append(char.ToLowerInvariant(pascalCase[i]));
            }
            else
            {
                sb.Append(pascalCase[i]);
            }
        }

        return sb.ToString();
    }

    private static string ToPascalCase(
        string value
    ) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value.Substring(1);

    private static string ToSnakeCase(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        StringBuilder sb = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static bool TryGetInputType(
        AttributeData attr,
        out INamedTypeSymbol? inputTypeSymbol
    )
    {
        inputTypeSymbol = null;
        if (attr.AttributeClass is not null && attr.AttributeClass.IsGenericType)
        {
            inputTypeSymbol = attr.AttributeClass.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        if (inputTypeSymbol is not null)
        {
            return true;
        }

        inputTypeSymbol =
            attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "InputType").Value.Value as INamedTypeSymbol;
        return inputTypeSymbol is not null;
    }

    private static McpSagaInfo? TryGetSagaInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? sagaEndpointsAttrSymbol,
        INamedTypeSymbol? sagaEndpointsGenericAttrSymbol,
        INamedTypeSymbol mcpSagaToolsAttrSymbol,
        INamedTypeSymbol sagaStateSymbol,
        INamedTypeSymbol? mcpParamDescAttrSymbol,
        string targetRootNamespace
    )
    {
        // Must have [GenerateMcpSagaTools]
        AttributeData? mcpAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpSagaToolsAttrSymbol));
        if (mcpAttr is null)
        {
            return null;
        }

        // Must also have [GenerateSagaEndpoints]
        AttributeData? sagaAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => MatchesSagaEndpointsAttribute(
                a,
                sagaEndpointsAttrSymbol,
                sagaEndpointsGenericAttrSymbol));
        if (sagaAttr is null)
        {
            return null;
        }

        // Must implement ISagaState
        if (!typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sagaStateSymbol)))
        {
            return null;
        }

        // Get input type from [GenerateSagaEndpoints]
        if (!TryGetInputType(sagaAttr, out INamedTypeSymbol? inputTypeSymbol) || inputTypeSymbol is null)
        {
            return null;
        }

        // Read MCP metadata from [GenerateMcpSagaTools]
        string? title = mcpAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Title").Value.Value?.ToString();
        string? description = mcpAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Description")
            .Value.Value?.ToString();
        string? toolPrefix = mcpAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ToolPrefix")
            .Value.Value?.ToString();
        string sagaName = RemoveSagaSuffix(typeSymbol.Name);
        if (string.IsNullOrEmpty(toolPrefix))
        {
            toolPrefix = ToSnakeCase(sagaName);
        }

        // Read input type properties
        IMethodSymbol? primaryConstructor =
            inputTypeSymbol.Constructors.FirstOrDefault(c => (c.Parameters.Length > 0) && !c.IsStatic);
        bool isInputPositionalRecord = inputTypeSymbol.IsRecord && (primaryConstructor?.Parameters.Length > 0);
        IPropertySymbol[] publicProperties = inputTypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic)
            .Where(p => p.GetMethod is not null)
            .ToArray();
        Dictionary<string, string> parameterDescriptions =
            CollectParameterDescriptions(inputTypeSymbol, mcpParamDescAttrSymbol);
        ImmutableArray<PropertyModel> inputProperties = publicProperties
            .Select(p => new PropertyModel(p))
            .ToImmutableArray();
        string sagaNamespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        string inputTypeNamespace = TypeAnalyzer.GetFullNamespace(inputTypeSymbol);
        string outputNamespace = targetRootNamespace + ".McpTools";
        return new(
            sagaName,
            typeSymbol.Name,
            sagaNamespace,
            inputTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            inputTypeNamespace,
            outputNamespace,
            toolPrefix!,
            title,
            description,
            inputProperties,
            isInputPositionalRecord,
            parameterDescriptions);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        IncrementalValueProvider<List<McpSagaInfo>> sagasProvider = compilationAndOptions.Select((
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
            return GetSagasFromCompilation(source.Compilation, targetRootNamespace);
        });
        context.RegisterSourceOutput(
            sagasProvider,
            static (
                spc,
                sagas
            ) =>
            {
                foreach (McpSagaInfo saga in sagas)
                {
                    string toolsSource = GenerateToolsClass(saga);
                    spc.AddSource($"{saga.ToolsClassName}.g.cs", SourceText.From(toolsSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Holds metadata about an MCP-enabled saga.
    /// </summary>
    internal sealed class McpSagaInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="McpSagaInfo" /> class.
        /// </summary>
        /// <param name="sagaName">The saga name without the "SagaState" suffix.</param>
        /// <param name="sagaStateTypeName">The full type name of the saga state.</param>
        /// <param name="sagaNamespace">The namespace containing the saga state.</param>
        /// <param name="inputTypeName">The display name of the saga input type.</param>
        /// <param name="inputTypeNamespace">The namespace containing the saga input type.</param>
        /// <param name="outputNamespace">The target output namespace for generated tools.</param>
        /// <param name="toolPrefix">The prefix for generated tool names.</param>
        /// <param name="title">An optional human-readable title for the tools.</param>
        /// <param name="description">An optional base description for the saga.</param>
        /// <param name="inputProperties">The public properties of the saga input type.</param>
        /// <param name="isInputPositionalRecord">Whether the input type is a positional record.</param>
        /// <param name="parameterDescriptions">Custom descriptions keyed by property name.</param>
        public McpSagaInfo(
            string sagaName,
            string sagaStateTypeName,
            string sagaNamespace,
            string inputTypeName,
            string inputTypeNamespace,
            string outputNamespace,
            string toolPrefix,
            string? title,
            string? description,
            ImmutableArray<PropertyModel> inputProperties,
            bool isInputPositionalRecord,
            Dictionary<string, string> parameterDescriptions
        )
        {
            SagaName = sagaName;
            SagaStateTypeName = sagaStateTypeName;
            SagaNamespace = sagaNamespace;
            InputTypeName = inputTypeName;
            InputTypeNamespace = inputTypeNamespace;
            OutputNamespace = outputNamespace;
            ToolPrefix = toolPrefix;
            Title = title;
            Description = description;
            InputProperties = inputProperties;
            IsInputPositionalRecord = isInputPositionalRecord;
            ParameterDescriptions = parameterDescriptions;
            ToolsClassName = sagaName + "SagaMcpTools";
        }

        /// <summary>
        ///     Gets an optional base description for the saga.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        ///     Gets the public properties of the saga input type.
        /// </summary>
        public ImmutableArray<PropertyModel> InputProperties { get; }

        /// <summary>
        ///     Gets the display name of the saga input type.
        /// </summary>
        public string InputTypeName { get; }

        /// <summary>
        ///     Gets the namespace containing the saga input type.
        /// </summary>
        public string InputTypeNamespace { get; }

        /// <summary>
        ///     Gets a value indicating whether the input type is a positional record.
        /// </summary>
        public bool IsInputPositionalRecord { get; }

        /// <summary>
        ///     Gets the target output namespace for generated tools.
        /// </summary>
        public string OutputNamespace { get; }

        /// <summary>
        ///     Gets custom parameter descriptions keyed by property name.
        /// </summary>
        public Dictionary<string, string> ParameterDescriptions { get; }

        /// <summary>
        ///     Gets the saga name without the "SagaState" suffix.
        /// </summary>
        public string SagaName { get; }

        /// <summary>
        ///     Gets the namespace containing the saga state.
        /// </summary>
        public string SagaNamespace { get; }

        /// <summary>
        ///     Gets the full type name of the saga state.
        /// </summary>
        public string SagaStateTypeName { get; }

        /// <summary>
        ///     Gets an optional human-readable title for the tools.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        ///     Gets the prefix for generated tool names.
        /// </summary>
        public string ToolPrefix { get; }

        /// <summary>
        ///     Gets the generated tools class name.
        /// </summary>
        public string ToolsClassName { get; }
    }
}