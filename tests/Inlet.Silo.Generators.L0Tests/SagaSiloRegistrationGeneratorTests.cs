using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaSiloRegistrationGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Silo Generators")]
[AllureSubSuite("Saga Silo Registration Generator")]
public class SagaSiloRegistrationGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeAndBaseStubs = """
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
                                                     public interface ISagaDefinition
                                                     {
                                                         static abstract string SagaName { get; }
                                                     }

                                                     public abstract class SagaStepBase<TSaga>
                                                         where TSaga : class, ISagaDefinition
                                                     {
                                                     }

                                                     public abstract class CompensationBase<TSaga>
                                                         where TSaga : class, ISagaDefinition
                                                     {
                                                     }

                                                     [AttributeUsage(AttributeTargets.Class)]
                                                     public sealed class SagaStepAttribute : Attribute
                                                     {
                                                         public int Order { get; set; }
                                                     }

                                                     [AttributeUsage(AttributeTargets.Class)]
                                                     public sealed class CompensationAttribute : Attribute
                                                     {
                                                     }
                                                 }

                                                 namespace Mississippi.EventSourcing.Reducers.Abstractions
                                                 {
                                                     public abstract class EventReducerBase<TEvent, TState>
                                                         where TState : class
                                                     {
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

        // Get all framework references needed for compilation
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];

        // Add netstandard if available (for compatibility)
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

        // Instantiate the generator and run it
        SagaSiloRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return (outputCompilation, diagnostics, runResult);
    }

    /// <summary>
    ///     Verifies that the generator handles a saga with only reducers (no steps/compensations).
    /// </summary>
    [Fact]
    [AllureTag("L0")]
    [AllureFeature("Code Generation")]
    [AllureStory("Generate saga registration for reducer-only saga")]
    public void GeneratorHandlesSagaWithOnlyReducers()
    {
        // Arrange: Saga with only reducers
        string source = $$"""
                          {{AttributeAndBaseStubs}}

                          namespace TestApp.Domain.Sagas.SimpleSaga
                          {
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              [GenerateSagaEndpoints]
                              public sealed record SimpleSagaState : ISagaDefinition
                              {
                                  public static string SagaName => "SimpleSaga";
                              }
                          }

                          namespace TestApp.Domain.Sagas.SimpleSaga.Events
                          {
                              public sealed record SagaStarted;
                          }

                          namespace TestApp.Domain.Sagas.SimpleSaga.Reducers
                          {
                              using Mississippi.EventSourcing.Reducers.Abstractions;
                              using TestApp.Domain.Sagas.SimpleSaga.Events;

                              public sealed class SagaStartedReducer : EventReducerBase<SagaStarted, SimpleSagaState>
                              {
                              }
                          }
                          """;

        // Act
        (var _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) = RunGenerator(source);

        // Assert: No errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Assert: Generated file exists
        GeneratorRunResult generatorResult = runResult.Results.Single();
        Assert.Single(generatorResult.GeneratedSources);

        // Assert: Contains reducer registration
        string generatedCode = generatorResult.GeneratedSources[0].SyntaxTree.ToString();
        Assert.Contains(
            "services.AddReducer<SagaStarted, SimpleSagaState, SagaStartedReducer>()",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the generator produces a valid registration file for a saga with steps, compensations, and reducers.
    /// </summary>
    [Fact]
    [AllureTag("L0")]
    [AllureFeature("Code Generation")]
    [AllureStory("Generate saga registration extension methods")]
    public void GeneratorProducesValidRegistrationForCompleteSaga()
    {
        // Arrange: Saga with steps, compensations, and reducers
        string source = $$"""
                          {{AttributeAndBaseStubs}}

                          namespace TestApp.Domain.Sagas.TransferFunds
                          {
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              [GenerateSagaEndpoints]
                              public sealed record TransferFundsSagaState : ISagaDefinition
                              {
                                  public static string SagaName => "TransferFunds";
                              }
                          }

                          namespace TestApp.Domain.Sagas.TransferFunds.Events
                          {
                              public sealed record TransferInitiated;
                              public sealed record SourceDebited;
                          }

                          namespace TestApp.Domain.Sagas.TransferFunds.Steps
                          {
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              [SagaStep(Order = 1)]
                              public sealed class DebitSourceStep : SagaStepBase<TransferFundsSagaState>
                              {
                              }
                          }

                          namespace TestApp.Domain.Sagas.TransferFunds.Compensations
                          {
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              [Compensation]
                              public sealed class RefundSourceCompensation : CompensationBase<TransferFundsSagaState>
                              {
                              }
                          }

                          namespace TestApp.Domain.Sagas.TransferFunds.Reducers
                          {
                              using Mississippi.EventSourcing.Reducers.Abstractions;
                              using TestApp.Domain.Sagas.TransferFunds.Events;

                              public sealed class TransferInitiatedReducer : EventReducerBase<TransferInitiated, TransferFundsSagaState>
                              {
                              }

                              public sealed class SourceDebitedReducer : EventReducerBase<SourceDebited, TransferFundsSagaState>
                              {
                              }
                          }
                          """;

        // Act
        (var _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) = RunGenerator(source);

        // Assert: No generator diagnostics
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Assert: Exactly one generated file
        GeneratorRunResult generatorResult = runResult.Results.Single();
        Assert.Single(generatorResult.GeneratedSources);

        // Assert: Generated file contains expected extension method
        SyntaxTree generatedTree = generatorResult.GeneratedSources[0].SyntaxTree;
        string generatedCode = generatedTree.ToString();
        Assert.Contains("namespace TestApp.Silo.Registrations", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public static class TransferFundsSagaRegistrations", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "public static IServiceCollection AddTransferFundsSaga",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("services.AddSaga<TransferFundsSagaState>()", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddSagaOrchestration()", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddEventType<TransferInitiated>()", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddEventType<SourceDebited>()", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "services.AddReducer<TransferInitiated, TransferFundsSagaState, TransferInitiatedReducer>()",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "services.AddReducer<SourceDebited, TransferFundsSagaState, SourceDebitedReducer>()",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "services.AddSnapshotStateConverter<TransferFundsSagaState>()",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the generator does not produce output when the saga attribute is missing.
    /// </summary>
    [Fact]
    [AllureTag("L0")]
    [AllureFeature("Code Generation")]
    [AllureStory("Skip generation for unmarked saga types")]
    public void GeneratorSkipsSagaWithoutAttribute()
    {
        // Arrange: Saga without [GenerateSagaEndpoints] attribute
        string source = $$"""
                          {{AttributeAndBaseStubs}}

                          namespace TestApp.Domain.Sagas.TransferFunds
                          {
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              public sealed record TransferFundsSagaState : ISagaDefinition
                              {
                                  public static string SagaName => "TransferFunds";
                              }
                          }
                          """;

        // Act
        (var _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) = RunGenerator(source);

        // Assert: No errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Assert: No generated files
        GeneratorRunResult generatorResult = runResult.Results.Single();
        Assert.Empty(generatorResult.GeneratedSources);
    }

    /// <summary>
    ///     Verifies that the generator produces correct namespace when RootNamespace differs from assembly name.
    /// </summary>
    [Fact]
    [AllureTag("L0")]
    [AllureFeature("Code Generation")]
    [AllureStory("Use correct namespace for saga registrations")]
    public void GeneratorUsesCorrectTargetNamespace()
    {
        // Arrange: Saga in a domain namespace
        string source = $$"""
                          {{AttributeAndBaseStubs}}

                          namespace MyCompany.Banking.Domain.Sagas.Payment
                          {
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.EventSourcing.Sagas.Abstractions;

                              [GenerateSagaEndpoints]
                              public sealed record PaymentSagaState : ISagaDefinition
                              {
                                  public static string SagaName => "Payment";
                              }
                          }

                          namespace MyCompany.Banking.Domain.Sagas.Payment.Events
                          {
                              public sealed record PaymentStarted;
                          }

                          namespace MyCompany.Banking.Domain.Sagas.Payment.Reducers
                          {
                              using Mississippi.EventSourcing.Reducers.Abstractions;
                              using MyCompany.Banking.Domain.Sagas.Payment.Events;

                              public sealed class PaymentStartedReducer : EventReducerBase<PaymentStarted, PaymentSagaState>
                              {
                              }
                          }
                          """;

        // Act
        (var _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) = RunGenerator(source);

        // Assert: No errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Assert: Generated namespace follows convention (uses target assembly namespace)
        string generatedCode = runResult.Results.Single().GeneratedSources[0].SyntaxTree.ToString();

        // The generator uses the target assembly name (TestApp.Silo) for the registration namespace
        Assert.Contains("namespace TestApp.Silo.Registrations", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public static class PaymentSagaRegistrations", generatedCode, StringComparison.Ordinal);
    }
}