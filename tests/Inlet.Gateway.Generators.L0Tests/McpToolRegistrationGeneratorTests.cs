using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="McpToolRegistrationGenerator" />.
/// </summary>
public sealed class McpToolRegistrationGeneratorTests
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
                                              public sealed class GenerateMcpReadToolAttribute : Attribute
                                              {
                                                  public string? Title { get; set; }
                                                  public string? Description { get; set; }
                                                  public bool Destructive { get; set; }
                                                  public bool ReadOnly { get; set; } = true;
                                                  public bool Idempotent { get; set; } = true;
                                                  public bool OpenWorld { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateMcpSagaToolsAttribute : Attribute
                                              {
                                                  public string? Title { get; set; }
                                                  public string? Description { get; set; }
                                                  public string? ToolPrefix { get; set; }
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
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.dll")),
        ];
        string netstandardPath = Path.Join(runtimeDirectory, "netstandard.dll");
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
        McpToolRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Aggregate without commands should not be included in registration even with GenerateMcpTools.
    /// </summary>
    [Fact]
    public void AggregateWithoutCommandsIsExcluded()
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
    ///     Generated file name should be McpToolRegistrations.g.cs.
    /// </summary>
    [Fact]
    public void GeneratedFileNameIsCorrect()
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
            tree => tree.FilePath.Contains("McpToolRegistrations.g.cs", StringComparison.Ordinal));
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
    ///     Generated registration class should be placed in the McpTools sub-namespace.
    /// </summary>
    [Fact]
    public void GeneratesInMcpToolsNamespace()
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
    ///     Generator should produce no output when no tools are found.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWhenNoToolsFound()
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

    /// <summary>
    ///     Generated registration class should be a static class named McpToolRegistrations.
    /// </summary>
    [Fact]
    public void GeneratesStaticRegistrationClass()
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
        Assert.Contains("public static class McpToolRegistrations", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce the WithGeneratedMcpTools extension method.
    /// </summary>
    [Fact]
    public void GeneratesWithGeneratedMcpToolsExtensionMethod()
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
            "public static IMcpServerBuilder WithGeneratedMcpTools(",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("this IMcpServerBuilder builder", generatedCode, StringComparison.Ordinal);
        Assert.Contains("return builder;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Registration should include aggregate tools when an aggregate has GenerateMcpTools and commands.
    /// </summary>
    [Fact]
    public void RegistrationIncludesAggregateTools()
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
        Assert.Contains("builder.WithTools<OrderMcpTools>();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Registration should include all tool types (aggregate, projection, saga) when all are present.
    /// </summary>
    [Fact]
    public void RegistrationIncludesAllToolTypes()
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

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }

                              namespace TestApp.Domain.Sagas
                              {
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("builder.WithTools<OrderMcpTools>();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.WithTools<OrderSagaMcpTools>();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.WithTools<OrderSummaryMcpTools>();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Registration should include projection tools when a projection has GenerateMcpReadTool.
    /// </summary>
    [Fact]
    public void RegistrationIncludesProjectionTools()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("builder.WithTools<OrderSummaryMcpTools>();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Registration should include saga tools when a type has GenerateMcpSagaTools.
    /// </summary>
    [Fact]
    public void RegistrationIncludesSagaTools()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("builder.WithTools<OrderSagaMcpTools>();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tool class names should be sorted alphabetically in the registration output.
    /// </summary>
    [Fact]
    public void ToolClassNamesAreSortedAlphabetically()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Aggregates.Zebra
                              {
                                  [GenerateMcpTools]
                                  public sealed record ZebraAggregate
                                  {
                                      public int Count { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Zebra.Commands
                              {
                                  [GenerateCommand(Route = "add")]
                                  public sealed record AddZebra
                                  {
                                      public string Name { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Apple
                              {
                                  [GenerateMcpTools]
                                  public sealed record AppleAggregate
                                  {
                                      public int Weight { get; init; }
                                  }
                              }

                              namespace TestApp.Aggregates.Apple.Commands
                              {
                                  [GenerateCommand(Route = "pick")]
                                  public sealed record PickApple
                                  {
                                      public string Variety { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        int appleIndex = generatedCode.IndexOf("AppleMcpTools", StringComparison.Ordinal);
        int zebraIndex = generatedCode.IndexOf("ZebraMcpTools", StringComparison.Ordinal);
        Assert.True(appleIndex < zebraIndex, "AppleMcpTools should appear before ZebraMcpTools in sorted output.");
    }

    /// <summary>
    ///     Generated registration should use Microsoft.Extensions.DependencyInjection namespace.
    /// </summary>
    [Fact]
    public void UsesCorrectNamespaceImports()
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
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", generatedCode, StringComparison.Ordinal);
    }
}