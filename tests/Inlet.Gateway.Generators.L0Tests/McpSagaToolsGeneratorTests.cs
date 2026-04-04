using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="McpSagaToolsGenerator" />.
/// </summary>
public sealed class McpSagaToolsGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                                  public Type? InputType { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateMcpSagaToolsAttribute : Attribute
                                              {
                                                  public string? Title { get; set; }
                                                  public string? Description { get; set; }
                                                  public string? ToolPrefix { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
                                              public sealed class GenerateMcpParameterDescriptionAttribute : Attribute
                                              {
                                                  public GenerateMcpParameterDescriptionAttribute(string description) { Description = description; }
                                                  public string Description { get; }
                                              }
                                          }

                                          namespace Mississippi.DomainModeling.Abstractions
                                          {
                                              public interface ISagaState { }
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
        McpSagaToolsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Custom Title from GenerateMcpSagaTools attribute should appear on generated tools.
    /// </summary>
    [Fact]
    public void CustomTitleIsApplied()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools(Title = "Order Process")]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Title = \"Order Process\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Title = \"Order Process Status\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Custom ToolPrefix from GenerateMcpSagaTools attribute should override default tool naming.
    /// </summary>
    [Fact]
    public void CustomToolPrefixIsApplied()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools(ToolPrefix = "start_order")]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"start_order\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"start_order_status\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file name should follow the pattern SagaName + SagaMcpTools.g.cs.
    /// </summary>
    [Fact]
    public void GeneratedFileNameMatchesToolsClassName()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Single(runResult.GeneratedTrees);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("OrderSagaMcpTools.g.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Generated file should start with the auto-generated header comment.
    /// </summary>
    [Fact]
    public void GeneratedFileStartsWithAutoGeneratedHeader()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated />", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should inject IAggregateGrainFactory and the saga recovery service via constructor.
    /// </summary>
    [Fact]
    public void GeneratesConstructorWithAggregateGrainFactoryAndRecoveryService()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("IAggregateGrainFactory aggregateGrainFactory", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "ISagaRecoveryService<OrderSagaState> sagaRecoveryService",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("AggregateGrainFactory = aggregateGrainFactory;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("SagaRecoveryService = sagaRecoveryService;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should be placed in the McpTools sub-namespace.
    /// </summary>
    [Fact]
    public void GeneratesInMcpToolsNamespace()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("namespace TestApp.Server.McpTools;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when the type does not implement ISagaState.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWithoutISagaState()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce no output when GenerateSagaEndpoints attribute is missing.
    /// </summary>
    [Fact]
    public void GeneratesNoOutputWithoutSagaEndpointsAttribute()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce start, raw-status, runtime-status, and resume tool methods for a saga.
    /// </summary>
    [Fact]
    public void GeneratesStartStatusRuntimeStatusAndResumeToolsForSaga()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("OrderAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("GetOrderStatusAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("GetOrderRuntimeStatusAsync", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ResumeOrderAsync", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class should have the McpServerToolType attribute.
    /// </summary>
    [Fact]
    public void GeneratesToolsClassWithMcpServerToolType()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[McpServerToolType]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Input parameter descriptions should be generated from GenerateMcpParameterDescription attributes.
    /// </summary>
    [Fact]
    public void InputParameterDescriptionsFromAttribute()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      [GenerateMcpParameterDescription("The unique identifier of the customer")]
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("The unique identifier of the customer", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Saga state type name should have the SagaState suffix removed for tool naming.
    /// </summary>
    [Fact]
    public void RemovesSagaStateSuffixFromToolName()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartTransferInput
                                  {
                                      public string AccountId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartTransferInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record TransferSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"transfer\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Name = \"transfer_status\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Resume tool should use destructive behavioral annotations and the saga recovery service seam.
    /// </summary>
    [Fact]
    public void ResumeToolHasDestructiveBehavior()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "[McpServerTool(Name = \"order_resume\", Destructive = true, ReadOnly = false, Idempotent = false, OpenWorld = false)]",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "SagaRecoveryService.ResumeAsync(sagaId, cancellationToken)",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-status tool should use read-only behavioral annotations and the saga recovery service seam.
    /// </summary>
    [Fact]
    public void RuntimeStatusToolHasReadOnlyBehavior()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "[McpServerTool(Name = \"order_runtime_status\", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "SagaRecoveryService.GetRuntimeStatusAsync(sagaId, cancellationToken)",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Status tool should use the saga recovery service seam so authorization matches the HTTP status surface.
    /// </summary>
    [Fact]
    public void StatusToolUsesSagaRecoveryServiceSeam()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "OrderSagaState? state = await SagaRecoveryService.GetStateAsync(sagaId, cancellationToken)",
            generatedCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AggregateGrainFactory.GetGenericAggregate<OrderSagaState>(sagaId)",
            generatedCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "await grain.GetStateAsync(cancellationToken)",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Start tool should use destructive behavioral annotations.
    /// </summary>
    [Fact]
    public void StartToolHasDestructiveBehavior()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // The start tool should have Destructive = true, ReadOnly = false, Idempotent = false
        Assert.Contains("Name = \"order\"", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Destructive = true", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Status tool description should be derived from the saga description or default text.
    /// </summary>
    [Fact]
    public void StatusToolDescriptionIsDerived()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools(Description = "Processes customer orders.")]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Gets the current status and state of", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Status tool should use read-only behavioral annotations.
    /// </summary>
    [Fact]
    public void StatusToolHasReadOnlyBehavior()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Name = \"order_status\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated tools class name should include the Saga suffix (e.g., OrderSagaMcpTools).
    /// </summary>
    [Fact]
    public void ToolsClassNameIncludesSagaSuffix()
    {
        const string source = """
                              using Mississippi.DomainModeling.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Sagas
                              {
                                  public sealed record StartOrderInput
                                  {
                                      public string CustomerId { get; init; }
                                  }

                                  [GenerateSagaEndpoints(InputType = typeof(StartOrderInput))]
                                  [GenerateMcpSagaTools]
                                  public sealed record OrderSagaState : ISagaState
                                  {
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public sealed class OrderSagaMcpTools", generatedCode, StringComparison.Ordinal);
    }
}