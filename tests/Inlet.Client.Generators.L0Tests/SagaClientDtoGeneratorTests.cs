using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaClientDtoGenerator" />.
/// </summary>
public class SagaClientDtoGeneratorTests
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
        SagaClientDtoGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     DTO should be in correct feature namespace with Dtos sub-namespace.
    /// </summary>
    [Fact]
    public void DtoIsInCorrectNamespace()
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
        SyntaxTree? dtoTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto.g.cs",
            StringComparison.Ordinal));
        Assert.NotNull(dtoTree);
        string code = dtoTree.GetText().ToString();

        // Should be in Features.TransferFundsSaga.Dtos namespace
        Assert.Contains("Features.TransferFundsSaga.Dtos", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have JsonPropertyName attributes.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasJsonPropertyNameAttributes()
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
        SyntaxTree? dtoTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto.g.cs",
            StringComparison.Ordinal));
        Assert.NotNull(dtoTree);
        string code = dtoTree.GetText().ToString();
        Assert.Contains("[JsonPropertyName(", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include CorrelationId property.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesCorrelationId()
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
        SyntaxTree? dtoTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto.g.cs",
            StringComparison.Ordinal));
        Assert.NotNull(dtoTree);
        string code = dtoTree.GetText().ToString();
        Assert.Contains("CorrelationId", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include input properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesInputProperties()
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
                                          public string TargetAccountId { get; init; }
                                          public decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        SyntaxTree? dtoTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto.g.cs",
            StringComparison.Ordinal));
        Assert.NotNull(dtoTree);
        string code = dtoTree.GetText().ToString();
        Assert.Contains("SourceAccountId", code, StringComparison.Ordinal);
        Assert.Contains("TargetAccountId", code, StringComparison.Ordinal);
        Assert.Contains("Amount", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTOs should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedDtosHaveAutoGeneratedHeader()
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
    ///     No output when ISagaDefinition is not implemented.
    /// </summary>
    [Fact]
    public void NoOutputWhenSagaDefinitionNotImplemented()
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

                                      // Missing ISagaDefinition implementation
                                      [GenerateSagaEndpoints(InputType = typeof(StartTransferFundsSagaInput))]
                                      public sealed class TransferFundsSaga
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }
}