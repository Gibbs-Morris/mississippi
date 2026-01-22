using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Mississippi.Sdk.Generators.Core.Analysis;
using Mississippi.Sdk.Generators.Core.Emit;
using Mississippi.Sdk.Generators.Core.Naming;


namespace Mississippi.Sdk.Client.Generators;

/// <summary>
///     Generates client-side aggregate reducer classes for commands marked with [GenerateCommand].
/// </summary>
/// <remarks>
///     <para>
///         This generator produces one static reducer class per aggregate containing
///         three reducer methods per command (Executing, Failed, Succeeded).
///         Each reducer delegates to <c>AggregateCommandStateReducers</c> helper methods.
///     </para>
///     <para>
///         Example: For commands in "Spring.Domain.Aggregates.BankAccount.Commands",
///         generates "BankAccountAggregateReducers" in "Spring.Client.Features.BankAccountAggregate.Reducers".
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class CommandClientReducersGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Sdk.Generators.Abstractions.GenerateCommandAttribute";

    /// <summary>
    ///     Recursively finds commands in a namespace.
    /// </summary>
    private static void FindCommandsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol generateAttrSymbol,
        List<CommandModel> commands
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            CommandModel? model = TryGetCommandModel(typeSymbol, generateAttrSymbol);
            if (model is not null)
            {
                commands.Add(model);
            }
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindCommandsInNamespace(childNs, generateAttrSymbol, commands);
        }
    }

    /// <summary>
    ///     Generates the three lifecycle reducers for a command.
    /// </summary>
    private static void GenerateCommandReducers(
        SourceBuilder sb,
        string commandName,
        string stateTypeName
    )
    {
        sb.AppendLine($"// {commandName} reducers");
        sb.AppendLine();

        // Executing
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Updates state when {commandName} command starts executing.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"state\">The current state.</param>");
        sb.AppendLine("/// <param name=\"action\">The action to reduce.</param>");
        sb.AppendLine("/// <returns>The new state with command tracked.</returns>");
        sb.AppendLine($"public static {stateTypeName} {commandName}Executing(");
        sb.IncreaseIndent();
        sb.AppendLine($"{stateTypeName} state,");
        sb.AppendLine($"{commandName}ExecutingAction action");
        sb.DecreaseIndent();
        sb.AppendLine(") =>");
        sb.IncreaseIndent();
        sb.AppendLine("AggregateCommandStateReducers.ReduceCommandExecuting(state, action);");
        sb.DecreaseIndent();
        sb.AppendLine();

        // Failed
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Updates state when {commandName} command fails.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"state\">The current state.</param>");
        sb.AppendLine("/// <param name=\"action\">The action containing error details.</param>");
        sb.AppendLine("/// <returns>The new state with error populated.</returns>");
        sb.AppendLine($"public static {stateTypeName} {commandName}Failed(");
        sb.IncreaseIndent();
        sb.AppendLine($"{stateTypeName} state,");
        sb.AppendLine($"{commandName}FailedAction action");
        sb.DecreaseIndent();
        sb.AppendLine(") =>");
        sb.IncreaseIndent();
        sb.AppendLine("AggregateCommandStateReducers.ReduceCommandFailed(state, action);");
        sb.DecreaseIndent();
        sb.AppendLine();

        // Succeeded
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"///     Updates state when {commandName} command succeeds.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"state\">The current state.</param>");
        sb.AppendLine("/// <param name=\"action\">The action to reduce.</param>");
        sb.AppendLine("/// <returns>The new state with success set.</returns>");
        sb.AppendLine($"public static {stateTypeName} {commandName}Succeeded(");
        sb.IncreaseIndent();
        sb.AppendLine($"{stateTypeName} state,");
        sb.AppendLine($"{commandName}SucceededAction action");
        sb.DecreaseIndent();
        sb.AppendLine(") =>");
        sb.IncreaseIndent();
        sb.AppendLine("AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);");
        sb.DecreaseIndent();
    }

    /// <summary>
    ///     Generates the reducers class for an aggregate.
    /// </summary>
    private static void GenerateReducers(
        SourceProductionContext context,
        AggregateInfo aggregate
    )
    {
        string reducersTypeName = aggregate.AggregateName + "AggregateReducers";
        string stateTypeName = aggregate.AggregateName + "AggregateState";
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Mississippi.Inlet.Blazor.WebAssembly.Abstractions");
        sb.AppendLine();
        sb.AppendLine($"using {aggregate.ActionsNamespace};");
        sb.AppendLine($"using {aggregate.StateNamespace};");
        sb.AppendFileScopedNamespace(aggregate.ReducersNamespace);
        sb.AppendLine();
        sb.AppendSummary($"Pure reducer functions for the {aggregate.AggregateName}Aggregate feature state.");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("///     <para>");
        sb.AppendLine(
            "///         Each command has three lifecycle reducers (Executing, Failed, Succeeded) that delegate");
        sb.AppendLine(
            "///         to <see cref=\"AggregateCommandStateReducers\" /> for the standard command tracking logic.");
        sb.AppendLine("///     </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendGeneratedCodeAttribute("CommandClientReducersGenerator");
        sb.AppendLine($"internal static class {reducersTypeName}");
        sb.OpenBrace();

        // Generate reducers for each command
        bool isFirst = true;
        foreach (string commandName in aggregate.CommandNames.OrderBy(n => n))
        {
            if (!isFirst)
            {
                sb.AppendLine();
            }

            isFirst = false;
            GenerateCommandReducers(sb, commandName, stateTypeName);
        }

        sb.CloseBrace();
        context.AddSource($"{reducersTypeName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    /// <summary>
    ///     Gets aggregates from command models by grouping on aggregate name.
    /// </summary>
    private static List<AggregateInfo> GetAggregatesFromCommands(
        List<CommandModel> commands
    )
    {
        Dictionary<string, AggregateInfo> aggregates = new();
        foreach (CommandModel command in commands)
        {
            string? aggregateName = NamingConventions.GetAggregateNameFromNamespace(command.Namespace);
            if (aggregateName is null)
            {
                continue;
            }

            if (!aggregates.TryGetValue(aggregateName, out AggregateInfo? aggregate))
            {
                string stateNamespace = NamingConventions.GetClientStateNamespace(command.Namespace);
                string reducersNamespace = NamingConventions.GetClientReducersNamespace(command.Namespace);
                string actionsNamespace = NamingConventions.GetClientActionsNamespace(command.Namespace);
                aggregate = new(aggregateName, stateNamespace, reducersNamespace, actionsNamespace);
                aggregates[aggregateName] = aggregate;
            }

            aggregate.CommandNames.Add(command.TypeName);
        }

        return aggregates.Values.ToList();
    }

    /// <summary>
    ///     Gets command models from the compilation.
    /// </summary>
    private static List<CommandModel> GetCommandsFromCompilation(
        Compilation compilation
    )
    {
        List<CommandModel> commands = new();
        INamedTypeSymbol? generateAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        if (generateAttrSymbol is null)
        {
            return commands;
        }

        foreach (IAssemblySymbol referencedAssembly in GetReferencedAssemblies(compilation))
        {
            FindCommandsInNamespace(referencedAssembly.GlobalNamespace, generateAttrSymbol, commands);
        }

        return commands;
    }

    /// <summary>
    ///     Gets all referenced assemblies from the compilation.
    /// </summary>
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
    ///     Tries to get command model from a type symbol.
    /// </summary>
    private static CommandModel? TryGetCommandModel(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol generateAttrSymbol
    )
    {
        AttributeData? attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generateAttrSymbol));
        if (attr is null)
        {
            return null;
        }

        string? route = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Route").Value.Value?.ToString();
        if (string.IsNullOrEmpty(route))
        {
            route = NamingConventions.ToKebabCase(typeSymbol.Name);
        }

        string httpMethod =
            attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "HttpMethod").Value.Value?.ToString() ?? "POST";
        return new(typeSymbol, route!, httpMethod);
    }

    /// <summary>
    ///     Initializes the generator pipeline.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        IncrementalValueProvider<List<AggregateInfo>> aggregatesProvider = context.CompilationProvider.Select((
            compilation,
            _
        ) =>
        {
            List<CommandModel> commands = GetCommandsFromCompilation(compilation);
            return GetAggregatesFromCommands(commands);
        });
        context.RegisterSourceOutput(
            aggregatesProvider,
            static (
                spc,
                aggregates
            ) =>
            {
                foreach (AggregateInfo aggregate in aggregates)
                {
                    GenerateReducers(spc, aggregate);
                }
            });
    }

    /// <summary>
    ///     Information about an aggregate derived from command namespaces.
    /// </summary>
    private sealed class AggregateInfo
    {
        public AggregateInfo(
            string aggregateName,
            string stateNamespace,
            string reducersNamespace,
            string actionsNamespace
        )
        {
            AggregateName = aggregateName;
            StateNamespace = stateNamespace;
            ReducersNamespace = reducersNamespace;
            ActionsNamespace = actionsNamespace;
            CommandNames = new();
        }

        public string ActionsNamespace { get; }

        public string AggregateName { get; }

        public List<string> CommandNames { get; }

        public string ReducersNamespace { get; }

        public string StateNamespace { get; }
    }
}