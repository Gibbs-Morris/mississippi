using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandClientActionEffectsGenerator" />.
/// </summary>
public class CommandClientActionEffectsGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string? Route { get; set; }
                                                  public string HttpMethod { get; set; } = "POST";
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
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];
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
        CommandClientActionEffectsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated action effect file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectFileHasCorrectName()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.Contains("PlaceOrderActionEffect.g.cs", runResult.GeneratedTrees[0].FilePath, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated action effect should have aggregate route prefix.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectHasAggregateRoutePrefix()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("AggregateRoutePrefix =>", generatedCode, StringComparison.Ordinal);
        Assert.Contains("/api/aggregates/order", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated action effect should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectHasAutoGeneratedHeader()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.NotEmpty(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated effect should have route property.
    /// </summary>
    [Fact]
    public void GeneratedEffectHasRouteProperty()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("protected override string Route =>", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated effect should be internal sealed class.
    /// </summary>
    [Fact]
    public void GeneratedEffectIsInternalSealedClass()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("internal sealed class PlaceOrderActionEffect", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no commands are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoCommands()
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
    ///     Multiple commands should generate separate action effects.
    /// </summary>
    [Fact]
    public void MultipleCommandsGenerateSeparateEffects()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }

                                         [GenerateCommand]
                                         public sealed record CancelOrder
                                         {
                                             public string Reason { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasPlaceOrderActionEffect = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "PlaceOrderActionEffect",
            StringComparison.Ordinal));
        bool hasCancelOrderActionEffect = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "CancelOrderActionEffect",
            StringComparison.Ordinal));
        Assert.True(hasPlaceOrderActionEffect);
        Assert.True(hasCancelOrderActionEffect);
    }
}