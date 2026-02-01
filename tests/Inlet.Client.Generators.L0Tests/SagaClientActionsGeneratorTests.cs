using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaClientActionsGenerator" />.
/// </summary>
public class SagaClientActionsGeneratorTests
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
        SagaClientActionsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Actions should be in correct feature namespace with Actions sub-namespace.
    /// </summary>
    [Fact]
    public void ActionsAreInCorrectNamespace()
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
        SyntaxTree? actionTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaAction.g.cs", StringComparison.Ordinal));
        Assert.NotNull(actionTree);
        string code = actionTree.GetText().ToString();

        // Should be in Features.TransferFundsSaga.Actions namespace
        Assert.Contains("Features.TransferFundsSaga.Actions", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Executing action should have factory method with saga parameters.
    /// </summary>
    [Fact]
    public void ExecutingActionHasFactoryMethod()
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
        SyntaxTree? actionTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaExecutingAction.g.cs", StringComparison.Ordinal));
        Assert.NotNull(actionTree);
        string code = actionTree.GetText().ToString();

        // Factory method takes saga execution parameters
        Assert.Contains("static", code, StringComparison.Ordinal);
        Assert.Contains("Create(", code, StringComparison.Ordinal);
        Assert.Contains("sagaId", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Failed action should include ErrorCode and ErrorMessage properties.
    /// </summary>
    [Fact]
    public void FailedActionHasErrorProperties()
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
        SyntaxTree? actionTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaFailedAction.g.cs", StringComparison.Ordinal));
        Assert.NotNull(actionTree);
        string code = actionTree.GetText().ToString();
        Assert.Contains("ErrorCode", code, StringComparison.Ordinal);
        Assert.Contains("ErrorMessage", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated actions should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedActionsHaveAutoGeneratedHeader()
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
        Assert.NotEmpty(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated actions should include executing action.
    /// </summary>
    [Fact]
    public void GeneratedActionsIncludeExecutingAction()
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
        bool hasExecutingAction = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaExecutingAction.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasExecutingAction, "Should generate StartTransferFundsSagaExecutingAction.g.cs");
    }

    /// <summary>
    ///     Generated actions should include failed action.
    /// </summary>
    [Fact]
    public void GeneratedActionsIncludeFailedAction()
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
        bool hasFailedAction = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaFailedAction.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasFailedAction, "Should generate StartTransferFundsSagaFailedAction.g.cs");
    }

    /// <summary>
    ///     Generated actions should include start action with SagaId.
    /// </summary>
    [Fact]
    public void GeneratedActionsIncludeStartAction()
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
        bool hasStartAction = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaAction.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasStartAction, "Should generate StartTransferFundsSagaAction.g.cs");
    }

    /// <summary>
    ///     Generated actions should include succeeded action.
    /// </summary>
    [Fact]
    public void GeneratedActionsIncludeSucceededAction()
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
        bool hasSucceededAction = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaSucceededAction.g.cs",
            StringComparison.Ordinal));
        Assert.True(hasSucceededAction, "Should generate StartTransferFundsSagaSucceededAction.g.cs");
    }

    /// <summary>
    ///     No output when InputType is not provided.
    /// </summary>
    [Fact]
    public void NoOutputWhenInputTypeNotProvided()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      // Missing InputType in attribute
                                      [GenerateSagaEndpoints]
                                      public sealed class TransferFundsSaga : ISagaDefinition
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
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

    /// <summary>
    ///     Start action should include Guid SagaId property.
    /// </summary>
    [Fact]
    public void StartActionHasGuidSagaIdProperty()
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
        SyntaxTree? actionTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaAction.g.cs", StringComparison.Ordinal));
        Assert.NotNull(actionTree);
        string code = actionTree.GetText().ToString();
        Assert.Contains("Guid SagaId", code, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Start action should include input properties.
    /// </summary>
    [Fact]
    public void StartActionIncludesInputProperties()
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
        SyntaxTree? actionTree = runResult.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.Contains("StartTransferFundsSagaAction.g.cs", StringComparison.Ordinal));
        Assert.NotNull(actionTree);
        string code = actionTree.GetText().ToString();
        Assert.Contains("SourceAccountId", code, StringComparison.Ordinal);
        Assert.Contains("TargetAccountId", code, StringComparison.Ordinal);
        Assert.Contains("Amount", code, StringComparison.Ordinal);
    }
}