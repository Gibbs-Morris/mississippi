using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="McpProjectionToolsGenerator" />.
/// </summary>
public sealed class McpProjectionToolsGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

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
        McpProjectionToolsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     At-version tool should derive description from the base read tool description.
    /// </summary>
    [Fact]
    public void AtVersionToolDerivesDescriptionFromBase()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool(Description = "Gets the current order summary.")]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("at a specific historical version.", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Custom Description from the attribute should appear in the Description attribute and tool metadata.
    /// </summary>
    [Fact]
    public void CustomDescriptionFromAttribute()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool(Description = "Retrieves the current order summary.")]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Retrieves the current order summary.", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Custom Title from the attribute should appear in the McpServerTool attribute.
    /// </summary>
    [Fact]
    public void CustomTitleFromAttribute()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool(Title = "Order Details")]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Title = \"Order Details\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Default behavioral annotations should mark the tool as read-only and non-destructive.
    /// </summary>
    [Fact]
    public void DefaultBehavioralAnnotationsAreReadOnly()
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
        Assert.Contains("Destructive = false", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ReadOnly = true", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Idempotent = true", generatedCode, StringComparison.Ordinal);
        Assert.Contains("OpenWorld = false", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file name should follow the pattern BaseName + McpTools.g.cs.
    /// </summary>
    [Fact]
    public void GeneratedFileNameMatchesToolsClassName()
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
        Assert.Single(runResult.GeneratedTrees);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("OrderSummaryMcpTools.g.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Generated file should start with the auto-generated header comment.
    /// </summary>
    [Fact]
    public void GeneratedFileStartsWithAutoGeneratedHeader()
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
        Assert.Contains("// <auto-generated />", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce an at-version tool method with _at_version suffix.
    /// </summary>
    [Fact]
    public void GeneratesAtVersionToolMethod()
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
        Assert.Contains("GetOrderSummaryAtVersionAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"get_order_summary_at_version\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("long version", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should inject IUxProjectionGrainFactory via constructor.
    /// </summary>
    [Fact]
    public void GeneratesConstructorWithUxProjectionGrainFactory()
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
        Assert.Contains("IUxProjectionGrainFactory uxProjectionGrainFactory", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "UxProjectionGrainFactory = uxProjectionGrainFactory;",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should be placed in the McpTools sub-namespace.
    /// </summary>
    [Fact]
    public void GeneratesInMcpToolsNamespace()
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
        Assert.Contains("namespace TestApp.Server.McpTools;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no projections have the GenerateMcpReadTool attribute.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWhenNoMcpReadToolAttribute()
    {
        const string source = """
                              namespace TestApp.Projections.Order
                              {
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce a read tool method for the projection.
    /// </summary>
    [Fact]
    public void GeneratesReadToolMethodForProjection()
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
        Assert.Contains("GetOrderSummaryAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"get_order_summary\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should have the McpServerToolType attribute.
    /// </summary>
    [Fact]
    public void GeneratesToolsClassWithMcpServerToolType()
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
        Assert.Contains("[McpServerToolType]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public sealed class OrderSummaryMcpTools", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce a version tool method with _version suffix.
    /// </summary>
    [Fact]
    public void GeneratesVersionToolMethod()
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
        Assert.Contains("GetOrderSummaryVersionAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"get_order_summary_version\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Projection suffix should be stripped from the generated class name.
    /// </summary>
    [Fact]
    public void RemovesProjectionSuffixFromClassName()
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
        Assert.Contains("OrderSummaryMcpTools", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderSummaryProjectionMcpTools", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Title from attribute should also appear with At Version and Version suffixes on derived tools.
    /// </summary>
    [Fact]
    public void TitleAppearsOnDerivedToolMethods()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Order
                              {
                                  [GenerateMcpReadTool(Title = "Order Summary")]
                                  public sealed record OrderSummaryProjection
                                  {
                                      public string Status { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Title = \"Order Summary\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Title = \"Order Summary At Version\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Title = \"Order Summary Version\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tool name should use snake_case with get_ prefix derived from the projection name.
    /// </summary>
    [Fact]
    public void ToolNameUsesSnakeCaseWithGetPrefix()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Projections.Account
                              {
                                  [GenerateMcpReadTool]
                                  public sealed record AccountBalanceProjection
                                  {
                                      public decimal Balance { get; init; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"get_account_balance\"", generatedCode, StringComparison.Ordinal);
    }
}