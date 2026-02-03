using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for saga client generators.
/// </summary>
public sealed class SagaClientGeneratorsTests
{
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                                  public Type? InputType { get; set; }
                                                  public string? FeatureKey { get; set; }
                                                  public string? RoutePrefix { get; set; }
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Sagas.Abstractions
                                          {
                                              public interface ISagaState
                                              {
                                              }
                                          }
                                          """;

    private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult
        RunResult) RunGenerator(
            IIncrementalGenerator generator,
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
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Verifies the actions generator emits start saga actions.
    /// </summary>
    [Fact]
    public void ActionsGeneratorProducesStartSagaActions()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                          public decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(new SagaClientActionsGenerator(), AttributeStubs, sagaSource);
        Assert.Contains(runResult.GeneratedTrees, tree =>
            tree.FilePath.Contains("StartTransferSagaAction.g.cs", StringComparison.Ordinal));
        Assert.Contains(runResult.GeneratedTrees, tree =>
            tree.FilePath.Contains("StartTransferSagaExecutingAction.g.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies the DTO generator emits the start saga request DTO.
    /// </summary>
    [Fact]
    public void DtoGeneratorProducesStartSagaDto()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                          public decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(new SagaClientDtoGenerator(), AttributeStubs, sagaSource);
        Assert.Contains(runResult.GeneratedTrees, tree =>
            tree.FilePath.Contains("StartTransferSagaRequestDto.g.cs", StringComparison.Ordinal));
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("string AccountId", generatedCode, StringComparison.Ordinal);
        Assert.Contains("decimal Amount", generatedCode, StringComparison.Ordinal);
        Assert.Contains("string? CorrelationId", generatedCode, StringComparison.Ordinal);
    }
}
