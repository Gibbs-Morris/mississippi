using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaClientActionEffectsGenerator" />.
/// </summary>
public class SagaClientActionEffectsGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                                  public Type? InputType { get; set; }
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Sagas.Abstractions
                                          {
                                              public interface ISagaDefinition { }
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
        SagaClientActionEffectsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Action effect should have SagaRoute property.
    /// </summary>
    [Fact]
    public void ActionEffectHasSagaRouteProperty()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        SyntaxTree? effectTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaActionEffect.g.cs", StringComparison.Ordinal));
        Assert.NotNull(effectTree);
        string code = effectTree.GetText().ToString();
        Assert.Contains("SagaRoute", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action effect should inherit from SagaActionEffectBase.
    /// </summary>
    [Fact]
    public void ActionEffectInheritsFromSagaActionEffectBase()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        SyntaxTree? effectTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaActionEffect.g.cs", StringComparison.Ordinal));
        Assert.NotNull(effectTree);
        string code = effectTree.GetText().ToString();
        Assert.Contains("SagaActionEffectBase<", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Action effect should be in correct feature namespace with ActionEffects sub-namespace.
    /// </summary>
    [Fact]
    public void ActionEffectIsInCorrectNamespace()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        SyntaxTree? effectTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaActionEffect.g.cs", StringComparison.Ordinal));
        Assert.NotNull(effectTree);
        string code = effectTree.GetText().ToString();

        // Should be in Features.TransferFundsSaga.ActionEffects namespace
        Assert.Contains("Features.TransferFundsSaga.ActionEffects", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated action effects should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectsHaveAutoGeneratedHeader()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.NotEmpty(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Should generate action effect.
    /// </summary>
    [Fact]
    public void GeneratesActionEffect()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        bool hasActionEffect = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaActionEffect.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasActionEffect, "Should generate StartTransferFundsSagaActionEffect.g.cs");
    }

    /// <summary>
    ///     SagaRoute should be in kebab-case.
    /// </summary>
    [Fact]
    public void SagaRouteIsKebabCase()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record StartTransferFundsSagaInput
                                      {
                                          public string SourceAccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        SyntaxTree? effectTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaActionEffect.g.cs", StringComparison.Ordinal));
        Assert.NotNull(effectTree);
        string code = effectTree.GetText().ToString();

        // TransferFunds should become transfer-funds in kebab-case
        Assert.Contains("transfer-funds", code, StringComparison.Ordinal);
    }
}