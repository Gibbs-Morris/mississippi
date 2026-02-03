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

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
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

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientActionsGenerator(),
            AttributeStubs,
            sagaSource);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("StartTransferSagaAction.g.cs", StringComparison.Ordinal));
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("StartTransferSagaExecutingAction.g.cs", StringComparison.Ordinal));
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientDtoGenerator(),
            AttributeStubs,
            sagaSource);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("StartTransferSagaRequestDto.g.cs", StringComparison.Ordinal));
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("string AccountId", generatedCode, StringComparison.Ordinal);
        Assert.Contains("decimal Amount", generatedCode, StringComparison.Ordinal);
        Assert.Contains("string? CorrelationId", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the action effects generator emits saga action effects.
    /// </summary>
    [Fact]
    public void ActionEffectsGeneratorProducesActionEffect()
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientActionEffectsGenerator(),
            AttributeStubs,
            sagaSource);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("StartTransferSagaActionEffect.g.cs", StringComparison.Ordinal));
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/api/sagas/transfer", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the mapper generator emits the saga action mapper.
    /// </summary>
    [Fact]
    public void MapperGeneratorProducesMapper()
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientMappersGenerator(),
            AttributeStubs,
            sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("StartTransferSagaActionMapper", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "new(input.AccountId, input.Amount, input.CorrelationId)",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the reducers generator emits saga reducers.
    /// </summary>
    [Fact]
    public void ReducersGeneratorProducesReducers()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientReducersGenerator(),
            AttributeStubs,
            sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "AggregateCommandStateReducers.ReduceCommandExecuting",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("StartTransferSagaExecutingAction", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the registration generator emits feature registration.
    /// </summary>
    [Fact]
    public void RegistrationGeneratorProducesFeatureRegistration()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientRegistrationGenerator(),
            AttributeStubs,
            sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("AddTransferSagaFeature", generatedCode, StringComparison.Ordinal);
        Assert.Contains("AddMapper<StartTransferSagaAction", generatedCode, StringComparison.Ordinal);
        Assert.Contains("AddActionEffect<TransferSagaState", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the state generator uses explicit feature keys.
    /// </summary>
    [Fact]
    public void StateGeneratorUsesExplicitFeatureKey()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput), FeatureKey = "customKey")]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientStateGenerator(),
            AttributeStubs,
            sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("FeatureKey => \"customKey\"", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies generators skip sagas missing input types.
    /// </summary>
    [Fact]
    public void GeneratorSkipsSagaWithoutInputType()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      [GenerateSagaEndpoints]
                                      public sealed record MissingSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientActionsGenerator(),
            AttributeStubs,
            sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies default route prefix and feature key derivation.
    /// </summary>
    [Fact]
    public void GeneratorDefaultsRoutePrefixAndFeatureKey()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record MoneyTransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(MoneyTransferInput))]
                                      public sealed record MoneyTransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult actionResult) = RunGenerator(
            new SagaClientActionEffectsGenerator(),
            AttributeStubs,
            sagaSource);
        string actionEffectCode = actionResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/api/sagas/money-transfer", actionEffectCode, StringComparison.Ordinal);

        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult stateResult) = RunGenerator(
            new SagaClientStateGenerator(),
            AttributeStubs,
            sagaSource);
        string stateCode = stateResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("FeatureKey => \"moneyTransfer\"", stateCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies types without saga state interface are ignored.
    /// </summary>
    [Fact]
    public void GeneratorSkipsTypeWithoutSagaInterface()
    {
        const string sagaSource = """
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record NotSagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientActionsGenerator(),
            AttributeStubs,
            sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies helper returns no sagas when the saga attribute type is missing.
    /// </summary>
    [Fact]
    public void HelperReturnsEmptyWhenAttributeTypeMissing()
    {
        const string sagaSource = """
                                  namespace TestApp.Domain.Sagas
                                  {
                                      public interface ISagaState
                                      {
                                      }

                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        Compilation compilation = CreateCompilation("TestAssembly", sagaSource);
        TestAnalyzerConfigOptionsProvider optionsProvider = new();
        List<SagaClientGeneratorHelper.SagaClientInfo> sagas =
            SagaClientGeneratorHelper.GetSagasFromCompilation(compilation, optionsProvider);
        Assert.Empty(sagas);
    }

    /// <summary>
    ///     Verifies helper populates saga metadata and accessors.
    /// </summary>
    [Fact]
    public void HelperProvidesSagaMetadataAccessors()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput(string AccountId);

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        Compilation compilation = CreateCompilation("TestAssembly", AttributeStubs, sagaSource);
        TestAnalyzerConfigOptionsProvider optionsProvider = new();
        SagaClientGeneratorHelper.SagaClientInfo info = Assert.Single(
            SagaClientGeneratorHelper.GetSagasFromCompilation(compilation, optionsProvider));
        Assert.Equal("TransferSagaState", info.SagaStateType.Name);
        Assert.Equal("TransferInput", info.InputType.Name);
        Assert.False(string.IsNullOrWhiteSpace(info.InputTypeNamespace));
        Assert.True(info.IsInputPositionalRecord);
    }

    /// <summary>
    ///     Verifies helper uses saga namespace when target root namespace is empty.
    /// </summary>
    [Fact]
    public void HelperUsesSagaNamespaceWhenTargetRootNamespaceEmpty()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        Compilation compilation = CreateCompilation(" ", AttributeStubs, sagaSource);
        TestAnalyzerConfigOptionsProvider optionsProvider = new();
        SagaClientGeneratorHelper.SagaClientInfo info = Assert.Single(
            SagaClientGeneratorHelper.GetSagasFromCompilation(compilation, optionsProvider));
        Assert.Equal(
            "TestApp.Domain.Sagas.Client.Features.TransferSaga",
            info.FeatureRootNamespace);
    }

    /// <summary>
    ///     Verifies explicit route prefixes are used for action effects.
    /// </summary>
    [Fact]
    public void ActionEffectsGeneratorUsesExplicitRoutePrefix()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput), RoutePrefix = "custom-route")]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(
            new SagaClientActionEffectsGenerator(),
            AttributeStubs,
            sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/api/sagas/custom-route", generatedCode, StringComparison.Ordinal);
    }
}