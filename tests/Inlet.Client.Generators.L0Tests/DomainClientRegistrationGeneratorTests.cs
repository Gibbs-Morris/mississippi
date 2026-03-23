using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Hosting.Client;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="DomainClientRegistrationGenerator" />.
/// </summary>
public sealed class DomainClientRegistrationGeneratorTests
{
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                              }
                                          }
                                          """;

    private const string FeatureRegistrationStubs = """
                                                    using Mississippi.Reservoir.Abstractions;

                                                    namespace TestApp.Client.Features.OrderAggregate
                                                    {
                                                        internal static class OrderAggregateFeatureRegistration
                                                        {
                                                            public static IReservoirBuilder AddOrderAggregateFeature(this IReservoirBuilder builder) => builder;
                                                        }
                                                    }

                                                    namespace TestApp.Client.Features.MoneyTransferSaga
                                                    {
                                                        internal static class MoneyTransferSagaFeatureRegistration
                                                        {
                                                            public static IReservoirBuilder AddMoneyTransferSagaFeature(this IReservoirBuilder builder) => builder;
                                                        }
                                                    }

                                                    namespace TestApp.Client.Features
                                                    {
                                                        internal static class ProjectionsFeatureRegistration
                                                        {
                                                            public static IReservoirBuilder AddProjectionsFeature(this IReservoirBuilder builder) => builder;
                                                        }
                                                    }
                                                    """;

    private static void AssertHasNoCompilationErrors(
        Compilation outputCompilation
    )
    {
        ImmutableArray<Diagnostic> compilationDiagnostics = outputCompilation.GetDiagnostics();
        Assert.DoesNotContain(compilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

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
            MetadataReference.CreateFromFile(typeof(MississippiClientBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IReservoirBuilder).Assembly.Location),
        ];
        string netstandardPath = RuntimeAssembly(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Client",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        DomainClientRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated client domain registration composes aggregate, saga, and projection feature registrations.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationComposesAllClientFeatureRegistrations()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Aggregates.Order.Commands
                              {
                                  [GenerateCommand]
                                  public sealed record PlaceOrder;
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
        (Compilation outputCompilation, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, FeatureRegistrationStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainFeatureRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddTestAppDomainClient", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "public static MississippiClientBuilder AddTestAppDomainClient",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("this MississippiClientBuilder client", generatedCode, StringComparison.Ordinal);
        Assert.Contains("client.Reservoir(reservoir =>", generatedCode, StringComparison.Ordinal);
        Assert.Contains("reservoir.AddOrderAggregateFeature();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("reservoir.AddMoneyTransferSagaFeature();", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("SagaSagaFeature", generatedCode, StringComparison.Ordinal);
        Assert.Contains("reservoir.AddProjectionsFeature();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("return client;", generatedCode, StringComparison.Ordinal);
        AssertHasNoCompilationErrors(outputCompilation);
    }

    /// <summary>
    ///     Generated client domain registration uses a non-Domain root name when no Domain segment exists.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationUsesNonDomainRootNameWhenDomainSegmentIsAbsent()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace CoreLogic.Aggregates.Order.Commands
                              {
                                  [GenerateCommand]
                                  public sealed record PlaceOrder;
                              }
                              """;
        (Compilation outputCompilation, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, FeatureRegistrationStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainFeatureRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddCoreLogicClient", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "public static MississippiClientBuilder AddCoreLogicClient",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains("reservoir.AddOrderAggregateFeature();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("return client;", generatedCode, StringComparison.Ordinal);
        AssertHasNoCompilationErrors(outputCompilation);
    }
}