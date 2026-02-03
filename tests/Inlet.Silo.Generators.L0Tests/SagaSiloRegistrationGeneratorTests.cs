using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Inlet.Silo.Generators;


namespace Mississippi.Inlet.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaSiloRegistrationGenerator" />.
/// </summary>
public sealed class SagaSiloRegistrationGeneratorTests
{
    /// <summary>
    ///     Minimal stubs required for saga generator tests.
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
                                          }

                                          namespace Mississippi.EventSourcing.Sagas.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class SagaStepAttribute : Attribute
                                              {
                                                  public SagaStepAttribute(int order)
                                                  {
                                                      Order = order;
                                                  }

                                                  public int Order { get; }

                                                  public Type? Saga { get; set; }
                                              }

                                              public interface ISagaState
                                              {
                                              }

                                              public interface ISagaStep<TSaga>
                                              {
                                              }

                                              public interface ICompensatable<TSaga>
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Reducers.Abstractions
                                          {
                                              public abstract class EventReducerBase<TEvent, TSaga>
                                              {
                                              }
                                          }
                                          """;

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
        ];
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Silo",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        SagaSiloRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    private static CSharpCompilation CreateCompilation(
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
        ];
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        return CSharpCompilation.Create(
            "TestApp.Silo",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
    }

    private static List<object> GetSagasFromCompilation(
        Compilation compilation,
        string targetRootNamespace
    )
    {
        MethodInfo method = typeof(SagaSiloRegistrationGenerator).GetMethod(
            "GetSagasFromCompilation",
            BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException("GetSagasFromCompilation not found.");
        IEnumerable sagas = (IEnumerable)method.Invoke(null, new object[] { compilation, targetRootNamespace })!;
        return sagas.Cast<object>().ToList();
    }

    /// <summary>
    ///     Verifies registration output includes steps and reducers.
    /// </summary>
    [Fact]
    public void GeneratesRegistrationWithStepsAndReducers()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.EventSourcing.Reducers.Abstractions;
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

                                      [SagaStep(0)]
                                      public sealed class DebitStep : ISagaStep<TransferSagaState>, ICompensatable<TransferSagaState>
                                      {
                                      }

                                      [SagaStep(1, Saga = typeof(TransferSagaState))]
                                      public sealed class CreditStep
                                      {
                                      }

                                      public sealed class TransferStarted
                                      {
                                      }

                                      public sealed class TransferStartedReducer : EventReducerBase<TransferStarted, TransferSagaState>
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "AddSagaOrchestration<global::TestApp.Domain.Sagas.TransferSagaState",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddTransient<global::TestApp.Domain.Sagas.DebitStep>",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddSagaStepInfo<global::TestApp.Domain.Sagas.TransferSagaState>",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "typeof(global::TestApp.Domain.Sagas.CreditStep)",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddReducer<global::TestApp.Domain.Sagas.TransferStarted",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies sagas missing input types are skipped.
    /// </summary>
    [Fact]
    public void SkipsSagaWithoutInputType()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      [GenerateSagaEndpoints]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies no step registrations are emitted when no steps exist.
    /// </summary>
    [Fact]
    public void OmitsStepInfoWhenNoSteps()
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.DoesNotContain("AddSagaStepInfo", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies no output when required symbols are missing.
    /// </summary>
    [Fact]
    public void SkipsSagaWhenRequiredSymbolsMissing()
    {
        const string missingStepStubs = """
                                        namespace Mississippi.Inlet.Generators.Abstractions
                                        {
                                            using System;

                                            [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                            public sealed class GenerateSagaEndpointsAttribute : Attribute
                                            {
                                                public Type? InputType { get; set; }
                                            }
                                        }

                                        namespace Mississippi.EventSourcing.Sagas.Abstractions
                                        {
                                            public interface ISagaState
                                            {
                                            }
                                        }
                                        """;
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(missingStepStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies sagas not implementing the saga state interface are ignored.
    /// </summary>
    [Fact]
    public void SkipsSagaWithoutSagaInterface()
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
                                      public sealed record TransferSagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies steps without saga type information are ignored.
    /// </summary>
    [Fact]
    public void IgnoresStepWithoutSagaTypeOrInterface()
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

                                      [SagaStep(0)]
                                      public sealed class MissingSagaStep
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.DoesNotContain("AddSagaStepInfo", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies saga registration accessors can be read via reflection.
    /// </summary>
    [Fact]
    public void SagaRegistrationAccessorsAreReadable()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.EventSourcing.Reducers.Abstractions;
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

                                      [SagaStep(0)]
                                      public sealed class DebitStep : ISagaStep<TransferSagaState>
                                      {
                                      }

                                      public sealed class TransferStarted
                                      {
                                      }

                                      public sealed class TransferStartedReducer
                                          : EventReducerBase<TransferStarted, TransferSagaState>
                                      {
                                      }
                                  }
                                  """;
        Compilation compilation = CreateCompilation(AttributeStubs, sagaSource);
        object sagaInfo = Assert.Single(GetSagasFromCompilation(compilation, "TestApp"));
        Type sagaInfoType = sagaInfo.GetType();
        object inputType = sagaInfoType.GetProperty("InputType")!.GetValue(sagaInfo)!;
        object sagaStateType = sagaInfoType.GetProperty("SagaStateType")!.GetValue(sagaInfo)!;
        IEnumerable reducers = (IEnumerable)sagaInfoType.GetProperty("Reducers")!.GetValue(sagaInfo)!;
        IEnumerable steps = (IEnumerable)sagaInfoType.GetProperty("Steps")!.GetValue(sagaInfo)!;
        object reducerInfo = reducers.Cast<object>().Single();
        object stepInfo = steps.Cast<object>().Single();
        Type reducerInfoType = reducerInfo.GetType();
        Type stepInfoType = stepInfo.GetType();
        object eventType = reducerInfoType.GetProperty("EventType")!.GetValue(reducerInfo)!;
        object reducerType = reducerInfoType.GetProperty("ReducerType")!.GetValue(reducerInfo)!;
        object stepType = stepInfoType.GetProperty("StepType")!.GetValue(stepInfo)!;
        Assert.NotNull(inputType);
        Assert.NotNull(sagaStateType);
        Assert.NotNull(eventType);
        Assert.NotNull(reducerType);
        Assert.NotNull(stepType);
    }
}
