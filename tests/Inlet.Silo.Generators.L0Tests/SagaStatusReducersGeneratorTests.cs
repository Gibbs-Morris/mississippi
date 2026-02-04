using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStatusReducersGenerator" />.
/// </summary>
public sealed class SagaStatusReducersGeneratorTests
{
    private const string AttributeAndBaseStubs = """
                                                 namespace Mississippi.Inlet.Generators.Abstractions
                                                 {
                                                     using System;

                                                     [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                                     public sealed class GenerateSagaStatusReducersAttribute : Attribute
                                                     {
                                                     }
                                                 }

                                                 namespace Mississippi.EventSourcing.Reducers.Abstractions
                                                 {
                                                     public abstract class EventReducerBase<TEvent, TProjection>
                                                         where TProjection : class
                                                     {
                                                     }
                                                 }

                                                 namespace Mississippi.EventSourcing.Sagas.Abstractions
                                                 {
                                                     using System;

                                                     public enum SagaPhase
                                                     {
                                                         NotStarted,
                                                         Running,
                                                         Compensating,
                                                         Completed,
                                                         Compensated,
                                                         Failed,
                                                     }

                                                     public sealed record SagaStartedEvent
                                                     {
                                                         public Guid SagaId { get; init; }
                                                         public string? CorrelationId { get; init; }
                                                         public DateTimeOffset StartedAt { get; init; }
                                                         public string? StepHash { get; init; }
                                                     }

                                                     public sealed record SagaStepCompleted
                                                     {
                                                         public int StepIndex { get; init; }
                                                         public string? StepName { get; init; }
                                                         public DateTimeOffset CompletedAt { get; init; }
                                                     }

                                                     public sealed record SagaStepFailed
                                                     {
                                                         public int StepIndex { get; init; }
                                                         public string? StepName { get; init; }
                                                         public string? ErrorCode { get; init; }
                                                         public string? ErrorMessage { get; init; }
                                                     }

                                                     public sealed record SagaFailed
                                                     {
                                                         public string? ErrorCode { get; init; }
                                                         public string? ErrorMessage { get; init; }
                                                         public DateTimeOffset FailedAt { get; init; }
                                                     }

                                                     public sealed record SagaCompleted
                                                     {
                                                         public DateTimeOffset CompletedAt { get; init; }
                                                     }

                                                     public sealed record SagaCompensating
                                                     {
                                                         public int FromStepIndex { get; init; }
                                                     }

                                                     public sealed record SagaCompensated
                                                     {
                                                         public DateTimeOffset CompletedAt { get; init; }
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
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.dll")),
        ];
        string netstandardPath = Path.Join(runtimeDirectory, "netstandard.dll");
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
        SagaStatusReducersGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated reducers should include saga status reducers with expected assignments.
    /// </summary>
    [Fact]
    public void GeneratedReducersIncludeSagaPhaseAssignments()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.EventSourcing.Sagas.Abstractions;

                                        namespace TestApp.Domain.Projections.SagaStatus
                                        {
                                            [GenerateSagaStatusReducers]
                                            public sealed record SagaStatusProjection
                                            {
                                                public SagaPhase Phase { get; init; }
                                                public int LastCompletedStepIndex { get; init; }
                                                public string? ErrorCode { get; init; }
                                                public string? ErrorMessage { get; init; }
                                                public DateTimeOffset? StartedAt { get; init; }
                                                public DateTimeOffset? CompletedAt { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string? reducersSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("SagaStatusReducers", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(reducersSource);
        Assert.Contains("class SagaStartedStatusReducer", reducersSource, StringComparison.Ordinal);
        Assert.Contains("Phase = SagaPhase.Running", reducersSource, StringComparison.Ordinal);
        Assert.Contains("class SagaFailedStatusReducer", reducersSource, StringComparison.Ordinal);
        Assert.Contains("Phase = SagaPhase.Failed", reducersSource, StringComparison.Ordinal);
        Assert.Contains("class SagaCompletedStatusReducer", reducersSource, StringComparison.Ordinal);
        Assert.Contains("Phase = SagaPhase.Completed", reducersSource, StringComparison.Ordinal);
    }
}