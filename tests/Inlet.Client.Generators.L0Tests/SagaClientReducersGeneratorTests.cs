using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaClientReducersGenerator" />.
/// </summary>
public class SagaClientReducersGeneratorTests
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
        SagaClientReducersGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Executing reducer should set IsExecuting to true.
    /// </summary>
    [Fact]
    public void ExecutingReducerSetsIsExecutingTrue()
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
        SyntaxTree? reducerTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaExecutingReducer.g.cs", StringComparison.Ordinal));
        Assert.NotNull(reducerTree);
        string code = reducerTree.GetText().ToString();
        Assert.Contains("IsExecuting = true", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Failed reducer should capture error details.
    /// </summary>
    [Fact]
    public void FailedReducerCapturesErrorDetails()
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
        SyntaxTree? reducerTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaFailedReducer.g.cs", StringComparison.Ordinal));
        Assert.NotNull(reducerTree);
        string code = reducerTree.GetText().ToString();
        Assert.Contains("ErrorCode = action.ErrorCode", code, StringComparison.Ordinal);
        Assert.Contains("ErrorMessage = action.ErrorMessage", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated reducers should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedReducersHaveAutoGeneratedHeader()
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
    ///     Should generate executing reducer.
    /// </summary>
    [Fact]
    public void GeneratesExecutingReducer()
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
        bool hasExecutingReducer = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaExecutingReducer.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasExecutingReducer, "Should generate StartTransferFundsSagaExecutingReducer.g.cs");
    }

    /// <summary>
    ///     Should generate failed reducer.
    /// </summary>
    [Fact]
    public void GeneratesFailedReducer()
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
        bool hasFailedReducer = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaFailedReducer.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasFailedReducer, "Should generate StartTransferFundsSagaFailedReducer.g.cs");
    }

    /// <summary>
    ///     Should generate succeeded reducer.
    /// </summary>
    [Fact]
    public void GeneratesSucceededReducer()
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
        bool hasSucceededReducer = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaSucceededReducer.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasSucceededReducer, "Should generate StartTransferFundsSagaSucceededReducer.g.cs");
    }

    /// <summary>
    ///     Reducers should be in correct feature namespace with Reducers sub-namespace.
    /// </summary>
    [Fact]
    public void ReducersAreInCorrectNamespace()
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
        SyntaxTree? reducerTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaExecutingReducer.g.cs", StringComparison.Ordinal));
        Assert.NotNull(reducerTree);
        string code = reducerTree.GetText().ToString();

        // Should be in Features.TransferFundsSaga.Reducers namespace
        Assert.Contains("Features.TransferFundsSaga.Reducers", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Succeeded reducer should set IsExecuting to false.
    /// </summary>
    [Fact]
    public void SucceededReducerSetsIsExecutingFalse()
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
        SyntaxTree? reducerTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaSucceededReducer.g.cs", StringComparison.Ordinal));
        Assert.NotNull(reducerTree);
        string code = reducerTree.GetText().ToString();
        Assert.Contains("IsExecuting = false", code, StringComparison.Ordinal);
    }
}