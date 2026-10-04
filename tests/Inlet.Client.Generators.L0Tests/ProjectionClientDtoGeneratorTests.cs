using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


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
                                                  public bool GenerateClientSubscription { get; set; } = true;
                                              }
                                          }

                                          namespace Mississippi.Inlet.Abstractions
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

    private static void AssertSuccessfulEnumOutput(
        Compilation output,
        ImmutableArray<Diagnostic> diagnostics,
        GeneratorDriverRunResult result,
        params string[] enumNames
    )
    {
        Compilation input = output.RemoveSyntaxTrees(result.GeneratedTrees);
        Diagnostic[] inputErrors = input.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        TestContext.Current.TestOutputHelper?.WriteLine($"Input errors: {inputErrors.Length}");
        Assert.Empty(inputErrors);
        foreach (Diagnostic diagnostic in diagnostics)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(diagnostic.ToString());
        }

        foreach (GeneratorRunResult generatorResult in result.Results)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"Generator exception: {generatorResult.Exception}");
        }

        foreach (SyntaxTree tree in result.GeneratedTrees)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{tree.FilePath}\n{tree.GetText(TestContext.Current.CancellationToken)}");
        }

        Assert.Empty(diagnostics);
        Assert.All(result.Results, generatorResult => Assert.Null(generatorResult.Exception));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorRunResult generated = Assert.Single(result.Results);
        Assert.Equal(
            generated.GeneratedSources.Length,
            generated.GeneratedSources.Select(item => item.HintName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            enumNames.Length,
            result.GeneratedTrees
                .SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes())
                .OfType<EnumDeclarationSyntax>()
                .Count());
        foreach (string enumName in enumNames)
        {
            INamedTypeSymbol enumType = Assert.IsType<INamedTypeSymbol>(output.GetTypeByMetadataName(enumName), false);
            Assert.Equal(TypeKind.Enum, enumType.TypeKind);
            Assert.Single(enumType.DeclaringSyntaxReferences);
            Assert.Equal(3, Assert.Single(enumType.GetMembers("Pending").OfType<IFieldSymbol>()).ConstantValue);
            Assert.Equal(7, Assert.Single(enumType.GetMembers("Complete").OfType<IFieldSymbol>()).ConstantValue);
        }
    }

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
    ///     Projection arrays generate their custom and enum element DTOs and compile alongside primitive arrays.
    /// </summary>
    [Fact]
    public void GeneratedDtoCompilesForCustomAndEnumArrays()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.Inlet.Abstractions;

                              namespace TestApp.Domain.Projections.Array;

                              public sealed record Entry
                              {
                                  public decimal Amount { get; init; }
                              }

                              public enum EntryStatus { New, Complete }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("array")]
                              public sealed record ArrayProjection
                              {
                                  public Entry[] Entries { get; init; } = [];
                                  public EntryStatus[] Statuses { get; init; } = [];
                                  public int[] Values { get; init; } = [];
                              }
                              """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, source);
        Compilation input = output.RemoveSyntaxTrees(result.GeneratedTrees);
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generatorResult => Assert.Null(generatorResult.Exception));
        foreach (SyntaxTree tree in result.GeneratedTrees)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{tree.FilePath}\n{tree.GetText(TestContext.Current.CancellationToken)}");
        }

        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Contains(
            result.GeneratedTrees,
            tree => tree.FilePath.EndsWith("EntryDto.g.cs", StringComparison.Ordinal));
        Assert.Contains(
            result.GeneratedTrees,
            tree => tree.FilePath.EndsWith("EntryStatusDto.g.cs", StringComparison.Ordinal));
        string projectionDto = Assert.Single(
                result.GeneratedTrees,
                tree => tree.FilePath.EndsWith("ArrayProjectionDto.g.cs", StringComparison.Ordinal))
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.Contains("EntryDto[] Entries", projectionDto, StringComparison.Ordinal);
        Assert.Contains("EntryStatusDto[] Statuses", projectionDto, StringComparison.Ordinal);
        Assert.Contains("int[] Values", projectionDto, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoFileHasCorrectName()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

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
    ///     Generated DTO should generate nested enum DTO for enum properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoGeneratesNestedEnumDto()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            public enum AccountStatus
                                            {
                                                Active = 0,
                                                Suspended = 1,
                                                Closed = 2
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
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
        string dtoCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum SagaPhase
                                            {
                                                NotStarted = 0,
                                                Running = 1,
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public SagaPhase Phase { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? enumDtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SagaPhaseDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            public sealed record TransactionRecord
                                            {
                                                public decimal Amount { get; init; }
                                                public string Description { get; init; } = string.Empty;
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public ImmutableArray<TransactionRecord> Transactions { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? nestedDtoSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("TransactionRecordDto.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(nestedDtoSource);
        Assert.Contains("public sealed record TransactionRecordDto", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("Amount", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("Description", nestedDtoSource, StringComparison.Ordinal);
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Timestamps
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("timestamps")]
                                            public sealed record TimestampProjection
                                            {
                                                public DateTimeOffset CreatedAt { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        SyntaxTree item = Assert.Single(runResult.GeneratedTrees);
        string generatedCode = item.GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Identifiers
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("identifiers")]
                                            public sealed record IdentifierProjection
                                            {
                                                public Guid Id { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        SyntaxTree item = Assert.Single(runResult.GeneratedTrees);
        string generatedCode = item.GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public ImmutableArray<decimal> TransactionAmounts { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal? OptionalBalance { get; init; }
                                                public string? OptionalName { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        SyntaxTree item = Assert.Single(runResult.GeneratedTrees);
        string generatedCode = item.GetText(TestContext.Current.CancellationToken).ToString();

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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("using Mississippi.Inlet.Abstractions;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO namespace should follow client convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoNamespaceFollowsClientConvention()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

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
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();

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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Special
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance/v2")]
                                            public sealed record SpecialPathProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("[ProjectionPath(\"account-balance/v2\")]", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should handle projection with no properties.
    /// </summary>
    [Fact]
    public void GeneratorHandlesProjectionWithNoProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Empty
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("empty")]
                                            public sealed record EmptyProjection;
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        SyntaxTree item = Assert.Single(runResult.GeneratedTrees);
        string generatedCode = item.GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.WithStatic
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("with-static")]
                                            public sealed record WithStaticProjection
                                            {
                                                public static string StaticValue { get; } = "static";
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
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
                                        using Mississippi.Inlet.Abstractions;

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
    ///     Generator should use assembly name when no root namespace is specified.
    /// </summary>
    [Fact]
    public void GeneratorUsesAssemblyNameForNamespaceWhenNoRootNamespace()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

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

        // Assembly is named "TestApp.Client" which becomes the root namespace
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();

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
                                        using Mississippi.Inlet.Abstractions;

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

    /// <summary>
    ///     Distinct client namespaces each retain their required enum DTO, including same-named source enums.
    /// </summary>
    /// <param name="hasSharedSourceEnum">Whether both namespaces reference the same source enum.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedEnumOutputPreservesNamespaceIdentity(
        bool hasSharedSourceEnum
    )
    {
        string source = $$"""
                          using System.Collections.Immutable;
                          using Mississippi.Inlet.Generators.Abstractions;
                          using Mississippi.Inlet.Abstractions;

                          namespace TestApp.Domain.Common
                          {
                              public enum Status { Pending = 3, Complete = 7 }
                          }

                          namespace TestApp.Domain.Projections.First
                          {
                              public enum Status { Pending = 3, Complete = 7 }
                              public sealed record FirstEntry
                              {
                                  public {{(hasSharedSourceEnum ? "TestApp.Domain.Common.Status" : "Status")}} Status { get; init; }
                              }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("first")]
                              public sealed record FirstProjection
                              {
                                  public ImmutableArray<FirstEntry> Entries { get; init; } = [];
                              }
                          }

                          namespace TestApp.Domain.Projections.Second
                          {
                              public enum Status { Pending = 3, Complete = 7 }
                              public sealed record SecondEntry
                              {
                                  public {{(hasSharedSourceEnum ? "TestApp.Domain.Common.Status" : "Status")}} Status { get; init; }
                              }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("second")]
                              public sealed record SecondProjection
                              {
                                  public ImmutableArray<SecondEntry> Entries { get; init; } = [];
                              }
                          }
                          """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, source);
        AssertSuccessfulEnumOutput(
            output,
            diagnostics,
            result,
            "TestApp.Client.Features.First.Dtos.StatusDto",
            "TestApp.Client.Features.Second.Dtos.StatusDto");
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.First.Dtos.FirstProjectionDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Second.Dtos.SecondProjectionDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.First.Dtos.FirstEntryDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Second.Dtos.SecondEntryDto"));
    }

    /// <summary>
    ///     Separate collection elements share their enum with direct and collection projection properties.
    /// </summary>
    /// <param name="hasProjectionEnum">Whether the projection also references the shared enum.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedEnumOutputReusesEnumAcrossNestedDtos(
        bool hasProjectionEnum
    )
    {
        string source = $$"""
                          using System.Collections.Immutable;
                          using Mississippi.Inlet.Generators.Abstractions;
                          using Mississippi.Inlet.Abstractions;

                          namespace TestApp.Domain.Projections.Shared;

                          public enum Status { Pending = 3, Complete = 7 }
                          public sealed record FirstEntry { public Status Status { get; init; } }
                          public sealed record SecondEntry { public Status? Status { get; init; } }

                          [GenerateProjectionEndpoints]
                          [ProjectionPath("shared")]
                          public sealed record SharedProjection
                          {
                              public ImmutableArray<FirstEntry> First { get; init; } = [];
                              public ImmutableArray<SecondEntry> Second { get; init; } = [];
                              {{(hasProjectionEnum ? "public Status Status { get; init; } public ImmutableArray<Status> Statuses { get; init; } = [];" : string.Empty)}}
                          }
                          """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, source);
        AssertSuccessfulEnumOutput(output, diagnostics, result, "TestApp.Client.Features.Shared.Dtos.StatusDto");
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.FirstEntryDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.SecondEntryDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.SharedProjectionDto"));
    }

    /// <summary>
    ///     Enum output is shared across projections that generate DTOs into the same namespace.
    /// </summary>
    [Fact]
    public void NestedEnumOutputReusesEnumAcrossProjections()
    {
        const string source = """
                              using System.Collections.Immutable;
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.Inlet.Abstractions;

                              namespace TestApp.Domain.Projections.Shared;

                              public enum Status { Pending = 3, Complete = 7 }
                              public sealed record FirstEntry { public Status Status { get; init; } }
                              public sealed record SecondEntry { public Status Status { get; init; } }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("first")]
                              public sealed record FirstProjection
                              {
                                  public ImmutableArray<FirstEntry> Entries { get; init; } = [];
                              }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("second")]
                              public sealed record SecondProjection
                              {
                                  public ImmutableArray<SecondEntry> Entries { get; init; } = [];
                              }
                              """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, source);
        AssertSuccessfulEnumOutput(output, diagnostics, result, "TestApp.Client.Features.Shared.Dtos.StatusDto");
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.FirstProjectionDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.SecondProjectionDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.FirstEntryDto"));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Shared.Dtos.SecondEntryDto"));
    }

    /// <summary>
    ///     Repeated enum properties on one collection element share a single generated enum.
    /// </summary>
    /// <param name="isNullable">Whether the second property is nullable.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedEnumOutputReusesEnumForRepeatedProperties(
        bool isNullable
    )
    {
        string source = $$"""
                          using System.Collections.Immutable;
                          using Mississippi.Inlet.Generators.Abstractions;
                          using Mississippi.Inlet.Abstractions;

                          namespace TestApp.Domain.Projections.Repeated;

                          public enum Status { Pending = 3, Complete = 7 }
                          public sealed record Entry
                          {
                              public Status First { get; init; }
                              public Status{{(isNullable ? "?" : string.Empty)}} Second { get; init; }
                          }

                          [GenerateProjectionEndpoints]
                          [ProjectionPath("repeated")]
                          public sealed record RepeatedProjection
                          {
                              public ImmutableArray<Entry> Entries { get; init; } = [];
                          }
                          """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, source);
        AssertSuccessfulEnumOutput(output, diagnostics, result, "TestApp.Client.Features.Repeated.Dtos.StatusDto");
        INamedTypeSymbol entry = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName("TestApp.Client.Features.Repeated.Dtos.EntryDto"),
            false);
        Assert.Equal(
            isNullable ? "StatusDto?" : "StatusDto",
            Assert.Single(entry.GetMembers("Second").OfType<IPropertySymbol>())
                .Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Client.Features.Repeated.Dtos.RepeatedProjectionDto"));
    }
}