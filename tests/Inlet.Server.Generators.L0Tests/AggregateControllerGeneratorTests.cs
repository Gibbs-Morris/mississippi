using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateControllerGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Server Generators")]
[AllureSubSuite("Aggregate Controller Generator")]
public class AggregateControllerGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAggregateEndpointsAttribute : Attribute
                                              {
                                                  public string? FeatureKey { get; set; }
                                                  public string? RoutePrefix { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string HttpMethod { get; set; } = "POST";
                                                  public string Route { get; set; } = "";
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

        // Use "TestApp.Server" as assembly name - the generator will use this as the target root namespace
        // when no RootNamespace MSBuild property is available
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        AggregateControllerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Action method should call mapper to convert DTO to domain command.
    /// </summary>
    [Fact]
    public void ActionMethodCallsMapperToConvertDto()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreateOrderMapper.Map(request)", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should include null check for request.
    /// </summary>
    [Fact]
    public void ActionMethodIncludesNullCheckForRequest()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("ArgumentNullException.ThrowIfNull(request);", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should include parameter documentation.
    /// </summary>
    [Fact]
    public void ActionMethodIncludesParameterDocumentation()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "/// <param name=\"entityId\">The entity identifier.</param>",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "/// <param name=\"request\">The command request.</param>",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "/// <param name=\"cancellationToken\">Cancellation token.</param>",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("/// <returns>The operation result.</returns>", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should include route from command attribute.
    /// </summary>
    [Fact]
    public void ActionMethodIncludesRouteFromCommandAttribute()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "submit-for-review")]
                                           public sealed record SubmitOrderForReview
                                           {
                                               public string Notes { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[HttpPost(\"submit-for-review\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should use correct DTO type name.
    /// </summary>
    [Fact]
    public void ActionMethodUsesCorrectDtoTypeName()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[FromBody] CreateOrderDto request", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should use correct HTTP method attribute.
    /// </summary>
    /// <param name="httpMethod">The HTTP method to test.</param>
    /// <param name="expectedAttribute">The expected attribute text in generated code.</param>
    [Theory]
    [InlineData("POST", "[HttpPost(")]
    [InlineData("GET", "[HttpGet(")]
    [InlineData("PUT", "[HttpPut(")]
    [InlineData("DELETE", "[HttpDelete(")]
    [InlineData("PATCH", "[HttpPatch(")]
    public void ActionMethodUsesCorrectHttpMethodAttribute(
        string httpMethod,
        string expectedAttribute
    )
    {
        string aggregateSource = $$"""
                                   using Mississippi.Inlet.Generators.Abstractions;

                                   namespace TestApp.Aggregates.Order
                                   {
                                       [GenerateAggregateEndpoints]
                                       public sealed record OrderAggregate
                                       {
                                           public decimal Total { get; init; }
                                       }
                                   }

                                   namespace TestApp.Aggregates.Order.Commands
                                   {
                                       [GenerateCommand(Route = "action", HttpMethod = "{{httpMethod}}")]
                                       public sealed record DoAction
                                       {
                                           public string Data { get; init; }
                                       }
                                   }
                                   """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(expectedAttribute, generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action method should use kebab-case route when not specified.
    /// </summary>
    [Fact]
    public void ActionMethodUsesKebabCaseRouteWhenNotSpecified()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand]
                                           public sealed record SubmitOrderForReview
                                           {
                                               public string Notes { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[HttpPost(\"submit-order-for-review\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Constructor should assign mappers to properties.
    /// </summary>
    [Fact]
    public void ConstructorAssignsMappersToProperties()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }

                                           [GenerateCommand(Route = "ship")]
                                           public sealed record ShipOrder
                                           {
                                               public string TrackingNumber { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreateOrderMapper = createOrderMapper;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ShipOrderMapper = shipOrderMapper;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct class declaration.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectClassDeclaration()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.BankAccount
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record BankAccountAggregate
                                           {
                                               public decimal Balance { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.BankAccount.Commands
                                       {
                                           [GenerateCommand(Route = "deposit")]
                                           public sealed record DepositFunds
                                           {
                                               public decimal Amount { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public sealed class BankAccountController", generatedCode, StringComparison.Ordinal);
        Assert.Contains("AggregateControllerBase<BankAccountAggregate>", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct route attribute.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectRouteAttribute()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.BankAccount
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record BankAccountAggregate
                                           {
                                               public decimal Balance { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.BankAccount.Commands
                                       {
                                           [GenerateCommand(Route = "deposit")]
                                           public sealed record DepositFunds
                                           {
                                               public decimal Amount { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[Route(\"api/aggregates/bank-account/{entityId}\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include action method for each command.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesActionMethodForEachCommand()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }

                                           [GenerateCommand(Route = "cancel")]
                                           public sealed record CancelOrder
                                           {
                                               public string Reason { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "public Task<ActionResult<OperationResult>> CreateOrderAsync(",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "public Task<ActionResult<OperationResult>> CancelOrderAsync(",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include constructor with required dependencies.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesConstructor()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public OrderController(", generatedCode, StringComparison.Ordinal);
        Assert.Contains("IAggregateGrainFactory aggregateGrainFactory", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ILogger<OrderController> logger", generatedCode, StringComparison.Ordinal);
        Assert.Contains(": base(logger)", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include ExecuteCommandAsync helper method.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesExecuteCommandAsyncMethod()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "private Task<OperationResult> ExecuteCommandAsync<TCommand>(",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "AggregateGrainFactory.GetGenericAggregate<OrderAggregate>(entityId)",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("grain.ExecuteAsync(command, cancellationToken)", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesGeneratedCodeAttribute()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "[global::System.CodeDom.Compiler.GeneratedCode(\"AggregateControllerGenerator\"",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include mapper for each command.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesMapperForEachCommand()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }

                                           [GenerateCommand(Route = "cancel")]
                                           public sealed record CancelOrder
                                           {
                                               public string Reason { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "IMapper<CreateOrderDto, CreateOrder> createOrderMapper",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "IMapper<CancelOrderDto, CancelOrder> cancelOrderMapper",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include required using statements.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesRequiredUsingStatements()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("using System;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using System.Threading;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using System.Threading.Tasks;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Microsoft.AspNetCore.Mvc;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Microsoft.Extensions.Logging;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.Common.Abstractions.Mapping;", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "using Mississippi.EventSourcing.Aggregates.Abstractions;",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Aggregates.Api;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should include XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedControllerIncludesXmlDocumentation()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/// <summary>", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Controller for Order aggregate commands.", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Executes the CreateOrder command.", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should use correct output namespace.
    /// </summary>
    [Fact]
    public void GeneratedControllerUsesCorrectOutputNamespace()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

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
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // The output namespace transforms .Domain.Aggregates.*.Commands → .Server.Controllers.Aggregates
        Assert.Contains("namespace TestApp.Server.Controllers.Aggregates;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should use custom route prefix when specified.
    /// </summary>
    [Fact]
    public void GeneratedControllerUsesCustomRoutePrefixWhenSpecified()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints(RoutePrefix = "orders")]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[Route(\"api/aggregates/orders/{entityId}\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have correct file name.
    /// </summary>
    [Fact]
    public void GeneratedFileHasCorrectFileName()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.BankAccount
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record BankAccountAggregate
                                           {
                                               public decimal Balance { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.BankAccount.Commands
                                       {
                                           [GenerateCommand(Route = "deposit")]
                                           public sealed record DepositFunds
                                           {
                                               public decimal Amount { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        Assert.Single(runResult.Results);
        GeneratorRunResult generatorResult = runResult.Results[0];
        Assert.Single(generatorResult.GeneratedSources);
        Assert.Equal("BankAccountController.g.cs", generatorResult.GeneratedSources[0].HintName);
    }

    /// <summary>
    ///     Generator should handle multiple aggregates in same compilation.
    /// </summary>
    [Fact]
    public void GeneratorHandlesMultipleAggregates()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Customer
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record CustomerAggregate
                                           {
                                               public string Name { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Customer.Commands
                                       {
                                           [GenerateCommand(Route = "register")]
                                           public sealed record RegisterCustomer
                                           {
                                               public string Email { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        List<string> generatedFileNames = runResult.Results[0].GeneratedSources.Select(s => s.HintName).ToList();
        Assert.Contains("OrderController.g.cs", generatedFileNames);
        Assert.Contains("CustomerController.g.cs", generatedFileNames);
    }

    /// <summary>
    ///     Generator should produce controller when aggregate has commands.
    /// </summary>
    [Fact]
    public void GeneratorProducesControllerWhenAggregateHasCommands()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                           }
                                       }

                                       namespace TestApp.Aggregates.Order.Commands
                                       {
                                           [GenerateCommand(Route = "create")]
                                           public sealed record CreateOrder
                                           {
                                               public string CustomerName { get; init; }
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("OrderController", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when aggregate has no commands.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenAggregateHasNoCommands()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace TestApp.Aggregates.Order
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record OrderAggregate
                                           {
                                               public decimal Total { get; init; }
                                               public string Status { get; init; } = string.Empty;
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, aggregateSource);
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
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }
}