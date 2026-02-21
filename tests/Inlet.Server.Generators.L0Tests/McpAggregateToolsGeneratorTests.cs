using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="McpAggregateToolsGenerator" />.
/// </summary>
public sealed class McpAggregateToolsGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string HttpMethod { get; set; } = "POST";
                                                  public string Route { get; set; } = "";
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateMcpToolsAttribute : Attribute
                                              {
                                                  public string? ToolPrefix { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateMcpToolMetadataAttribute : Attribute
                                              {
                                                  public string? Title { get; set; }
                                                  public string? Description { get; set; }
                                                  public bool Destructive { get; set; } = true;
                                                  public bool ReadOnly { get; set; }
                                                  public bool Idempotent { get; set; }
                                                  public bool OpenWorld { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
                                              public sealed class GenerateMcpParameterDescriptionAttribute : Attribute
                                              {
                                                  public GenerateMcpParameterDescriptionAttribute(string description) { Description = description; }
                                                  public string Description { get; }
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

        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];

        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        McpAggregateToolsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated file should start with the auto-generated header comment.
    /// </summary>
    [Fact]
    public void GeneratedFileStartsWithAutoGeneratedHeader()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated />", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should have the McpServerToolType attribute.
    /// </summary>
    [Fact]
    public void GeneratesToolsClassWithMcpServerToolType()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[McpServerToolType]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public sealed class OrderMcpTools", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tool method name should use snake_case derived from the command type name.
    /// </summary>
    [Fact]
    public void GeneratesToolMethodWithSnakeCaseNaming()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"create_order\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Default behavioral annotations should mark the tool as destructive and non-read-only.
    /// </summary>
    [Fact]
    public void GeneratesDefaultBehavioralAnnotations()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Destructive = true", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ReadOnly = false", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Idempotent = false", generatedCode, StringComparison.Ordinal);
        Assert.Contains("OpenWorld = false", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     GenerateMcpToolMetadata attribute should override default behavioral annotations.
    /// </summary>
    [Fact]
    public void RespectsToolMetadataOverrides()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "status")]
                                  [GenerateMcpToolMetadata(
                                      Title = "Get Order Status",
                                      Description = "Retrieves the current order status.",
                                      Destructive = false,
                                      ReadOnly = true,
                                      Idempotent = true)]
                                  public sealed record GetOrderStatus
                                  {
                                      public string OrderId { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Title = \"Get Order Status\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Destructive = false", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ReadOnly = true", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Idempotent = true", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Retrieves the current order status.", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     GenerateMcpParameterDescription attribute on properties should produce custom descriptions.
    /// </summary>
    [Fact]
    public void GeneratesParameterDescriptionsFromAttribute()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
                                      [GenerateMcpParameterDescription("The full name of the customer placing the order")]
                                      public string CustomerName { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "The full name of the customer placing the order",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Positional record commands should use constructor-based instantiation.
    /// </summary>
    [Fact]
    public void HandlesPositionalRecordCommands()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "create")]
                                  public sealed record CreateOrder(string CustomerName, decimal Amount);
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreateOrder command = new(customerName, amount);", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no aggregates have the GenerateMcpTools attribute.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWhenNoMcpToolsAttribute()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
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
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce no output when aggregate has no commands in the Commands sub-namespace.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWhenAggregateHasNoCommands()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Custom ToolPrefix from GenerateMcpTools attribute should be prepended to tool names.
    /// </summary>
    [Fact]
    public void CustomToolPrefixIsApplied()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Inventory
                              {
                                  [GenerateMcpTools(ToolPrefix = "Inventory")]
                                  public sealed record InventoryAggregate
                                  {
                                      public int Quantity { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Inventory.Commands
                              {
                                  [GenerateCommand(Route = "add")]
                                  public sealed record AddItem
                                  {
                                      public string Sku { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"inventory_add_item\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Nullable properties should be passed through with their nullable type annotation.
    /// </summary>
    [Fact]
    public void HandlesNullableProperties()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
                                      public string? Notes { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("string? notes", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Declared default values on command properties should be preserved in generated parameters.
    /// </summary>
    [Fact]
    public void PreservesDefaultValues()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "create")]
                                  public sealed record CreateOrder(string CustomerName, int Quantity = 1);
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("= 1", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple commands on the same aggregate should each generate a tool method.
    /// </summary>
    [Fact]
    public void GeneratesMultipleCommandMethods()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreateOrderAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("CancelOrderAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"create_order\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"cancel_order\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should be placed in the McpTools sub-namespace of the target root namespace.
    /// </summary>
    [Fact]
    public void GeneratesToolClassInMcpToolsNamespace()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("namespace TestApp.Server.McpTools;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should inject IAggregateGrainFactory via constructor.
    /// </summary>
    [Fact]
    public void GeneratesConstructorWithAggregateGrainFactory()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("IAggregateGrainFactory aggregateGrainFactory", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "AggregateGrainFactory = aggregateGrainFactory;",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tool method should include Description attribute with auto-generated description text.
    /// </summary>
    [Fact]
    public void GeneratesDescriptionAttributeOnToolMethod()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "Executes the CreateOrder command on the Order aggregate.",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Aggregate name should have the Aggregate suffix stripped in generated class name.
    /// </summary>
    [Fact]
    public void StripsAggregateSuffixFromClassName()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("OrderMcpTools", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderAggregateMcpTools", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Non-record command classes should use property initializer syntax for instantiation.
    /// </summary>
    [Fact]
    public void PropertyBasedCommandUsesPropertyInitializerSyntax()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "create")]
                                  public sealed class CreateOrder
                                  {
                                      public string CustomerName { get; init; }
                                      public decimal Amount { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreateOrder command = new() {", generatedCode, StringComparison.Ordinal);
        Assert.Contains("CustomerName = customerName,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Amount = amount,", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Positional record parameter descriptions should be collected from constructor parameters.
    /// </summary>
    [Fact]
    public void CollectsParameterDescriptionsFromPositionalRecordConstructor()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
                                  public sealed record OrderAggregate
                                  {
                                      public decimal Total { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "create")]
                                  public sealed record CreateOrder(
                                      [GenerateMcpParameterDescription("Name of the customer")] string CustomerName);
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name of the customer", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file name should follow the pattern BaseName + McpTools.g.cs.
    /// </summary>
    [Fact]
    public void GeneratedFileNameMatchesToolsClassName()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        Assert.Single(runResult.GeneratedTrees);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("OrderMcpTools.g.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Tool method parameters should use camelCase naming derived from PascalCase property names.
    /// </summary>
    [Fact]
    public void ParameterNamesAreCamelCase()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("string customerName", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tool method should include entityId as the first parameter with a description.
    /// </summary>
    [Fact]
    public void ToolMethodIncludesEntityIdParameter()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Order
                              {
                                  [GenerateMcpTools]
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
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "[Description(\"The entity identifier\")] string entityId",
            generatedCode,
            StringComparison.Ordinal);
    }
}
