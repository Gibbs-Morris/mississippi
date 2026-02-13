using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="DomainSiloRegistrationGenerator" />.
/// </summary>
public sealed class DomainSiloRegistrationGeneratorTests
{
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAggregateEndpointsAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Aggregates
                                          {
                                              public static class AggregateRegistrations
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Reducers
                                          {
                                              public static class ReducerRegistrations
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Snapshots
                                          {
                                              public static class SnapshotRegistrations
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.UxProjections
                                          {
                                              public static class UxProjectionRegistrations
                                              {
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Sagas
                                          {
                                              public static class SagaRegistrations
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
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location) ??
                                  throw new InvalidOperationException("Runtime directory is unavailable.");
        static string RuntimeAssembly(
            string directory,
            string fileName
        ) =>
            Path.Join(directory, fileName);

        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(RuntimeAssembly(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(RuntimeAssembly(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(RuntimeAssembly(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];
        string netstandardPath = RuntimeAssembly(runtimeDirectory, "netstandard.dll");
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
        DomainSiloRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated silo domain registration composes aggregate, saga, and projection registrations.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationComposesAllSiloRegistrations()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Aggregates.Order
                              {
                                  [GenerateAggregateEndpoints]
                                  public sealed record OrderAggregate;
                              }

                              namespace TestApp.Domain.Aggregates.Transfer
                              {
                                  [GenerateSagaEndpoints]
                                  public sealed record MoneyTransferSagaState;
                              }

                              namespace TestApp.Domain.Projections.Balance
                              {
                                  [GenerateProjectionEndpoints]
                                  public sealed record BalanceProjection;
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainSiloRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddTestAppDomainSilo", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddOrderAggregate();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddMoneyTransferSaga();", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("SagaSaga", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddBalanceProjection();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated silo domain registration uses a non-Domain root name when no Domain segment exists.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationUsesNonDomainRootNameWhenDomainSegmentIsAbsent()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace CoreLogic.Aggregates.Order
                              {
                                  [GenerateAggregateEndpoints]
                                  public sealed record OrderAggregate;
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainSiloRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddCoreLogicSilo", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddOrderAggregate();", generatedCode, StringComparison.Ordinal);
    }
}