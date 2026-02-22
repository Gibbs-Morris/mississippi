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


namespace Mississippi.Inlet.Gateway.Generators;

/// <summary>
///     Generates MCP tool registration extension methods that wire all generated MCP tools.
/// </summary>
/// <remarks>
///     <para>
///         This generator produces a registration class with a <c>WithGeneratedMcpTools()</c> extension method
///         on <c>IMcpServerBuilder</c> that chains <c>.WithTools&lt;T&gt;()</c> for each generated tools class.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class McpToolRegistrationGenerator : IIncrementalGenerator
{
    private const string GenerateCommandAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateCommandAttribute";

    private const string GenerateMcpReadToolAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpReadToolAttribute";

    private const string GenerateMcpSagaToolsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpSagaToolsAttribute";

    private const string GenerateMcpToolsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateMcpToolsAttribute";

    private static void FindAggregateToolsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol mcpToolsAttrSymbol,
        INamedTypeSymbol commandAttrSymbol,
        List<string> toolsClassNames
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            bool hasMcpToolsAttr = typeSymbol.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpToolsAttrSymbol));
            if (!hasMcpToolsAttr)
            {
                continue;
            }

            // Verify it has commands
            INamespaceSymbol? containingNs = typeSymbol.ContainingNamespace;
            if (containingNs is null)
            {
                continue;
            }

            INamespaceSymbol? commandsNs = containingNs.GetNamespaceMembers()
                .FirstOrDefault(ns => ns.Name == "Commands");
            if (commandsNs is null)
            {
                continue;
            }

            bool hasCommands = commandsNs.GetTypeMembers()
                .Any(t => t.GetAttributes()
                    .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, commandAttrSymbol)));
            if (!hasCommands)
            {
                continue;
            }

            string baseName = typeSymbol.Name.EndsWith("Aggregate", StringComparison.Ordinal)
                ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Aggregate".Length)
                : typeSymbol.Name;
            toolsClassNames.Add(baseName + "McpTools");
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregateToolsInNamespace(childNs, mcpToolsAttrSymbol, commandAttrSymbol, toolsClassNames);
        }
    }

    private static void FindProjectionToolsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol mcpReadToolAttrSymbol,
        List<string> toolsClassNames
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            bool hasMcpReadToolAttr = typeSymbol.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpReadToolAttrSymbol));
            if (!hasMcpReadToolAttr)
            {
                continue;
            }

            string baseName = typeSymbol.Name.EndsWith("Projection", StringComparison.Ordinal)
                ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "Projection".Length)
                : typeSymbol.Name;
            toolsClassNames.Add(baseName + "McpTools");
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindProjectionToolsInNamespace(childNs, mcpReadToolAttrSymbol, toolsClassNames);
        }
    }

    private static void FindSagaToolsInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol mcpSagaToolsAttrSymbol,
        List<string> toolsClassNames
    )
    {
        foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            bool hasMcpSagaToolsAttr = typeSymbol.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mcpSagaToolsAttrSymbol));
            if (!hasMcpSagaToolsAttr)
            {
                continue;
            }

            string baseName = typeSymbol.Name.EndsWith("SagaState", StringComparison.Ordinal)
                ? typeSymbol.Name.Substring(0, typeSymbol.Name.Length - "SagaState".Length)
                : typeSymbol.Name;
            toolsClassNames.Add(baseName + "SagaMcpTools");
        }

        foreach (INamespaceSymbol childNs in namespaceSymbol.GetNamespaceMembers())
        {
            FindSagaToolsInNamespace(childNs, mcpSagaToolsAttrSymbol, toolsClassNames);
        }
    }

    private static string GenerateRegistration(
        McpRegistrationModel model
    )
    {
        SourceBuilder sb = new();
        sb.AppendAutoGeneratedHeader();
        sb.AppendUsing("Microsoft.Extensions.DependencyInjection");
        sb.AppendFileScopedNamespace(model.OutputNamespace);
        sb.AppendLine();
        sb.AppendSummary("Provides registration extensions for generated MCP tools.");
        sb.AppendGeneratedCodeAttribute("McpToolRegistrationGenerator");
        sb.AppendLine("public static class McpToolRegistrations");
        sb.OpenBrace();
        sb.AppendSummary("Registers all generated MCP tools with the MCP server builder.");
        sb.AppendLine("/// <param name=\"builder\">The MCP server builder.</param>");
        sb.AppendLine("/// <returns>The builder for chaining.</returns>");
        sb.AppendLine("public static IMcpServerBuilder WithGeneratedMcpTools(");
        sb.IncreaseIndent();
        sb.AppendLine("this IMcpServerBuilder builder");
        sb.DecreaseIndent();
        sb.AppendLine(")");
        sb.OpenBrace();
        foreach (string toolsClassName in model.ToolsClassNames)
        {
            sb.AppendLine($"builder.WithTools<{toolsClassName}>();");
        }

        sb.AppendLine("return builder;");
        sb.CloseBrace();
        sb.CloseBrace();
        return sb.ToString();
    }

    private static McpRegistrationModel? GetRegistrationModel(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        INamedTypeSymbol? mcpToolsAttrSymbol = compilation.GetTypeByMetadataName(GenerateMcpToolsAttributeFullName);
        INamedTypeSymbol? commandAttrSymbol = compilation.GetTypeByMetadataName(GenerateCommandAttributeFullName);
        INamedTypeSymbol? mcpReadToolAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpReadToolAttributeFullName);
        List<string> toolsClassNames = [];
        if (mcpToolsAttrSymbol is not null && commandAttrSymbol is not null)
        {
            foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
            {
                FindAggregateToolsInNamespace(
                    assembly.GlobalNamespace,
                    mcpToolsAttrSymbol,
                    commandAttrSymbol,
                    toolsClassNames);
            }
        }

        if (mcpReadToolAttrSymbol is not null)
        {
            foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
            {
                FindProjectionToolsInNamespace(assembly.GlobalNamespace, mcpReadToolAttrSymbol, toolsClassNames);
            }
        }

        INamedTypeSymbol? mcpSagaToolsAttrSymbol =
            compilation.GetTypeByMetadataName(GenerateMcpSagaToolsAttributeFullName);
        if (mcpSagaToolsAttrSymbol is not null)
        {
            foreach (IAssemblySymbol assembly in GeneratorSymbolAnalysis.GetReferencedAssemblies(compilation))
            {
                FindSagaToolsInNamespace(assembly.GlobalNamespace, mcpSagaToolsAttrSymbol, toolsClassNames);
            }
        }

        if (toolsClassNames.Count == 0)
        {
            return null;
        }

        toolsClassNames.Sort(StringComparer.Ordinal);
        string outputNamespace = targetRootNamespace + ".McpTools";
        return new(outputNamespace, toolsClassNames);
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
        IncrementalValueProvider<McpRegistrationModel?> registrationProvider = compilationAndOptions.Select((
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
            return GetRegistrationModel(source.Compilation, targetRootNamespace);
        });
        context.RegisterSourceOutput(
            registrationProvider,
            static (
                spc,
                model
            ) =>
            {
                if (model is null)
                {
                    return;
                }

                string registrationSource = GenerateRegistration(model);
                spc.AddSource("McpToolRegistrations.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
            });
    }

    /// <summary>
    ///     Holds metadata for generating MCP tool registration extensions.
    /// </summary>
    private sealed class McpRegistrationModel
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="McpRegistrationModel" /> class.
        /// </summary>
        /// <param name="outputNamespace">The target output namespace for the registration class.</param>
        /// <param name="toolsClassNames">The names of the generated tool classes to register.</param>
        public McpRegistrationModel(
            string outputNamespace,
            List<string> toolsClassNames
        )
        {
            OutputNamespace = outputNamespace;
            ToolsClassNames = toolsClassNames;
        }

        /// <summary>
        ///     Gets the target output namespace for the registration class.
        /// </summary>
        public string OutputNamespace { get; }

        /// <summary>
        ///     Gets the names of the generated tool classes to register.
        /// </summary>
        public List<string> ToolsClassNames { get; }
    }
}