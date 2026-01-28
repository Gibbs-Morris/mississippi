using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Silo.Generators;

/// <summary>
///     Generates silo-side service registrations for sagas marked with [GenerateSagaEndpoints].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>
///             Add{SagaName}Saga() extension method that registers saga steps, compensations,
///             event effects, reducers, and snapshot converters.
///         </item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find saga types decorated with [GenerateSagaEndpoints] and their
///         associated steps, compensations, reducers, and effects in sibling namespaces.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class SagaSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string EventReducerBaseFullName =
        "Mississippi.EventSourcing.Reducers.Abstractions.EventReducerBase`2";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string ISagaStateFullName = "Mississippi.EventSourcing.Sagas.Abstractions.ISagaState";

    private const string SagaCompensationAttributeFullName =
        "Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensationAttribute";

    private const string SagaCompensationBaseFullName =
        "Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensationBase`1";

    private const string SagaStepAttributeFullName = "Mississippi.EventSourcing.Sagas.Abstractions.SagaStepAttribute";

    private const string SagaStepBaseFullName = "Mississippi.EventSourcing.Sagas.Abstractions.SagaStepBase`1";

    /// <summary>
    ///     Finds saga compensations in the Compensations sub-namespace.
    /// </summary>
    private static List<CompensationInfo> FindCompensationsForSaga(
        INamespaceSymbol sagaNamespace,
        INamedTypeSymbol sagaSymbol,
        INamedTypeSymbol? compensationBaseSymbol,
        INamedTypeSymbol? compensationAttrSymbol
    )
    {
        List<CompensationInfo> compensations = [];
        if (compensationBaseSymbol is null || compensationAttrSymbol is null)
        {
            return compensations;
        }

        // Look for Compensations sub-namespace
        INamespaceSymbol? compensationsNs = sagaNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Compensations");
        if (compensationsNs is null)
        {
            return compensations;
        }

        // Find all types that extend SagaCompensationBase<TSaga> and have [SagaCompensation]
        foreach (INamedTypeSymbol typeSymbol in compensationsNs.GetTypeMembers())
        {
            CompensationInfo? info = TryCreateCompensationInfo(typeSymbol, sagaSymbol, compensationAttrSymbol);
            if (info is not null)
            {
                compensations.Add(info);
            }
        }

        return compensations;
    }

    /// <summary>
    ///     Finds reducers for a saga in the Reducers sub-namespace.
    /// </summary>
    private static List<ReducerInfo> FindReducersForSaga(
        INamespaceSymbol sagaNamespace,
        INamedTypeSymbol sagaSymbol,
        INamedTypeSymbol? reducerBaseSymbol
    )
    {
        List<ReducerInfo> reducers = [];
        if (reducerBaseSymbol is null)
        {
            return reducers;
        }

        // Look for Reducers sub-namespace
        INamespaceSymbol? reducersNs = sagaNamespace.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == "Reducers");
        if (reducersNs is null)
        {
            return reducers;
        }

        // Find all types that extend EventReducerBase<TEvent, TSaga>
        foreach (INamedTypeSymbol typeSymbol in reducersNs.GetTypeMembers())
        {
            ReducerInfo? reducerInfo = TryCreateReducerInfo(typeSymbol, sagaSymbol);
            if (reducerInfo is not null)
            {
                reducers.Add(reducerInfo);
            }
        }

        return reducers;
    }

    /// <summary>
    ///     Recursively finds sagas in a namespace.
    /// </summary>
    private static void FindSagasInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol sagaAttrSymbol,
        INamedTypeSymbol? stepBaseSymbol,
        INamedTypeSymbol? compensationBaseSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        INamedTypeSymbol? stepAttrSymbol,
        INamedTypeSymbol? compensationAttrSymbol,
        INamedTypeSymbol? sagaStateInterfaceSymbol,
        List<SagaRegistrationInfo> sagas,
        string targetRootNamespace
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            SagaRegistrationInfo? info = TryGetSagaInfo(
                typeSymbol,
                sagaAttrSymbol,
                stepBaseSymbol,
                compensationBaseSymbol,
                reducerBaseSymbol,
                stepAttrSymbol,
                compensationAttrSymbol,
                sagaStateInterfaceSymbol,
                targetRootNamespace);
            if (info is not null)
            {
                sagas.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindSagasInNamespace(
                childNs,
                sagaAttrSymbol,
                stepBaseSymbol,
                compensationBaseSymbol,
                reducerBaseSymbol,
                stepAttrSymbol,
                compensationAttrSymbol,
                sagaStateInterfaceSymbol,
                sagas,
                targetRootNamespace);
        }
    }

    /// <summary>
    ///     Finds saga steps in the Steps sub-namespace.
    /// </summary>
    private static List<StepInfo> FindStepsForSaga(
        INamespaceSymbol sagaNamespace,
        INamedTypeSymbol sagaSymbol,
        INamedTypeSymbol? stepBaseSymbol,
        INamedTypeSymbol? stepAttrSymbol
    )
    {
        List<StepInfo> steps = [];
        if (stepBaseSymbol is null || stepAttrSymbol is null)
        {
            return steps;
        }

        // Look for Steps sub-namespace
        INamespaceSymbol? stepsNs = sagaNamespace.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == "Steps");
        if (stepsNs is null)
        {
            return steps;
        }

        // Find all types that extend SagaStepBase<TSaga> and have [SagaStep]
        foreach (INamedTypeSymbol typeSymbol in stepsNs.GetTypeMembers())
        {
            StepInfo? stepInfo = TryCreateStepInfo(typeSymbol, sagaSymbol, stepAttrSymbol);
            if (stepInfo is not null)
            {
                steps.Add(stepInfo);
            }
        }

        return steps.OrderBy(s => s.Order).ToList();
    }

    /// <summary>
    ///     Generates the registration extension method for a saga.
    /// </summary>
    private static string GenerateRegistration(
        SagaRegistrationInfo saga
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendUsing("Mississippi.EventSourcing.Aggregates");
        sb.AppendUsing("Mississippi.EventSourcing.Reducers");
        sb.AppendUsing("Mississippi.EventSourcing.Sagas");
        sb.AppendUsing("Mississippi.EventSourcing.Snapshots");

        // Add usings for saga infrastructure reducers and events if saga implements ISagaState
        if (saga.Model.ImplementsISagaState)
        {
            sb.AppendUsing("Mississippi.EventSourcing.Sagas.Abstractions.Events");
            sb.AppendUsing("Mississippi.EventSourcing.Sagas.Reducers");
        }

        // Add using for saga namespace
        sb.AppendUsing(saga.Model.Namespace);

        // Add using for events namespace
        string eventsNamespace = saga.Model.Namespace + ".Events";
        sb.AppendUsing(eventsNamespace);

        // Add using for steps namespace
        if (saga.Steps.Count > 0)
        {
            string stepsNamespace = saga.Model.Namespace + ".Steps";
            sb.AppendUsing(stepsNamespace);
        }

        // Add using for compensations namespace
        if (saga.Compensations.Count > 0)
        {
            string compensationsNamespace = saga.Model.Namespace + ".Compensations";
            sb.AppendUsing(compensationsNamespace);
        }

        // Add using for reducers namespace
        if (saga.Reducers.Count > 0)
        {
            string reducersNamespace = saga.Model.Namespace + ".Reducers";
            sb.AppendUsing(reducersNamespace);
        }

        sb.AppendFileScopedNamespace(saga.OutputNamespace);
        sb.AppendLine();
        string registrationsName = $"{saga.Model.SagaName}SagaRegistrations";
        sb.AppendSummary($"Extension methods for registering {saga.Model.SagaName} saga services.");
        sb.AppendGeneratedCodeAttribute("SagaSiloRegistrationGenerator");
        sb.AppendLine($"public static class {registrationsName}");
        sb.OpenBrace();
        sb.AppendSummary($"Adds the {saga.Model.SagaName} saga services to the service collection.");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"public static IServiceCollection Add{saga.Model.SagaName}Saga(");
        sb.IncreaseIndent();
        sb.AppendLine("this IServiceCollection services");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Add saga infrastructure (step registry, effects)
        sb.AppendLine("// Add saga infrastructure (step registry, effects, orchestration)");
        if (!string.IsNullOrEmpty(saga.Model.InputTypeName))
        {
            sb.AppendLine($"services.AddSaga<{saga.Model.TypeName}, {saga.Model.InputTypeName}>();");
        }
        else
        {
            sb.AppendLine($"services.AddSaga<{saga.Model.TypeName}>();");
        }

        sb.AppendLine("services.AddSagaOrchestration();");
        sb.AppendLine();

        // Register event types for hydration
        sb.AppendLine("// Register event types for hydration");
        HashSet<string> seenEventTypes = new();
        foreach (ReducerInfo reducer in saga.Reducers.Where(r => seenEventTypes.Add(r.EventTypeName)))
        {
            sb.AppendLine($"services.AddEventType<{reducer.EventTypeName}>();");
        }

        // Register infrastructure saga event types if saga implements ISagaState
        if (saga.Model.ImplementsISagaState)
        {
            sb.AppendLine("services.AddEventType<SagaStartedEvent>();");
            sb.AppendLine("services.AddEventType<SagaStepCompletedEvent>();");
            sb.AppendLine("services.AddEventType<SagaStepRetryEvent>();");
            sb.AppendLine("services.AddEventType<SagaCompensatingEvent>();");
            sb.AppendLine("services.AddEventType<SagaCompletedEvent>();");
            sb.AppendLine("services.AddEventType<SagaFailedEvent>();");
        }

        sb.AppendLine();

        // Register reducers for state computation
        if (saga.Reducers.Count > 0)
        {
            sb.AppendLine("// Register reducers for state computation");
            foreach (ReducerInfo reducer in saga.Reducers)
            {
                sb.AppendLine(
                    $"services.AddReducer<{reducer.EventTypeName}, {saga.Model.TypeName}, {reducer.TypeName}>();");
            }

            sb.AppendLine();
        }

        // Register infrastructure saga reducers if saga implements ISagaState
        if (saga.Model.ImplementsISagaState)
        {
            sb.AppendLine("// Register infrastructure saga reducers for ISagaState tracking");
            sb.AppendLine(
                $"services.AddReducer<SagaStartedEvent, {saga.Model.TypeName}, SagaStartedReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine(
                $"services.AddReducer<SagaStepCompletedEvent, {saga.Model.TypeName}, SagaStepCompletedReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine(
                $"services.AddReducer<SagaStepRetryEvent, {saga.Model.TypeName}, SagaStepRetryReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine(
                $"services.AddReducer<SagaCompensatingEvent, {saga.Model.TypeName}, SagaCompensatingReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine(
                $"services.AddReducer<SagaCompletedEvent, {saga.Model.TypeName}, SagaCompletedReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine(
                $"services.AddReducer<SagaFailedEvent, {saga.Model.TypeName}, SagaFailedReducer<{saga.Model.TypeName}>>();");
            sb.AppendLine();
        }

        // Add snapshot state converter
        sb.AppendLine("// Add snapshot state converter for saga snapshots");
        sb.AppendLine($"services.AddSnapshotStateConverter<{saga.Model.TypeName}>();");
        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
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
    ///     Gets saga information from the compilation, including referenced assemblies.
    /// </summary>
    private static List<SagaRegistrationInfo> GetSagasFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<SagaRegistrationInfo> sagas = [];

        // Get the attribute symbols
        INamedTypeSymbol? sagaAttrSymbol = compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        if (sagaAttrSymbol is null)
        {
            return sagas;
        }

        // Get base type symbols for step, compensation, and reducer detection
        INamedTypeSymbol? stepBaseSymbol = compilation.GetTypeByMetadataName(SagaStepBaseFullName);
        INamedTypeSymbol? compensationBaseSymbol = compilation.GetTypeByMetadataName(SagaCompensationBaseFullName);
        INamedTypeSymbol? reducerBaseSymbol = compilation.GetTypeByMetadataName(EventReducerBaseFullName);

        // Get attribute symbols for steps and compensations
        INamedTypeSymbol? stepAttrSymbol = compilation.GetTypeByMetadataName(SagaStepAttributeFullName);
        INamedTypeSymbol? compensationAttrSymbol = compilation.GetTypeByMetadataName(SagaCompensationAttributeFullName);

        // Get ISagaState interface symbol for detecting if saga states implement it
        INamedTypeSymbol? sagaStateInterfaceSymbol = compilation.GetTypeByMetadataName(ISagaStateFullName);

        // Scan all assemblies referenced by this compilation
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindSagasInNamespace(
                referencedAssembly.GlobalNamespace,
                sagaAttrSymbol,
                stepBaseSymbol,
                compensationBaseSymbol,
                reducerBaseSymbol,
                stepAttrSymbol,
                compensationAttrSymbol,
                sagaStateInterfaceSymbol,
                sagas,
                targetRootNamespace);
        }

        return sagas;
    }

    /// <summary>
    ///     Determines if a type extends EventReducerBase for the given saga.
    /// </summary>
    private static bool IsEventReducerBaseType(
        INamedTypeSymbol baseType,
        INamedTypeSymbol sagaSymbol
    )
    {
        if (!baseType.IsGenericType)
        {
            return false;
        }

        INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
        if (constructedFrom is null)
        {
            return false;
        }

        string ns = constructedFrom.ContainingNamespace.ToDisplayString();
        if (ns != "Mississippi.EventSourcing.Reducers.Abstractions")
        {
            return false;
        }

        if (constructedFrom.MetadataName != "EventReducerBase`2")
        {
            return false;
        }

        // Verify the second type argument is our saga
        return (baseType.TypeArguments.Length == 2) &&
               SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], sagaSymbol);
    }

    /// <summary>
    ///     Determines if a type extends SagaCompensationBase.
    /// </summary>
    private static bool IsSagaCompensationBaseType(
        INamedTypeSymbol baseType,
        INamedTypeSymbol sagaSymbol
    )
    {
        if (!baseType.IsGenericType)
        {
            return false;
        }

        INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
        if (constructedFrom is null)
        {
            return false;
        }

        string ns = constructedFrom.ContainingNamespace.ToDisplayString();
        if (ns != "Mississippi.EventSourcing.Sagas.Abstractions")
        {
            return false;
        }

        if (constructedFrom.MetadataName != "SagaCompensationBase`1")
        {
            return false;
        }

        // Verify the type argument is our saga
        return (baseType.TypeArguments.Length == 1) &&
               SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], sagaSymbol);
    }

    /// <summary>
    ///     Determines if a type extends SagaStepBase.
    /// </summary>
    private static bool IsSagaStepBaseType(
        INamedTypeSymbol baseType,
        INamedTypeSymbol sagaSymbol
    )
    {
        if (!baseType.IsGenericType)
        {
            return false;
        }

        INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
        if (constructedFrom is null)
        {
            return false;
        }

        string ns = constructedFrom.ContainingNamespace.ToDisplayString();
        if (ns != "Mississippi.EventSourcing.Sagas.Abstractions")
        {
            return false;
        }

        if (constructedFrom.MetadataName != "SagaStepBase`1")
        {
            return false;
        }

        // Verify the type argument is our saga
        return (baseType.TypeArguments.Length == 1) &&
               SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], sagaSymbol);
    }

    /// <summary>
    ///     Attempts to create a CompensationInfo from a type symbol if it's a valid saga compensation.
    /// </summary>
    private static CompensationInfo? TryCreateCompensationInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaSymbol,
        INamedTypeSymbol compensationAttrSymbol
    )
    {
        // Check for [SagaCompensation] attribute
        AttributeData? compAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, compensationAttrSymbol));
        if (compAttr is null)
        {
            return null;
        }

        // Check if extends SagaCompensationBase<TSaga>
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        if (baseType is null || !IsSagaCompensationBaseType(baseType, sagaSymbol))
        {
            return null;
        }

        // Extract ForStep from attribute
        string? forStep = compAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ForStep").Value.Value?.ToString();
        return new(typeSymbol.ToDisplayString(), typeSymbol.Name, forStep ?? string.Empty);
    }

    /// <summary>
    ///     Attempts to create a ReducerInfo if the type extends EventReducerBase for the given saga.
    /// </summary>
    private static ReducerInfo? TryCreateReducerInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaSymbol
    )
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        if (baseType is null || !IsEventReducerBaseType(baseType, sagaSymbol))
        {
            return null;
        }

        // Extract event type
        ITypeSymbol eventType = baseType.TypeArguments[0];
        return new(
            typeSymbol.ToDisplayString(),
            typeSymbol.Name,
            eventType.ToDisplayString(),
            eventType.Name,
            TypeAnalyzer.GetFullNamespace(eventType));
    }

    /// <summary>
    ///     Attempts to create a StepInfo from a type symbol if it's a valid saga step.
    /// </summary>
    private static StepInfo? TryCreateStepInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaSymbol,
        INamedTypeSymbol stepAttrSymbol
    )
    {
        // Check for [SagaStep] attribute
        AttributeData? stepAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stepAttrSymbol));
        if (stepAttr is null)
        {
            return null;
        }

        // Check if extends SagaStepBase<TSaga>
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        if (baseType is null || !IsSagaStepBaseType(baseType, sagaSymbol))
        {
            return null;
        }

        // Extract order from attribute constructor argument
        int order = 0;
        if (stepAttr.ConstructorArguments.Length > 0)
        {
            order = (int)(stepAttr.ConstructorArguments[0].Value ?? 0);
        }

        return new(typeSymbol.ToDisplayString(), typeSymbol.Name, order);
    }

    /// <summary>
    ///     Tries to get saga info from a type symbol.
    /// </summary>
    private static SagaRegistrationInfo? TryGetSagaInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaAttrSymbol,
        INamedTypeSymbol? stepBaseSymbol,
        INamedTypeSymbol? compensationBaseSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        INamedTypeSymbol? stepAttrSymbol,
        INamedTypeSymbol? compensationAttrSymbol,
        INamedTypeSymbol? sagaStateInterfaceSymbol,
        string targetRootNamespace
    )
    {
        // Check for [GenerateSagaEndpoints] attribute
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sagaAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Get InputType from named argument
        string? inputTypeName = null;
        TypedConstant inputTypeArg = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "InputType").Value;
        if (!inputTypeArg.IsNull && inputTypeArg.Value is INamedTypeSymbol inputTypeSymbol)
        {
            inputTypeName = inputTypeSymbol.ToDisplayString();
        }

        // Remove "SagaState" or "Saga" suffix for the saga name
        string baseName = typeSymbol.Name;
        if (baseName.EndsWith("SagaState", StringComparison.Ordinal))
        {
            baseName = baseName.Substring(0, baseName.Length - "SagaState".Length);
        }
        else if (baseName.EndsWith("Saga", StringComparison.Ordinal))
        {
            baseName = baseName.Substring(0, baseName.Length - "Saga".Length);
        }

        // Check if the saga state implements ISagaState for infrastructure reducer registration
        bool implementsISagaState = sagaStateInterfaceSymbol is not null &&
                                    typeSymbol.AllInterfaces.Any(i =>
                                        SymbolEqualityComparer.Default.Equals(i, sagaStateInterfaceSymbol));

        // Build saga model
        SagaModel model = new(typeSymbol, baseName, inputTypeName, implementsISagaState);

        // Find steps, compensations, and reducers
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<StepInfo> steps = containingNs is not null
            ? FindStepsForSaga(containingNs, typeSymbol, stepBaseSymbol, stepAttrSymbol)
            : [];
        List<CompensationInfo> compensations = containingNs is not null
            ? FindCompensationsForSaga(containingNs, typeSymbol, compensationBaseSymbol, compensationAttrSymbol)
            : [];
        List<ReducerInfo> reducers = containingNs is not null
            ? FindReducersForSaga(containingNs, typeSymbol, reducerBaseSymbol)
            : [];

        // Only generate if there are steps or reducers (sagas need at least steps to be meaningful)
        if ((steps.Count == 0) && (reducers.Count == 0))
        {
            return null;
        }

        // Output namespace: {DomainRoot}.Silo.Registrations based on saga namespace
        string outputNamespace = NamingConventions.GetSiloRegistrationNamespace(model.Namespace, targetRootNamespace);
        return new(model, steps, compensations, reducers, outputNamespace);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Combine compilation with options provider
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)>
            compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        // Use the compilation provider to scan referenced assemblies
        IncrementalValueProvider<List<SagaRegistrationInfo>> sagasProvider = compilationAndOptions.Select((
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

        // Register source output
        context.RegisterSourceOutput(
            sagasProvider,
            static (
                spc,
                sagas
            ) =>
            {
                foreach (SagaRegistrationInfo saga in sagas)
                {
                    string registrationSource = GenerateRegistration(saga);
                    spc.AddSource(
                        $"{saga.Model.SagaName}SagaRegistrations.g.cs",
                        SourceText.From(registrationSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Information about a saga compensation.
    /// </summary>
    private sealed class CompensationInfo
    {
        public CompensationInfo(
            string fullTypeName,
            string typeName,
            string forStep
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            ForStep = forStep;
        }

        public string ForStep { get; }

        public string FullTypeName { get; }

        public string TypeName { get; }
    }

    /// <summary>
    ///     Information about an event reducer.
    /// </summary>
    private sealed class ReducerInfo
    {
        public ReducerInfo(
            string fullTypeName,
            string typeName,
            string eventFullTypeName,
            string eventTypeName,
            string eventNamespace
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            EventFullTypeName = eventFullTypeName;
            EventTypeName = eventTypeName;
            EventNamespace = eventNamespace;
        }

        public string EventFullTypeName { get; }

        public string EventNamespace { get; }

        public string EventTypeName { get; }

        public string FullTypeName { get; }

        public string TypeName { get; }
    }

    /// <summary>
    ///     Model for a saga type.
    /// </summary>
    private sealed class SagaModel
    {
        public SagaModel(
            INamedTypeSymbol typeSymbol,
            string sagaName,
            string? inputTypeName,
            bool implementsISagaState
        )
        {
            TypeName = typeSymbol.Name;
            FullTypeName = typeSymbol.ToDisplayString();
            Namespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
            SagaName = sagaName;
            InputTypeName = inputTypeName;
            ImplementsISagaState = implementsISagaState;
        }

        public string FullTypeName { get; }

        /// <summary>
        ///     Gets a value indicating whether the saga state implements ISagaState.
        /// </summary>
        public bool ImplementsISagaState { get; }

        public string? InputTypeName { get; }

        public string Namespace { get; }

        public string SagaName { get; }

        public string TypeName { get; }
    }

    /// <summary>
    ///     Information about a saga type with its steps, compensations, and reducers.
    /// </summary>
    private sealed class SagaRegistrationInfo
    {
        public SagaRegistrationInfo(
            SagaModel model,
            List<StepInfo> steps,
            List<CompensationInfo> compensations,
            List<ReducerInfo> reducers,
            string outputNamespace
        )
        {
            Model = model;
            Steps = steps;
            Compensations = compensations;
            Reducers = reducers;
            OutputNamespace = outputNamespace;
        }

        public List<CompensationInfo> Compensations { get; }

        public SagaModel Model { get; }

        public string OutputNamespace { get; }

        public List<ReducerInfo> Reducers { get; }

        public List<StepInfo> Steps { get; }
    }

    /// <summary>
    ///     Information about a saga step.
    /// </summary>
    private sealed class StepInfo
    {
        public StepInfo(
            string fullTypeName,
            string typeName,
            int order
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            Order = order;
        }

        public string FullTypeName { get; }

        public int Order { get; }

        public string TypeName { get; }
    }
}