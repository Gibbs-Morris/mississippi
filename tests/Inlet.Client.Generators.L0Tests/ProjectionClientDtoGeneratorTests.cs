using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionClientDtoGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Client Generators")]
[AllureSubSuite("Projection Client DTO Generator")]
public class ProjectionClientDtoGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                                  public bool GenerateClientSubscription { get; set; } = true;
                                              }
                                          }

                                          namespace Mississippi.Inlet.Projection.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class ProjectionPathAttribute : Attribute
                                              {
                                                  public ProjectionPathAttribute(string path) { Path = path; }
                                                  public string Path { get; }
                                              }
                                          }
                                          """;

    /// <summary>
    ///     Creates a Roslyn compilation from the provided source code and runs the generator.
    /// </summary>
    /// <param name="sources">The source code to compile.</param>
    /// <remarks>
    ///     The compilation uses "TestApp.Client" as the assembly name, which the generator uses as the
    ///     target root namespace when no RootNamespace MSBuild property is available.
    /// </remarks>
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
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];

        // Add netstandard if available (for compatibility)
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        // Use "TestApp.Client" as assembly name - the generator will use this as the target root namespace
        // when no RootNamespace MSBuild property is available
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Client",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        ProjectionClientDtoGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated DTO file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoFileHasCorrectName()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Contains(
            "AccountBalanceProjectionDto.g.cs",
            runResult.GeneratedTrees[0].FilePath,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasAutoGeneratedHeader()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have correct naming.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasCorrectNaming()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public sealed record AccountBalanceProjectionDto(", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have nullable enabled.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasNullableEnabled()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("#nullable enable", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have ProjectionPath attribute.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasProjectionPathAttribute()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[ProjectionPath(\"account-balance\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have properties from projection.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasPropertiesFromProjection()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string AccountName { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("decimal Balance", generatedCode, StringComparison.Ordinal);
        Assert.Contains("string AccountName", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasXmlDocumentation()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/// <summary>", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Client-side DTO", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include using statement for ProjectionPath attribute.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesUsingStatement()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("using Mississippi.Inlet.Projection.Abstractions;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO namespace should follow client convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoNamespaceFollowsClientConvention()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // Domain.Projections.* -> Client.Features.*.Dtos
        Assert.Contains(
            "namespace TestApp.Client.Features.AccountBalance.Dtos;",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no projections are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoProjections()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public class RegularClass
                                  {
                                      public string Name { get; set; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce no output when projection has GenerateProjectionEndpoints but no ProjectionPath.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenProjectionMissingProjectionPath()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce output when projection has both required attributes.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWhenProjectionHasBothAttributes()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Multiple projections should generate separate DTOs.
    /// </summary>
    [Fact]
    public void MultipleProjectionsGenerateSeparateDtos()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.TransactionHistory
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("transaction-history")]
                                            public sealed record TransactionHistoryProjection
                                            {
                                                public int TransactionCount { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasAccountBalanceDto = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "AccountBalanceProjectionDto",
            StringComparison.Ordinal));
        bool hasTransactionHistoryDto = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "TransactionHistoryProjectionDto",
            StringComparison.Ordinal));
        Assert.True(hasAccountBalanceDto);
        Assert.True(hasTransactionHistoryDto);
    }
}