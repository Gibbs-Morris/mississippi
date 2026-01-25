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
///     Generates silo-side service registrations for aggregates marked with [GenerateAggregateEndpoints].
/// </summary>
/// <remarks>
///     <para>This generator produces:</para>
///     <list type="bullet">
///         <item>
///             Add{AggregateName}() extension method that registers event types, command handlers, reducers,
///             event effects, and snapshot converters.
///         </item>
///     </list>
///     <para>
///         The generator scans both the current compilation and referenced assemblies
///         to find aggregate types decorated with [GenerateAggregateEndpoints] and their
///         associated handlers, reducers, effects, and events in sibling namespaces.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AggregateSiloRegistrationGenerator : IIncrementalGenerator
{
    private const string CommandHandlerBaseFullName =
        "Mississippi.EventSourcing.Aggregates.Abstractions.CommandHandlerBase`2";

    private const string EventEffectBaseFullName =
        "Mississippi.EventSourcing.Aggregates.Abstractions.EventEffectBase`2";

    private const string EventReducerBaseFullName =
        "Mississippi.EventSourcing.Reducers.Abstractions.EventReducerBase`2";

    private const string GenerateAggregateEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpointsAttribute";

    private const string SimpleEventEffectBaseFullName =
        "Mississippi.EventSourcing.Aggregates.Abstractions.SimpleEventEffectBase`2";

    /// <summary>
    ///     Recursively finds aggregates in a namespace.
    /// </summary>
    private static void FindAggregatesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol? handlerBaseSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        INamedTypeSymbol? effectBaseSymbol,
        INamedTypeSymbol? simpleEffectBaseSymbol,
        List<AggregateRegistrationInfo> aggregates,
        string targetRootNamespace
    )
    {
        // Check types in this namespace
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            AggregateRegistrationInfo? info = TryGetAggregateInfo(
                typeSymbol,
                aggregateAttrSymbol,
                handlerBaseSymbol,
                reducerBaseSymbol,
                effectBaseSymbol,
                simpleEffectBaseSymbol,
                targetRootNamespace);
            if (info is not null)
            {
                aggregates.Add(info);
            }
        }

        // Recurse into nested namespaces
        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatesInNamespace(
                childNs,
                aggregateAttrSymbol,
                handlerBaseSymbol,
                reducerBaseSymbol,
                effectBaseSymbol,
                simpleEffectBaseSymbol,
                aggregates,
                targetRootNamespace);
        }
    }

    /// <summary>
    ///     Finds event effects for an aggregate in the Effects sub-namespace.
    /// </summary>
    private static List<EffectInfo> FindEffectsForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol aggregateSymbol,
        INamedTypeSymbol? effectBaseSymbol,
        INamedTypeSymbol? simpleEffectBaseSymbol
    )
    {
        List<EffectInfo> effects = [];
        if (effectBaseSymbol is null && simpleEffectBaseSymbol is null)
        {
            return effects;
        }

        // Look for Effects sub-namespace
        INamespaceSymbol? effectsNs = aggregateNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Effects");
        if (effectsNs is null)
        {
            return effects;
        }

        // Find all types that extend EventEffectBase<TEvent, TAggregate> or SimpleEventEffectBase<TEvent, TAggregate>
        foreach (INamedTypeSymbol typeSymbol in effectsNs.GetTypeMembers())
        {
            EffectInfo? effectInfo = TryCreateEffectInfo(typeSymbol, aggregateSymbol);
            if (effectInfo is not null)
            {
                effects.Add(effectInfo);
            }
        }

        return effects;
    }

    /// <summary>
    ///     Finds command handlers for an aggregate in the Handlers sub-namespace.
    /// </summary>
    private static List<HandlerInfo> FindHandlersForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol aggregateSymbol,
        INamedTypeSymbol? handlerBaseSymbol
    )
    {
        List<HandlerInfo> handlers = [];
        if (handlerBaseSymbol is null)
        {
            return handlers;
        }

        // Look for Handlers sub-namespace
        INamespaceSymbol? handlersNs = aggregateNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Handlers");
        if (handlersNs is null)
        {
            return handlers;
        }

        // Find all types that extend CommandHandlerBase<TCommand, TAggregate>
        foreach (INamedTypeSymbol typeSymbol in handlersNs.GetTypeMembers())
        {
            INamedTypeSymbol? baseType = typeSymbol.BaseType;
            if (baseType is null || !baseType.IsGenericType)
            {
                continue;
            }

            // Check if it extends CommandHandlerBase<,>
            INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
            if (constructedFrom is null ||
                (constructedFrom.MetadataName != "CommandHandlerBase`2") ||
                (constructedFrom.ContainingNamespace.ToDisplayString() !=
                 "Mississippi.EventSourcing.Aggregates.Abstractions"))
            {
                continue;
            }

            // Verify the second type argument is our aggregate
            if ((baseType.TypeArguments.Length != 2) ||
                !SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], aggregateSymbol))
            {
                continue;
            }

            // Extract command type
            ITypeSymbol commandType = baseType.TypeArguments[0];
            handlers.Add(
                new(typeSymbol.ToDisplayString(), typeSymbol.Name, commandType.ToDisplayString(), commandType.Name));
        }

        return handlers;
    }

    /// <summary>
    ///     Finds reducers for an aggregate in the Reducers sub-namespace.
    /// </summary>
    private static List<ReducerInfo> FindReducersForAggregate(
        INamespaceSymbol aggregateNamespace,
        INamedTypeSymbol aggregateSymbol,
        INamedTypeSymbol? reducerBaseSymbol
    )
    {
        List<ReducerInfo> reducers = [];
        if (reducerBaseSymbol is null)
        {
            return reducers;
        }

        // Look for Reducers sub-namespace
        INamespaceSymbol? reducersNs = aggregateNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.Name == "Reducers");
        if (reducersNs is null)
        {
            return reducers;
        }

        // Find all types that extend EventReducerBase<TEvent, TAggregate>
        foreach (INamedTypeSymbol typeSymbol in reducersNs.GetTypeMembers())
        {
            INamedTypeSymbol? baseType = typeSymbol.BaseType;
            if (baseType is null || !baseType.IsGenericType)
            {
                continue;
            }

            // Check if it extends EventReducerBase<,>
            INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
            if (constructedFrom is null ||
                (constructedFrom.MetadataName != "EventReducerBase`2") ||
                (constructedFrom.ContainingNamespace.ToDisplayString() !=
                 "Mississippi.EventSourcing.Reducers.Abstractions"))
            {
                continue;
            }

            // Verify the second type argument is our aggregate
            if ((baseType.TypeArguments.Length != 2) ||
                !SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], aggregateSymbol))
            {
                continue;
            }

            // Extract event type
            ITypeSymbol eventType = baseType.TypeArguments[0];
            reducers.Add(
                new(typeSymbol.ToDisplayString(), typeSymbol.Name, eventType.ToDisplayString(), eventType.Name));
        }

        return reducers;
    }

    /// <summary>
    ///     Generates the registration extension method for an aggregate.
    /// </summary>
    private static string GenerateRegistration(
        AggregateRegistrationInfo aggregate
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendUsing("Mississippi.EventSourcing.Aggregates");
        sb.AppendUsing("Mississippi.EventSourcing.Reducers");
        sb.AppendUsing("Mississippi.EventSourcing.Snapshots");

        // Add using for aggregate namespace
        sb.AppendUsing(aggregate.Model.Namespace);

        // Add using for commands namespace
        string commandsNamespace = aggregate.Model.Namespace + ".Commands";
        sb.AppendUsing(commandsNamespace);

        // Add using for events namespace
        string eventsNamespace = aggregate.Model.Namespace + ".Events";
        sb.AppendUsing(eventsNamespace);

        // Add using for handlers namespace
        string handlersNamespace = aggregate.Model.Namespace + ".Handlers";
        sb.AppendUsing(handlersNamespace);

        // Add using for reducers namespace
        string reducersNamespace = aggregate.Model.Namespace + ".Reducers";
        sb.AppendUsing(reducersNamespace);

        // Add using for effects namespace if there are effects
        if (aggregate.Effects.Count > 0)
        {
            string effectsNamespace = aggregate.Model.Namespace + ".Effects";
            sb.AppendUsing(effectsNamespace);
        }

        sb.AppendFileScopedNamespace(aggregate.OutputNamespace);
        sb.AppendLine();
        string registrationsName = $"{aggregate.Model.AggregateName}AggregateRegistrations";
        sb.AppendSummary($"Extension methods for registering {aggregate.Model.AggregateName} aggregate services.");
        sb.AppendGeneratedCodeAttribute("AggregateSiloRegistrationGenerator");
        sb.AppendLine($"public static class {registrationsName}");
        sb.OpenBrace();
        sb.AppendSummary($"Adds the {aggregate.Model.AggregateName} aggregate services to the service collection.");
        sb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("/// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"public static IServiceCollection Add{aggregate.Model.AggregateName}Aggregate(");
        sb.IncreaseIndent();
        sb.AppendLine("this IServiceCollection services");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();

        // Add aggregate infrastructure
        sb.AppendLine("// Add aggregate infrastructure");
        sb.AppendLine("services.AddAggregateSupport();");
        sb.AppendLine();

        // Register event types for hydration
        sb.AppendLine("// Register event types for hydration");
        HashSet<string> seenEventTypes = new();
        foreach (ReducerInfo reducer in aggregate.Reducers.Where(r => seenEventTypes.Add(r.EventTypeName)))
        {
            sb.AppendLine($"services.AddEventType<{reducer.EventTypeName}>();");
        }

        sb.AppendLine();

        // Register command handlers
        sb.AppendLine("// Register command handlers");
        foreach (HandlerInfo handler in aggregate.Handlers)
        {
            sb.AppendLine(
                $"services.AddCommandHandler<{handler.CommandTypeName}, {aggregate.Model.TypeName}, {handler.TypeName}>();");
        }

        sb.AppendLine();

        // Register reducers for state computation
        sb.AppendLine("// Register reducers for state computation");
        foreach (ReducerInfo reducer in aggregate.Reducers)
        {
            sb.AppendLine(
                $"services.AddReducer<{reducer.EventTypeName}, {aggregate.Model.TypeName}, {reducer.TypeName}>();");
        }

        sb.AppendLine();

        // Register event effects if present
        if (aggregate.Effects.Count > 0)
        {
            sb.AppendLine("// Register event effects");
            foreach (EffectInfo effect in aggregate.Effects)
            {
                sb.AppendLine($"services.AddEventEffect<{effect.TypeName}, {aggregate.Model.TypeName}>();");
            }

            sb.AppendLine();
        }

        // Add snapshot state converter
        sb.AppendLine("// Add snapshot state converter for aggregate snapshots");
        sb.AppendLine($"services.AddSnapshotStateConverter<{aggregate.Model.TypeName}>();");
        sb.AppendLine("return services;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    /// <summary>
    ///     Gets aggregate information from the compilation, including referenced assemblies.
    /// </summary>
    private static List<AggregateRegistrationInfo> GetAggregatesFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        List<AggregateRegistrationInfo> aggregates = [];

        // Get the attribute symbols
        INamedTypeSymbol? aggregateAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateAggregateEndpointsAttributeFullName);
        if (aggregateAttrSymbol is null)
        {
            return aggregates;
        }

        // Get base type symbols for handler, reducer, and effect detection
        INamedTypeSymbol? handlerBaseSymbol = compilation.GetTypeByMetadataName(CommandHandlerBaseFullName);
        INamedTypeSymbol? reducerBaseSymbol = compilation.GetTypeByMetadataName(EventReducerBaseFullName);
        INamedTypeSymbol? effectBaseSymbol = compilation.GetTypeByMetadataName(EventEffectBaseFullName);
        INamedTypeSymbol? simpleEffectBaseSymbol = compilation.GetTypeByMetadataName(SimpleEventEffectBaseFullName);

        // Scan all assemblies referenced by this compilation
        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindAggregatesInNamespace(
                referencedAssembly.GlobalNamespace,
                aggregateAttrSymbol,
                handlerBaseSymbol,
                reducerBaseSymbol,
                effectBaseSymbol,
                simpleEffectBaseSymbol,
                aggregates,
                targetRootNamespace);
        }

        return aggregates;
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
    ///     Determines if the given base type is an event effect base class.
    /// </summary>
    private static bool IsEventEffectBaseType(
        INamedTypeSymbol baseType
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
        if (ns != "Mississippi.EventSourcing.Aggregates.Abstractions")
        {
            return false;
        }

        return (constructedFrom.MetadataName == "EventEffectBase`2") ||
               (constructedFrom.MetadataName == "SimpleEventEffectBase`2");
    }

    /// <summary>
    ///     Attempts to create an EffectInfo from a type symbol if it's a valid event effect.
    /// </summary>
    private static EffectInfo? TryCreateEffectInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol aggregateSymbol
    )
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        if (baseType is null || !IsEventEffectBaseType(baseType))
        {
            return null;
        }

        // Verify the second type argument is our aggregate
        if ((baseType.TypeArguments.Length != 2) ||
            !SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], aggregateSymbol))
        {
            return null;
        }

        // Extract event type
        ITypeSymbol eventType = baseType.TypeArguments[0];
        return new(typeSymbol.ToDisplayString(), typeSymbol.Name, eventType.ToDisplayString(), eventType.Name);
    }

    /// <summary>
    ///     Tries to get aggregate info from a type symbol.
    /// </summary>
    private static AggregateRegistrationInfo? TryGetAggregateInfo(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol aggregateAttrSymbol,
        INamedTypeSymbol? handlerBaseSymbol,
        INamedTypeSymbol? reducerBaseSymbol,
        INamedTypeSymbol? effectBaseSymbol,
        INamedTypeSymbol? simpleEffectBaseSymbol,
        string targetRootNamespace
    )
    {
        // Check for [GenerateAggregateEndpoints] attribute
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, aggregateAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        // Get RoutePrefix from named argument, fallback to kebab-case of type name
        string? routePrefix = attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "RoutePrefix")
            .Value.Value?.ToString();

        // Remove "Aggregate" suffix for route and feature key
        string baseName = typeSymbol.Name.EndsWith("Aggregate", StringComparison.Ordinal)
            ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Aggregate".Length)
            : typeSymbol.Name;
        if (string.IsNullOrEmpty(routePrefix))
        {
            routePrefix = NamingConventions.ToKebabCase(baseName);
        }

        // Get FeatureKey from named argument, fallback to camelCase
        string featureKey =
            attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "FeatureKey").Value.Value?.ToString() ??
            NamingConventions.ToCamelCase(baseName);

        // Build aggregate model
        AggregateModel model = new(typeSymbol, routePrefix!, featureKey);

        // Find handlers, reducers, and effects
        INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
        List<HandlerInfo> handlers = containingNs is not null
            ? FindHandlersForAggregate(containingNs, typeSymbol, handlerBaseSymbol)
            : [];
        List<ReducerInfo> reducers = containingNs is not null
            ? FindReducersForAggregate(containingNs, typeSymbol, reducerBaseSymbol)
            : [];
        List<EffectInfo> effects = containingNs is not null
            ? FindEffectsForAggregate(containingNs, typeSymbol, effectBaseSymbol, simpleEffectBaseSymbol)
            : [];

        // Only generate if there are handlers or reducers
        if ((handlers.Count == 0) && (reducers.Count == 0))
        {
            return null;
        }

        // Output namespace: {DomainRoot}.Silo.Registrations based on aggregate namespace
        string outputNamespace = NamingConventions.GetSiloRegistrationNamespace(model.Namespace, targetRootNamespace);
        return new(model, handlers, reducers, effects, outputNamespace);
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
        IncrementalValueProvider<List<AggregateRegistrationInfo>> aggregatesProvider = compilationAndOptions.Select((
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
            return GetAggregatesFromCompilation(source.Compilation, targetRootNamespace);
        });

        // Register source output
        context.RegisterSourceOutput(
            aggregatesProvider,
            static (
                spc,
                aggregates
            ) =>
            {
                foreach (AggregateRegistrationInfo aggregate in aggregates)
                {
                    string registrationSource = GenerateRegistration(aggregate);
                    spc.AddSource(
                        $"{aggregate.Model.AggregateName}AggregateRegistrations.g.cs",
                        SourceText.From(registrationSource, Encoding.UTF8));
                }
            });
    }

    /// <summary>
    ///     Information about an aggregate type with its handlers, reducers, and effects.
    /// </summary>
    private sealed class AggregateRegistrationInfo
    {
        public AggregateRegistrationInfo(
            AggregateModel model,
            List<HandlerInfo> handlers,
            List<ReducerInfo> reducers,
            List<EffectInfo> effects,
            string outputNamespace
        )
        {
            Model = model;
            Handlers = handlers;
            Reducers = reducers;
            Effects = effects;
            OutputNamespace = outputNamespace;
        }

        public List<EffectInfo> Effects { get; }

        public List<HandlerInfo> Handlers { get; }

        public AggregateModel Model { get; }

        public string OutputNamespace { get; }

        public List<ReducerInfo> Reducers { get; }
    }

    /// <summary>
    ///     Information about an event effect.
    /// </summary>
    private sealed class EffectInfo
    {
        public EffectInfo(
            string fullTypeName,
            string typeName,
            string eventFullTypeName,
            string eventTypeName
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            EventFullTypeName = eventFullTypeName;
            EventTypeName = eventTypeName;
        }

        public string EventFullTypeName { get; }

        public string EventTypeName { get; }

        public string FullTypeName { get; }

        public string TypeName { get; }
    }

    /// <summary>
    ///     Information about a command handler.
    /// </summary>
    private sealed class HandlerInfo
    {
        public HandlerInfo(
            string fullTypeName,
            string typeName,
            string commandFullTypeName,
            string commandTypeName
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            CommandFullTypeName = commandFullTypeName;
            CommandTypeName = commandTypeName;
        }

        public string CommandFullTypeName { get; }

        public string CommandTypeName { get; }

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
            string eventTypeName
        )
        {
            FullTypeName = fullTypeName;
            TypeName = typeName;
            EventFullTypeName = eventFullTypeName;
            EventTypeName = eventTypeName;
        }

        public string EventFullTypeName { get; }

        public string EventTypeName { get; }

        public string FullTypeName { get; }

        public string TypeName { get; }
    }
}