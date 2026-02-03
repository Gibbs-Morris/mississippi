using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Inlet.Generators.Core.Emit;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Silo.Generators;

/// <summary>
///     Generates silo-side registrations for sagas marked with [GenerateSagaEndpoints].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SagaSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string CompensatableInterfaceFullName =
        "Mississippi.EventSourcing.Sagas.Abstractions.ICompensatable`1";

    private const string EventReducerBaseFullName =
        "Mississippi.EventSourcing.Reducers.Abstractions.EventReducerBase`2";

    private const string GenerateSagaEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute";

    private const string GenerateSagaEndpointsAttributeGenericFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaEndpointsAttribute`1";

    private const string ReducerRegistrationsTypeFullName = "Mississippi.EventSourcing.Reducers.ReducerRegistrations";

    private const string SagaRegistrationsTypeFullName = "Mississippi.EventSourcing.Sagas.SagaRegistrations";

    private const string SagaStateInterfaceFullName = "Mississippi.EventSourcing.Sagas.Abstractions.ISagaState";

    private const string SagaStepAttributeFullName = "Mississippi.EventSourcing.Sagas.Abstractions.SagaStepAttribute";

    private const string SagaStepInterfaceFullName = "Mississippi.EventSourcing.Sagas.Abstractions.ISagaStep`1";

    private const string SnapshotRegistrationsTypeFullName =
        "Mississippi.EventSourcing.Snapshots.SnapshotRegistrations";

    private static readonly DiagnosticDescriptor SagaDuplicateStepDescriptor = new(
        "MSI1008",
        "Saga step order duplicated",
        "Saga '{0}' defines multiple steps with order {1}",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaMissingInputTypeDescriptor = new(
        "MSI1000",
        "Saga input type missing",
        "Saga '{0}' must specify an input type via [GenerateSagaEndpoints(InputType = typeof(...))] or the generic attribute form",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaMissingStepsDescriptor = new(
        "MSI1007",
        "Saga has no steps",
        "Saga '{0}' defines no steps. At least one [SagaStep] is required.",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaNonContiguousStepsDescriptor = new(
        "MSI1009",
        "Saga step order non-contiguous",
        "Saga '{0}' has non-contiguous step order. Expected {1} but found {2}.",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStateInterfaceMissingDescriptor = new(
        "MSI1001",
        "Saga state interface missing",
        "Saga '{0}' must implement ISagaState",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStepInvalidOrderDescriptor = new(
        "MSI1003",
        "Saga step order invalid",
        "Saga step '{0}' has invalid order {1}. Step order must be zero or greater.",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStepMissingInterfaceDescriptor = new(
        "MSI1004",
        "Saga step interface missing",
        "Saga step '{0}' must implement ISagaStep<{1}>",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStepMissingSagaDescriptor = new(
        "MSI1002",
        "Saga step missing saga association",
        "Saga step '{0}' must implement ISagaStep<TSaga> or specify Saga = typeof(TSaga) on the attribute",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStepMissingSagaRegistrationDescriptor = new(
        "MSI1006",
        "Saga step saga not registered",
        "Saga step '{0}' references saga '{1}' which is not marked with [GenerateSagaEndpoints]",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SagaStepSagaStateInvalidDescriptor = new(
        "MSI1005",
        "Saga step has invalid saga state",
        "Saga step '{0}' specifies saga state '{1}' which does not implement ISagaState",
        "Mississippi.Inlet.Sagas",
        DiagnosticSeverity.Error,
        true);

    private static string GenerateRegistration(
        SagaRegistrationInfo saga
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("System");
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendUsing("Mississippi.EventSourcing.Sagas");
        sb.AppendUsing("Mississippi.EventSourcing.Sagas.Abstractions");
        sb.AppendUsing("Mississippi.EventSourcing.Snapshots");
        sb.AppendUsing("Mississippi.EventSourcing.Reducers");
        sb.AppendUsing(saga.SagaNamespace);
        sb.AppendFileScopedNamespace(saga.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Registrations for the {saga.SagaName} saga.");
        sb.AppendGeneratedCodeAttribute("SagaSiloRegistrationGenerator");
        sb.AppendLine($"public static class {saga.SagaName}SagaRegistrations");
        sb.OpenBrace();
        sb.AppendSummary($"Adds the {saga.SagaName} saga registrations.");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The updated service collection.</returns>");
        sb.AppendLine($"public static IServiceCollection Add{saga.SagaName}Saga(this IServiceCollection services)");
        sb.OpenBrace();
        sb.AppendLine("ArgumentNullException.ThrowIfNull(services);");
        sb.AppendLine($"services.AddSagaOrchestration<{saga.SagaStateTypeName}, {saga.InputTypeName}>();");
        sb.AppendLine($"services.AddSnapshotStateConverter<{saga.SagaStateTypeName}>();");
        foreach (StepInfo step in saga.Steps)
        {
            sb.AppendLine($"services.AddTransient<{step.StepTypeName}>();");
        }

        if (saga.Steps.Count > 0)
        {
            sb.AppendLine($"services.AddSagaStepInfo<{saga.SagaStateTypeName}>(new SagaStepInfo[]");
            sb.OpenBrace();
            foreach (StepInfo step in saga.Steps)
            {
                sb.AppendLine(
                    $"new({step.StepIndex}, \"{step.StepName}\", typeof({step.StepTypeName}), hasCompensation: {(step.HasCompensation ? "true" : "false")}),");
            }

            sb.CloseBrace();
            sb.AppendLine(");");
        }

        foreach (ReducerInfo reducer in saga.Reducers)
        {
            sb.AppendLine(
                $"services.AddReducer<{reducer.EventTypeName}, {saga.SagaStateTypeName}, {reducer.ReducerTypeName}>();");
        }

        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    private static IEnumerable<INamespaceSymbol> GetAllNamespaces(
        INamespaceSymbol namespaceSymbol
    )
    {
        yield return namespaceSymbol;
        foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (INamespaceSymbol descendant in GetAllNamespaces(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(
        Compilation compilation
    )
    {
        foreach (IAssemblySymbol assembly in GetReferencedAssemblies(compilation))
        {
            foreach (INamespaceSymbol namespaceSymbol in GetAllNamespaces(assembly.GlobalNamespace))
            {
                foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
                {
                    yield return typeSymbol;
                }
            }
        }
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

    private static SagaGenerationResult GetSagasWithDiagnostics(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<SagaRegistrationInfo> sagas = [];
        List<Diagnostic> diagnostics = [];
        INamedTypeSymbol? sagaAttrSymbol = compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeFullName);
        INamedTypeSymbol? sagaAttrGenericSymbol =
            compilation.GetTypeByMetadataName(GenerateSagaEndpointsAttributeGenericFullName);
        INamedTypeSymbol? sagaStateSymbol = compilation.GetTypeByMetadataName(SagaStateInterfaceFullName);
        INamedTypeSymbol? sagaStepAttrSymbol = compilation.GetTypeByMetadataName(SagaStepAttributeFullName);
        INamedTypeSymbol? sagaStepInterfaceSymbol = compilation.GetTypeByMetadataName(SagaStepInterfaceFullName);
        INamedTypeSymbol? compensatableInterfaceSymbol =
            compilation.GetTypeByMetadataName(CompensatableInterfaceFullName);
        INamedTypeSymbol? reducerBaseSymbol = compilation.GetTypeByMetadataName(EventReducerBaseFullName);
        if ((sagaAttrSymbol is null && sagaAttrGenericSymbol is null) ||
            sagaStateSymbol is null ||
            sagaStepAttrSymbol is null ||
            sagaStepInterfaceSymbol is null ||
            reducerBaseSymbol is null)
        {
            return new(sagas, diagnostics);
        }

        List<INamedTypeSymbol> allTypes = GetAllTypes(compilation).ToList();
        Dictionary<INamedTypeSymbol, SagaRegistrationInfo> sagaMap = new(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol typeSymbol in allTypes)
        {
            SagaRegistrationInfo? info = TryGetSagaInfo(
                typeSymbol,
                sagaAttrSymbol,
                sagaAttrGenericSymbol,
                sagaStateSymbol,
                targetRootNamespace,
                diagnostics);
            if (info is not null)
            {
                sagaMap[typeSymbol] = info;
            }
        }

        HashSet<INamedTypeSymbol> sagaStatesWithStepAttributes = new(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol typeSymbol in allTypes)
        {
            StepInfoResult stepResult = TryGetStepInfo(
                typeSymbol,
                sagaStepAttrSymbol,
                sagaStepInterfaceSymbol,
                sagaStateSymbol,
                compensatableInterfaceSymbol);
            if (stepResult.Diagnostic is not null)
            {
                diagnostics.Add(stepResult.Diagnostic);
            }

            if (stepResult.SagaStateSymbol is not null)
            {
                sagaStatesWithStepAttributes.Add(stepResult.SagaStateSymbol);
            }

            if (stepResult.Step is not null &&
                sagaMap.TryGetValue(stepResult.Step.SagaStateSymbol, out SagaRegistrationInfo? sagaInfo))
            {
                sagaInfo.Steps.Add(stepResult.Step);
            }
            else if (stepResult.Step is not null)
            {
                Location? location = stepResult.Step.Location ?? typeSymbol.Locations.FirstOrDefault();
                diagnostics.Add(
                    Diagnostic.Create(
                        SagaStepMissingSagaRegistrationDescriptor,
                        location,
                        stepResult.Step.StepName,
                        stepResult.Step.SagaStateSymbol.Name));
            }
        }

        foreach (INamedTypeSymbol typeSymbol in allTypes)
        {
            ReducerInfo? reducer = TryGetReducerInfo(typeSymbol, reducerBaseSymbol);
            if (reducer is not null && sagaMap.TryGetValue(reducer.SagaStateSymbol, out SagaRegistrationInfo? sagaInfo))
            {
                sagaInfo.Reducers.Add(reducer);
            }
        }

        foreach (SagaRegistrationInfo saga in sagaMap.Values)
        {
            saga.Steps.Sort((
                a,
                b
            ) => a.StepIndex.CompareTo(b.StepIndex));
            bool hasStepAttributes = sagaStatesWithStepAttributes.Contains(saga.SagaStateType);
            ValidateSteps(saga, diagnostics, hasStepAttributes);
            sagas.Add(saga);
        }

        return new(sagas, diagnostics);
    }

    private static bool HasRegistrationDependencies(
        Compilation compilation
    ) =>
        compilation.GetTypeByMetadataName(SagaRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(ReducerRegistrationsTypeFullName) is not null &&
        compilation.GetTypeByMetadataName(SnapshotRegistrationsTypeFullName) is not null;

    private static bool MatchesSagaAttribute(
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

    private static ReducerInfo? TryGetReducerInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol reducerBaseSymbol
    )
    {
        INamedTypeSymbol? current = typeSymbol;
        while (current is not null)
        {
            if (current.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, reducerBaseSymbol))
            {
                INamedTypeSymbol? sagaState = current.TypeArguments[1] as INamedTypeSymbol;
                INamedTypeSymbol? eventType = current.TypeArguments[0] as INamedTypeSymbol;
                if (sagaState is null || eventType is null)
                {
                    return null;
                }

                return new(sagaState, typeSymbol, eventType);
            }

            current = current.BaseType;
        }

        return null;
    }

    private static SagaRegistrationInfo? TryGetSagaInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? sagaAttrSymbol,
        INamedTypeSymbol? sagaAttrGenericSymbol,
        INamedTypeSymbol sagaStateSymbol,
        string targetRootNamespace,
        List<Diagnostic> diagnostics
    )
    {
        if (sagaAttrSymbol is null && sagaAttrGenericSymbol is null)
        {
            return null;
        }

        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => MatchesSagaAttribute(a, sagaAttrSymbol, sagaAttrGenericSymbol));
        if (attr is null)
        {
            return null;
        }

        if (!typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sagaStateSymbol)))
        {
            Location? location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                                 typeSymbol.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(SagaStateInterfaceMissingDescriptor, location, typeSymbol.Name));
            return null;
        }

        if (!TryGetInputType(attr, out INamedTypeSymbol? inputTypeSymbol) || inputTypeSymbol is null)
        {
            Location? location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                                 typeSymbol.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(SagaMissingInputTypeDescriptor, location, typeSymbol.Name));
            return null;
        }

        INamedTypeSymbol inputType = inputTypeSymbol;
        string outputNamespace = NamingConventions.GetSiloRegistrationNamespace(
            typeSymbol.ContainingNamespace.ToDisplayString(),
            targetRootNamespace);
        return new(typeSymbol, inputType, outputNamespace);
    }

    private static StepInfoResult TryGetStepInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol sagaStepAttrSymbol,
        INamedTypeSymbol sagaStepInterfaceSymbol,
        INamedTypeSymbol sagaStateInterfaceSymbol,
        INamedTypeSymbol? compensatableInterfaceSymbol
    )
    {
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sagaStepAttrSymbol));
        if (attr is null)
        {
            return StepInfoResult.None;
        }

        Location? location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                             typeSymbol.Locations.FirstOrDefault();
        int stepIndex = attr.ConstructorArguments.Length > 0 ? (int)attr.ConstructorArguments[0].Value! : 0;
        if (stepIndex < 0)
        {
            return new(
                null,
                Diagnostic.Create(SagaStepInvalidOrderDescriptor, location, typeSymbol.Name, stepIndex),
                null);
        }

        INamedTypeSymbol? sagaStateSymbol = null;
        if (attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Saga").Value.Value is INamedTypeSymbol sagaType)
        {
            sagaStateSymbol = sagaType;
        }
        else
        {
            INamedTypeSymbol? sagaInterface = typeSymbol.AllInterfaces.FirstOrDefault(iface =>
                iface.OriginalDefinition is not null &&
                SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, sagaStepInterfaceSymbol));
            sagaStateSymbol = sagaInterface?.TypeArguments[0] as INamedTypeSymbol;
        }

        if (sagaStateSymbol is null)
        {
            return new(null, Diagnostic.Create(SagaStepMissingSagaDescriptor, location, typeSymbol.Name), null);
        }

        if (!sagaStateSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sagaStateInterfaceSymbol)))
        {
            return new(
                null,
                Diagnostic.Create(SagaStepSagaStateInvalidDescriptor, location, typeSymbol.Name, sagaStateSymbol.Name),
                sagaStateSymbol);
        }

        bool implementsStepInterface = typeSymbol.AllInterfaces.Any(i =>
            i.OriginalDefinition is not null &&
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, sagaStepInterfaceSymbol) &&
            SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], sagaStateSymbol));
        if (!implementsStepInterface)
        {
            return new(
                null,
                Diagnostic.Create(SagaStepMissingInterfaceDescriptor, location, typeSymbol.Name, sagaStateSymbol.Name),
                sagaStateSymbol);
        }

        bool hasCompensation = false;
        if (compensatableInterfaceSymbol is not null)
        {
            hasCompensation = typeSymbol.AllInterfaces.Any(i =>
                i.OriginalDefinition is not null &&
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, compensatableInterfaceSymbol));
        }

        return new(new(sagaStateSymbol, typeSymbol, stepIndex, hasCompensation, location), null, sagaStateSymbol);
    }

    private static void ValidateSteps(
        SagaRegistrationInfo saga,
        List<Diagnostic> diagnostics,
        bool hasStepAttributes
    )
    {
        if ((saga.Steps.Count == 0) && !hasStepAttributes)
        {
            Location? location = saga.SagaStateType.Locations.FirstOrDefault();
            diagnostics.Add(Diagnostic.Create(SagaMissingStepsDescriptor, location, saga.SagaName));
            return;
        }

        Dictionary<int, List<StepInfo>> stepGroups = saga.Steps.GroupBy(step => step.StepIndex)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (KeyValuePair<int, List<StepInfo>> group in stepGroups)
        {
            if (group.Value.Count > 1)
            {
                foreach (StepInfo step in group.Value)
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            SagaDuplicateStepDescriptor,
                            step.Location ?? step.StepType.Locations.FirstOrDefault(),
                            saga.SagaName,
                            group.Key));
                }
            }
        }

        int expectedIndex = 0;
        foreach (int stepIndex in saga.Steps.Select(step => step.StepIndex).OrderBy(stepIndex => stepIndex))
        {
            if (stepIndex != expectedIndex)
            {
                Location? location = saga.SagaStateType.Locations.FirstOrDefault();
                diagnostics.Add(
                    Diagnostic.Create(
                        SagaNonContiguousStepsDescriptor,
                        location,
                        saga.SagaName,
                        expectedIndex,
                        stepIndex));
                break;
            }

            expectedIndex++;
        }
    }

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider Options)> source =
            context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
        IncrementalValueProvider<SagaGenerationResult> sagasProvider = source.Select((
            pair,
            _
        ) =>
        {
            if (!HasRegistrationDependencies(pair.Compilation))
            {
                return new([], []);
            }

            string? rootNamespace = pair.Options.GlobalOptions.TryGetValue(
                TargetNamespaceResolver.RootNamespaceProperty,
                out string? rootNs)
                ? rootNs
                : null;
            string? assemblyName = pair.Options.GlobalOptions.TryGetValue(
                TargetNamespaceResolver.AssemblyNameProperty,
                out string? asmName)
                ? asmName
                : null;
            string targetRootNamespace = TargetNamespaceResolver.GetTargetRootNamespace(
                rootNamespace,
                assemblyName,
                pair.Compilation);
            return GetSagasWithDiagnostics(pair.Compilation, targetRootNamespace);
        });
        context.RegisterSourceOutput(
            sagasProvider,
            (
                spc,
                result
            ) =>
            {
                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                foreach (SagaRegistrationInfo saga in result.Sagas)
                {
                    string sourceText = GenerateRegistration(saga);
                    spc.AddSource($"{saga.SagaName}SagaRegistrations.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            });
    }

    private sealed class ReducerInfo
    {
        public ReducerInfo(
            INamedTypeSymbol sagaStateSymbol,
            INamedTypeSymbol reducerType,
            INamedTypeSymbol eventType
        )
        {
            SagaStateSymbol = sagaStateSymbol;
            ReducerType = reducerType;
            EventType = eventType;
            ReducerTypeName = reducerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            EventTypeName = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public INamedTypeSymbol EventType { get; }

        public string EventTypeName { get; }

        public INamedTypeSymbol ReducerType { get; }

        public string ReducerTypeName { get; }

        public INamedTypeSymbol SagaStateSymbol { get; }
    }

    private sealed class SagaGenerationResult
    {
        public SagaGenerationResult(
            List<SagaRegistrationInfo> sagas,
            List<Diagnostic> diagnostics
        )
        {
            Sagas = sagas;
            Diagnostics = diagnostics;
        }

        public List<Diagnostic> Diagnostics { get; }

        public List<SagaRegistrationInfo> Sagas { get; }
    }

    private sealed class SagaRegistrationInfo
    {
        public SagaRegistrationInfo(
            INamedTypeSymbol sagaStateType,
            INamedTypeSymbol inputType,
            string outputNamespace
        )
        {
            SagaStateType = sagaStateType;
            InputType = inputType;
            OutputNamespace = outputNamespace;
            SagaNamespace = sagaStateType.ContainingNamespace.ToDisplayString();
            SagaName = RemoveSagaSuffix(sagaStateType.Name);
            SagaStateTypeName = sagaStateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            InputTypeName = inputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            Steps = [];
            Reducers = [];
        }

        public INamedTypeSymbol InputType { get; }

        public string InputTypeName { get; }

        public string OutputNamespace { get; }

        public List<ReducerInfo> Reducers { get; }

        public string SagaName { get; }

        public string SagaNamespace { get; }

        public INamedTypeSymbol SagaStateType { get; }

        public string SagaStateTypeName { get; }

        public List<StepInfo> Steps { get; }

        private static string RemoveSagaSuffix(
            string typeName
        ) =>
            typeName.EndsWith("SagaState", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - "SagaState".Length)
                : typeName;
    }

    private sealed class StepInfo
    {
        public StepInfo(
            INamedTypeSymbol sagaStateSymbol,
            INamedTypeSymbol stepType,
            int stepIndex,
            bool hasCompensation,
            Location? location
        )
        {
            SagaStateSymbol = sagaStateSymbol;
            StepType = stepType;
            StepIndex = stepIndex;
            HasCompensation = hasCompensation;
            Location = location;
            StepTypeName = stepType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            StepName = stepType.Name;
        }

        public bool HasCompensation { get; }

        public Location? Location { get; }

        public INamedTypeSymbol SagaStateSymbol { get; }

        public int StepIndex { get; }

        public string StepName { get; }

        public INamedTypeSymbol StepType { get; }

        public string StepTypeName { get; }
    }

    private readonly struct StepInfoResult
    {
        public StepInfoResult(
            StepInfo? step,
            Diagnostic? diagnostic,
            INamedTypeSymbol? sagaStateSymbol
        )
        {
            Step = step;
            Diagnostic = diagnostic;
            SagaStateSymbol = sagaStateSymbol;
        }

        public StepInfo? Step { get; }

        public Diagnostic? Diagnostic { get; }

        public INamedTypeSymbol? SagaStateSymbol { get; }

        public static StepInfoResult None => new(null, null, null);
    }
}