using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Sdk.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateSiloRegistrationGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Silo Generators")]
[AllureSubSuite("Aggregate Silo Registration Generator")]
public class AggregateSiloRegistrationGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeAndBaseStubs = """
                                                 namespace Mississippi.Sdk.Generators.Abstractions
                                                 {
                                                     using System;

                                                     [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                                     public sealed class GenerateAggregateEndpointsAttribute : Attribute
                                                     {
                                                         public string? FeatureKey { get; set; }
                                                         public string? RoutePrefix { get; set; }
                                                     }
                                                 }

                                                 namespace Mississippi.EventSourcing.Aggregates.Abstractions
                                                 {
                                                     public abstract class CommandHandlerBase<TCommand, TAggregate>
                                                         where TAggregate : class
                                                     {
                                                     }
                                                 }

                                                 namespace Mississippi.EventSourcing.Reducers.Abstractions
                                                 {
                                                     public abstract class EventReducerBase<TEvent, TAggregate>
                                                         where TAggregate : class
                                                     {
                                                     }
                                                 }
                                                 """;

    /// <summary>
    ///     Creates a Roslyn compilation from the provided source code and runs the generator.
    /// </summary>
    private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult
        RunResult) RunGenerator(
            params string[] sources
        )
    {
        SyntaxTree[] syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        // Get all framework references needed for compilation
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];

        // Add netstandard if available (for compatibility)
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        AggregateSiloRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated class should have correct extension method name.
    /// </summary>
    [Fact]
    public void GeneratedClassHasCorrectExtensionMethodName()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public static IServiceCollection AddOrderAggregate(", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFileHasAutoGeneratedHeader()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have correct namespace transformation.
    /// </summary>
    [Fact]
    public void GeneratedFileHasCorrectNamespace()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // Domain.Aggregates.* → Silo.Registrations
        Assert.Contains("namespace TestApp.Silo.Registrations;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFileHasGeneratedCodeAttribute()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedFileHasXmlDocumentation()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/// <summary>", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "Extension methods for registering Order aggregate services.",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should add snapshot state converter.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsAddsSnapshotStateConverter()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "services.AddSnapshotStateConverter<OrderAggregate>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should call AddAggregateSupport.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsCallsAddAggregateSupport()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("services.AddAggregateSupport();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsFileHasCorrectName()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        Assert.Contains(
            "OrderAggregateRegistrations.g.cs",
            runResult.GeneratedTrees[0].FilePath,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should include using statements for namespaces.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsIncludesUsingStatements()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Aggregates;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Reducers;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Snapshots;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should register command handlers.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsRegistersCommandHandlers()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "services.AddCommandHandler<CreateOrder, OrderAggregate, CreateOrderHandler>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should register event types.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsRegistersEventTypes()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("services.AddEventType<OrderCreated>();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should register reducers.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsRegistersReducers()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "services.AddReducer<OrderCreated, OrderAggregate, OrderCreatedReducer>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should return services for chaining.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsReturnsServicesForChaining()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("return services;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when aggregate has no handlers or reducers.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenAggregateHasNoHandlersOrReducers()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce no output when no aggregates are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoAggregates()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public class RegularClass
                                  {
                                      public string Name { get; set; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce output when aggregate has handlers.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWhenAggregateHasHandlers()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        Assert.Single(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Multiple aggregates should generate separate registration files.
    /// </summary>
    [Fact]
    public void MultipleAggregatesGenerateSeparateRegistrationFiles()
    {
        const string aggregateSource = """
                                       using Mississippi.Sdk.Generators.Abstractions;
                                       using Mississippi.EventSourcing.Aggregates.Abstractions;
                                       using Mississippi.EventSourcing.Reducers.Abstractions;

                                       namespace TestApp.Domain.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Commands
                                       {
                                           public sealed record CreateOrder { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Events
                                       {
                                           public sealed record OrderCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Handlers
                                       {
                                           public class CreateOrderHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Order.Commands.CreateOrder, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Order.Reducers
                                       {
                                           public class OrderCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Order.Events.OrderCreated, TestApp.Domain.Aggregates.Order.OrderAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Product
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record ProductAggregate
                                           {
                                               public string Name { get; init; } = string.Empty;
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Product.Commands
                                       {
                                           public sealed record CreateProduct { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Product.Events
                                       {
                                           public sealed record ProductCreated { }
                                       }

                                       namespace TestApp.Domain.Aggregates.Product.Handlers
                                       {
                                           public class CreateProductHandler : CommandHandlerBase<TestApp.Domain.Aggregates.Product.Commands.CreateProduct, TestApp.Domain.Aggregates.Product.ProductAggregate>
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Aggregates.Product.Reducers
                                       {
                                           public class ProductCreatedReducer : EventReducerBase<TestApp.Domain.Aggregates.Product.Events.ProductCreated, TestApp.Domain.Aggregates.Product.ProductAggregate>
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, aggregateSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasOrderRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "OrderAggregateRegistrations",
            StringComparison.Ordinal));
        bool hasProductRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "ProductAggregateRegistrations",
            StringComparison.Ordinal));
        Assert.True(hasOrderRegistrations);
        Assert.True(hasProductRegistrations);
    }
}