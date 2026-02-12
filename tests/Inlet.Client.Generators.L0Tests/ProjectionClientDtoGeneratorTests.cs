using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionClientDtoGenerator" />.
/// </summary>
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
                                                  public string? Path { get; set; }
                                                  public bool GenerateClientSubscription { get; set; } = true;
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

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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
    ///     Generated DTO should generate nested enum DTO for enum properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoGeneratesNestedEnumDto()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            public enum AccountStatus
                                            {
                                                Active = 0,
                                                Suspended = 1,
                                                Closed = 2
                                            }

                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public AccountStatus Status { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);

        // Should generate main DTO
        Assert.True(runResult.GeneratedTrees.Length >= 1);
        string dtoCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("AccountBalanceProjectionDto", dtoCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTOs should include enum DTOs for top-level enum properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoGeneratesTopLevelEnumDto()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum SagaPhase
                                            {
                                                NotStarted = 0,
                                                Running = 1,
                                            }

                                            [GenerateProjectionEndpoints(Path = "saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public SagaPhase Phase { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? enumDtoSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("SagaPhaseDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(enumDtoSource);
        Assert.Contains("public enum SagaPhaseDto", enumDtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle collection of custom types.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesCollectionOfCustomTypes()
    {
        const string projectionSource = """
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            public sealed record TransactionRecord
                                            {
                                                public decimal Amount { get; init; }
                                                public string Description { get; init; } = string.Empty;
                                            }

                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public ImmutableArray<TransactionRecord> Transactions { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);

        // Should generate main DTO and nested DTO
        Assert.True(runResult.GeneratedTrees.Length >= 1);
    }

    /// <summary>
    ///     Generated DTO should handle DateTimeOffset properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesDateTimeOffsetProperties()
    {
        const string projectionSource = """
                                        using System;
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.Timestamps
                                        {
                                            [GenerateProjectionEndpoints(Path = "timestamps")]
                                            public sealed record TimestampProjection
                                            {
                                                public DateTimeOffset CreatedAt { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("CreatedAt", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle Guid properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesGuidProperties()
    {
        const string projectionSource = """
                                        using System;
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.Identifiers
                                        {
                                            [GenerateProjectionEndpoints(Path = "identifiers")]
                                            public sealed record IdentifierProjection
                                            {
                                                public Guid Id { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("Id", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle ImmutableArray properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesImmutableArrayProperties()
    {
        const string projectionSource = """
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public ImmutableArray<decimal> TransactionAmounts { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("ImmutableArray<decimal> TransactionAmounts", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle nullable properties correctly.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesNullableProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal? OptionalBalance { get; init; }
                                                public string? OptionalName { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // The generator should preserve nullable annotations
        Assert.Contains("OptionalBalance", generatedCode, StringComparison.Ordinal);
        Assert.Contains("OptionalName", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasAutoGeneratedHeader()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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
    ///     Generated DTO should have properties from projection.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasPropertiesFromProjection()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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
    ///     Generated DTO namespace should follow client convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoNamespaceFollowsClientConvention()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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
    ///     Generator should handle projection path with special characters.
    /// </summary>
    [Fact]
    public void GeneratorHandlesProjectionPathWithSpecialCharacters()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.Special
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance/v2")]
                                            public sealed record SpecialPathProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("SpecialPathProjectionDto", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should handle projection with no properties.
    /// </summary>
    [Fact]
    public void GeneratorHandlesProjectionWithNoProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.Empty
                                        {
                                            [GenerateProjectionEndpoints(Path = "empty")]
                                            public sealed record EmptyProjection;
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("public sealed record EmptyProjectionDto()", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should handle projection with static properties by ignoring them.
    /// </summary>
    [Fact]
    public void GeneratorIgnoresStaticProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.WithStatic
                                        {
                                            [GenerateProjectionEndpoints(Path = "with-static")]
                                            public sealed record WithStaticProjection
                                            {
                                                public static string StaticValue { get; } = "static";
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("decimal Balance", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("StaticValue", generatedCode, StringComparison.Ordinal);
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
    ///     Generator should auto-derive path from projection type name when Path is not set.
    /// </summary>
    [Fact]
    public void GeneratorAutoDerivesPathWhenNotExplicit()
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
        Assert.Single(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("AccountBalanceProjectionDto", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce output when projection has explicit Path.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWithExplicitPath()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
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
    ///     Generator should use assembly name when no root namespace is specified.
    /// </summary>
    [Fact]
    public void GeneratorUsesAssemblyNameForNamespaceWhenNoRootNamespace()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;

        // Assembly is named "TestApp.Client" which becomes the root namespace
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // Should use TestApp.Client as root and transform Domain → Client
        Assert.Contains("namespace TestApp.Client", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple projections should generate separate DTOs.
    /// </summary>
    [Fact]
    public void MultipleProjectionsGenerateSeparateDtos()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints(Path = "account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.TransactionHistory
                                        {
                                            [GenerateProjectionEndpoints(Path = "transaction-history")]
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