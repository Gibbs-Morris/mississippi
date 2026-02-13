using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="DomainServerRegistrationGenerator" />.
/// </summary>
public sealed class DomainServerRegistrationGeneratorTests
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
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                              }
                                          }

                                          namespace Mississippi.Inlet.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class ProjectionPathAttribute : Attribute
                                              {
                                                  public ProjectionPathAttribute(string path)
                                                  {
                                                  }
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
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        DomainServerRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated server domain registration composes aggregate and projection mapper registrations.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationComposesAllServerMapperRegistrations()
    {
        const string source = """
                              using Mississippi.Inlet.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Aggregates.Order.Commands
                              {
                                  [GenerateCommand]
                                  public sealed record PlaceOrder;
                              }

                              namespace TestApp.Domain.Projections.Balance
                              {
                                  [GenerateProjectionEndpoints]
                                  [ProjectionPath("balances/{id}")]
                                  public sealed record BalanceProjection;
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainServerRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddTestAppDomainServer", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddOrderAggregateMappers();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddBalanceProjectionMappers();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated server domain registration uses a non-Domain root name when no Domain segment exists.
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
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainServerRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("AddCoreLogicServer", generatedCode, StringComparison.Ordinal);
        Assert.Contains("services.AddOrderAggregateMappers();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated server domain registration compiles when only aggregate mapper registrations are included.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationWithOnlyAggregatesCompilesWithoutProjectionMapperUsing()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Aggregates.Order.Commands
                              {
                                  [GenerateCommand]
                                  public sealed record PlaceOrder;
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainServerRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("Controllers.Aggregates.Mappers;", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Controllers.Projections.Mappers;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated server domain registration compiles when only projection mapper registrations are included.
    /// </summary>
    [Fact]
    public void GeneratedDomainRegistrationWithOnlyProjectionsCompilesWithoutAggregateMapperUsing()
    {
        const string source = """
                              using Mississippi.Inlet.Abstractions;
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace TestApp.Domain.Projections.Balance
                              {
                                  [GenerateProjectionEndpoints]
                                  [ProjectionPath("balances/{id}")]
                                  public sealed record BalanceProjection;
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        string generatedCode = runResult.GeneratedTrees
            .First(tree => tree.FilePath.Contains("DomainServerRegistrations", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("Controllers.Projections.Mappers;", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Controllers.Aggregates.Mappers;", generatedCode, StringComparison.Ordinal);
    }
}