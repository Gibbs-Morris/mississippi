using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Mississippi.EventSourcing.Aggregates.Generators.Models;


namespace Mississippi.EventSourcing.Aggregates.Generators;

/// <summary>
///     Incremental source generator for aggregate service interfaces and implementations.
/// </summary>
/// <remarks>
///     <para>
///         This generator scans for classes decorated with <c>[AggregateService]</c>
///         and discovers command handlers to generate typed service methods.
///     </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AggregateServiceGenerator : IIncrementalGenerator
{
    private const string AggregateServiceAttributeFullName =
        "Mississippi.EventSourcing.Aggregates.Abstractions.AggregateServiceAttribute";

    private const string CommandHandlerBaseFullName =
        "Mississippi.EventSourcing.Aggregates.Abstractions.CommandHandler";

    private static AggregateServiceInfo? ExtractAggregateInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        AttributeData? attribute = null;
        foreach (AttributeData attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == AggregateServiceAttributeFullName)
            {
                attribute = attr;
                break;
            }
        }

        if (attribute is null)
        {
            return null;
        }

        // Extract route from constructor argument
        string route = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : string.Empty;

        // Extract optional properties
        bool generateApi = false;
        string? authorize = null;
        foreach (KeyValuePair<string, TypedConstant> arg in attribute.NamedArguments)
        {
            switch (arg.Key)
            {
                case "GenerateApi":
                    generateApi = (bool)(arg.Value.Value ?? false);
                    break;
                case "Authorize":
                    authorize = arg.Value.Value?.ToString();
                    break;
            }
        }

        string typeName = typeSymbol.Name;
        string serviceName = typeName.EndsWith("Aggregate", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "Aggregate".Length)
            : typeName;

        // Check accessibility - if the aggregate is internal, services should be internal too
        bool isInternal = typeSymbol.DeclaredAccessibility != Accessibility.Public;
        return new()
        {
            FullTypeName = typeSymbol.ToDisplayString(),
            TypeName = typeName,
            ServiceName = serviceName,
            Namespace = typeSymbol.ContainingNamespace.ToDisplayString(),
            Route = route,
            GenerateApi = generateApi,
            Authorize = authorize,
            IsInternal = isInternal,
        };
    }

    private static CommandHandlerInfo? ExtractCommandHandlerInfo(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        if (context.Node is not ClassDeclarationSyntax classDecl)
        {
            return null;
        }

        SemanticModel semanticModel = context.SemanticModel;
        INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
        if (typeSymbol is null)
        {
            return null;
        }

        // Check if this type inherits from CommandHandler<TCommand, TAggregate>
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            string baseTypeName = baseType.OriginalDefinition.ToDisplayString();
            if (baseTypeName.StartsWith(CommandHandlerBaseFullName, StringComparison.Ordinal) &&
                (baseType.TypeArguments.Length == 2))
            {
                ITypeSymbol commandType = baseType.TypeArguments[0];
                ITypeSymbol aggregateType = baseType.TypeArguments[1];
                return new()
                {
                    CommandFullTypeName = commandType.ToDisplayString(),
                    CommandTypeName = commandType.Name,
                    CommandNamespace = commandType.ContainingNamespace.ToDisplayString(),
                    AggregateFullTypeName = aggregateType.ToDisplayString(),
                };
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static string GenerateApiController(
        AggregateServiceInfo aggregate
    )
    {
        string authorizeAttribute = aggregate.Authorize is not null
            ? $"\n    [Authorize(Policy = \"{aggregate.Authorize}\")]"
            : string.Empty;
        StringBuilder commandEndpoints = new();
        foreach (CommandInfo command in aggregate.Commands)
        {
            commandEndpoints.Append(
                $@"

    /// <summary>
    ///     Executes the {command.TypeName} command.
    /// </summary>
    /// <param name=""entityId"">The entity identifier.</param>
    /// <param name=""command"">The command to execute.</param>
    /// <param name=""cancellationToken"">A token to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    [HttpPost(""{command.MethodName.ToLowerInvariant()}"")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status400BadRequest)]
    public virtual Task<ActionResult<OperationResult>> {command.MethodName}Async(
        [FromRoute] string entityId,
        [FromBody] {command.FullTypeName} command,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(entityId, command, Service.{command.MethodName}Async, cancellationToken);");
        }

        return $@"// <auto-generated/>
// This file was generated by Mississippi.EventSourcing.Aggregates.Generators.
// Do not edit this file directly.
#nullable enable

using System;
using System.CodeDom.Compiler;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Api;

namespace {aggregate.Namespace};

/// <summary>
///     Generated API controller for <see cref=""{aggregate.TypeName}""/> aggregate.
/// </summary>
/// <remarks>
///     <para>
///         This controller was auto-generated from the <c>[AggregateService]</c> attribute.
///         It delegates command execution to <see cref=""I{aggregate.ServiceName}Service""/>.
///         To customize behavior, create a partial class with additional methods or
///         override the command endpoint methods.
///     </para>
/// </remarks>
[GeneratedCode(""Mississippi.EventSourcing.Aggregates.Generators"", ""1.0.0"")]
[Route(""api/aggregates/{aggregate.Route}/{{entityId}}"")]{authorizeAttribute}
[Produces(""application/json"")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public partial class {aggregate.ServiceName}Controller
    : AggregateControllerBase<{aggregate.FullTypeName}>
{{
    /// <summary>
    ///     Initializes a new instance of the <see cref=""{aggregate.ServiceName}Controller""/> class.
    /// </summary>
    /// <param name=""service"">The aggregate service.</param>
    /// <param name=""logger"">The logger instance.</param>
    public {aggregate.ServiceName}Controller(
        I{aggregate.ServiceName}Service service,
        ILogger<AggregateControllerBase<{aggregate.FullTypeName}>> logger)
        : base(logger)
    {{
        Service = service ?? throw new ArgumentNullException(nameof(service));
    }}

    /// <summary>
    ///     Gets the aggregate service for command execution.
    /// </summary>
    private I{aggregate.ServiceName}Service Service {{ get; }}{commandEndpoints}
}}
";
    }

    private static string GenerateServiceImplementation(
        AggregateServiceInfo aggregate
    )
    {
        string accessModifier = aggregate.IsInternal ? "internal" : "public";
        StringBuilder methods = new();
        foreach (CommandInfo command in aggregate.Commands)
        {
            methods.Append(
                $@"

    /// <inheritdoc />
    public Task<OperationResult> {command.MethodName}Async(
        string entityId,
        {command.FullTypeName} command,
        CancellationToken cancellationToken = default)
        => ExecuteCommandAsync(entityId, command, cancellationToken);");
        }

        return $@"// <auto-generated/>
// This file was generated by Mississippi.EventSourcing.Aggregates.Generators.
// Do not edit this file directly.
#nullable enable

using System;
using System.CodeDom.Compiler;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Api;

namespace {aggregate.Namespace};

/// <summary>
///     Generated service implementation for <see cref=""{aggregate.TypeName}""/> aggregate.
/// </summary>
/// <remarks>
///     <para>
///         This service provides strongly-typed methods for executing commands against
///         the <see cref=""{aggregate.TypeName}""/> aggregate. It inherits from
///         <see cref=""AggregateServiceBase{{TAggregate}}""/> which provides logging
///         and extensibility hooks.
///     </para>
/// </remarks>
[GeneratedCode(""Mississippi.EventSourcing.Aggregates.Generators"", ""1.0.0"")]
{accessModifier} sealed partial class {aggregate.ServiceName}Service
    : AggregateServiceBase<{aggregate.FullTypeName}>, I{aggregate.ServiceName}Service
{{
    /// <summary>
    ///     Initializes a new instance of the <see cref=""{aggregate.ServiceName}Service""/> class.
    /// </summary>
    /// <param name=""aggregateGrainFactory"">Factory for resolving aggregate grains.</param>
    /// <param name=""logger"">The logger for diagnostic output.</param>
    public {aggregate.ServiceName}Service(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<AggregateServiceBase<{aggregate.FullTypeName}>> logger)
        : base(aggregateGrainFactory, logger)
    {{
    }}{methods}
}}
";
    }

    private static string GenerateServiceInterface(
        AggregateServiceInfo aggregate
    )
    {
        string accessModifier = aggregate.IsInternal ? "internal" : "public";
        StringBuilder methods = new();
        foreach (CommandInfo command in aggregate.Commands)
        {
            methods.Append(
                $@"

    /// <summary>
    ///     Executes the <see cref=""{command.FullTypeName}""/> command.
    /// </summary>
    /// <param name=""entityId"">The entity identifier.</param>
    /// <param name=""command"">The command to execute.</param>
    /// <param name=""cancellationToken"">A token to cancel the operation.</param>
    /// <returns>An <see cref=""OperationResult""/> indicating success or failure.</returns>
    Task<OperationResult> {command.MethodName}Async(
        string entityId,
        {command.FullTypeName} command,
        CancellationToken cancellationToken = default);");
        }

        return $@"// <auto-generated/>
// This file was generated by Mississippi.EventSourcing.Aggregates.Generators.
// Do not edit this file directly.
#nullable enable

using System;
using System.CodeDom.Compiler;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

namespace {aggregate.Namespace};

/// <summary>
///     Generated service interface for <see cref=""{aggregate.TypeName}""/> aggregate.
/// </summary>
/// <remarks>
///     <para>
///         This interface was auto-generated from command handlers registered for
///         <see cref=""{aggregate.TypeName}""/>. To add custom methods, create a partial
///         interface or extend the service implementation.
///     </para>
/// </remarks>
[GeneratedCode(""Mississippi.EventSourcing.Aggregates.Generators"", ""1.0.0"")]
{accessModifier} partial interface I{aggregate.ServiceName}Service
{{{methods}
}}
";
    }

    private static string GetMethodName(
        string commandTypeName
    ) =>
        // Remove common verb prefixes that would be redundant in method name
        // e.g., "RegisterUser" -> "Register", "JoinChannel" -> "JoinChannel"
        // The method will be called "{MethodName}Async"
        commandTypeName;

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Filter for types with [AggregateService] attribute
        IncrementalValuesProvider<AggregateServiceInfo?> aggregates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AggregateServiceAttributeFullName,
                static (
                    node,
                    _
                ) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (
                    ctx,
                    ct
                ) => ExtractAggregateInfo(ctx, ct))
            .Where(static info => info is not null);

        // Find all command handler types in the compilation
        IncrementalValuesProvider<CommandHandlerInfo?> commandHandlers = context.SyntaxProvider.CreateSyntaxProvider(
                static (
                    node,
                    _
                ) => node is ClassDeclarationSyntax cds && (cds.BaseList != null),
                static (
                    ctx,
                    ct
                ) => ExtractCommandHandlerInfo(ctx, ct))
            .Where(static info => info is not null);

        // Collect all command handlers
        IncrementalValueProvider<ImmutableArray<CommandHandlerInfo?>> allHandlers = commandHandlers.Collect();

        // Combine aggregates with handlers
        IncrementalValuesProvider<(AggregateServiceInfo? Aggregate, ImmutableArray<CommandHandlerInfo?> Handlers)>
            combined = aggregates.Combine(allHandlers);

        // Generate service interface and implementation
        context.RegisterSourceOutput(
            combined,
            static (
                spc,
                tuple
            ) =>
            {
                AggregateServiceInfo? aggregate = tuple.Aggregate;
                if (aggregate is null)
                {
                    return;
                }

                ImmutableArray<CommandHandlerInfo?> handlers = tuple.Handlers;

                // Find commands for this aggregate
                foreach (CommandHandlerInfo? handler in handlers)
                {
                    if (handler is null)
                    {
                        continue;
                    }

                    if (handler.AggregateFullTypeName == aggregate.FullTypeName)
                    {
                        aggregate.Commands.Add(
                            new()
                            {
                                FullTypeName = handler.CommandFullTypeName,
                                TypeName = handler.CommandTypeName,
                                Namespace = handler.CommandNamespace,
                                MethodName = GetMethodName(handler.CommandTypeName),
                            });
                    }
                }

                if (aggregate.Commands.Count == 0)
                {
                    return;
                }

                // Generate interface
                string interfaceSource = GenerateServiceInterface(aggregate);
                spc.AddSource($"I{aggregate.ServiceName}Service.g.cs", interfaceSource);

                // Generate implementation
                string implementationSource = GenerateServiceImplementation(aggregate);
                spc.AddSource($"{aggregate.ServiceName}Service.g.cs", implementationSource);

                // Generate API controller if requested
                if (aggregate.GenerateApi && !string.IsNullOrEmpty(aggregate.Route))
                {
                    string controllerSource = GenerateApiController(aggregate);
                    spc.AddSource($"{aggregate.ServiceName}Controller.g.cs", controllerSource);
                }
            });
    }
}