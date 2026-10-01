using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

    /// <summary>
    ///     DTO names whose compiled enum identities are verified together.
    /// </summary>
    private static readonly string[] MixedEnumDtoNames = ["SagaStatusProjectionDto", "RecoveryEntryDto"];

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
    ///     Generated projection DTOs preserve nullable custom enum properties and emit their enum DTO.
    /// </summary>
    [Fact]
    public void GeneratedDtoGeneratesNullableTopLevelEnumDto()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum ResumeSource
                                            {
                                                Reminder = 0,
                                                Manual = 1,
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public ResumeSource? LastResumeSource { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        string? dtoSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.EndsWith("SagaStatusProjectionDto.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Nullable<ResumeSourceDto> LastResumeSource", dtoSource, StringComparison.Ordinal);
        string? enumDtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("ResumeSourceDto.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(enumDtoSource);
        Assert.Contains("public enum ResumeSourceDto", enumDtoSource, StringComparison.Ordinal);
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
    ///     Different source symbols that normalize to one DTO name produce an actionable error.
    /// </summary>
    /// <param name="separateProjections">Whether the conflicting properties belong to separate projections.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedDtoNameCollisionProducesDiagnostic(
        bool separateProjections
    )
    {
        const string projectionTemplate = """
                                          using System.Collections.Immutable;
                                          using Mississippi.Inlet.Generators.Abstractions;
                                          using Mississippi.Inlet.Abstractions;

                                          namespace TestApp.Domain.Projections.Sagas
                                          {
                                              public enum WorkflowState { Starting = 2, Ended = 9 }
                                              public sealed record Workflow(string Name);
                                              [GenerateProjectionEndpoints]
                                              [ProjectionPath("workflow")]
                                              public sealed record WorkflowProjection
                                              {
                                                  public WorkflowState? State { get; init; }
                                                  __HISTORY__
                                              }
                                              __SECOND_PROJECTION__
                                          }
                                          """;
        const string secondProjection = """
                                        [GenerateProjectionEndpoints]
                                        [ProjectionPath("history")]
                                        public sealed record HistoryProjection
                                        {
                                            public ImmutableArray<Workflow> History { get; init; }
                                        }
                                        """;
        const string historyProperty = "public ImmutableArray<Workflow> History { get; init; }";
        string projectionSource = projectionTemplate
            .Replace("__HISTORY__", separateProjections ? string.Empty : historyProperty, StringComparison.Ordinal)
            .Replace(
                "__SECOND_PROJECTION__",
                separateProjections ? secondProjection : string.Empty,
                StringComparison.Ordinal);
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Diagnostic collision = Assert.Single(diagnostics);
        Assert.Equal("INLETDTO001", collision.Id);
        Assert.Equal(DiagnosticSeverity.Error, collision.Severity);
        string message = collision.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("TestApp.Client.Features.Sagas.Dtos.WorkflowDto", message, StringComparison.Ordinal);
        Assert.Contains("global::TestApp.Domain.Projections.Sagas.WorkflowState", message, StringComparison.Ordinal);
        Assert.Contains("global::TestApp.Domain.Projections.Sagas.Workflow", message, StringComparison.Ordinal);
        Assert.True(collision.Location.IsInSource);
        Assert.All(runResult.Results, result => Assert.Null(result.Exception));
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
    ///     Each feature receives its own enum and nested DTO declarations with unique source hints.
    /// </summary>
    /// <param name="shareGeneratedNames">Whether both features use the same projection and history names.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEnumDtosCompileAcrossFeatureNamespaces(
        bool shareGeneratedNames
    )
    {
        const string projectionTemplate = """
                                          using System.Collections.Immutable;
                                          using Mississippi.Inlet.Generators.Abstractions;
                                          using Mississippi.Inlet.Abstractions;
                                          using TestApp.Domain.Shared;

                                          namespace TestApp.Domain.Shared
                                          {
                                              public enum SharedSourceState { Reminder = 0, Manual = 7 }
                                              public sealed record FirstEntry
                                              {
                                                  public SharedSourceState? Kind { get; init; }
                                              }
                                              public sealed record SecondEntry
                                              {
                                                  public SharedSourceState? Kind { get; init; }
                                              }
                                          }
                                          namespace TestApp.Domain.Projections.First
                                          {
                                              [GenerateProjectionEndpoints]
                                              [ProjectionPath("first")]
                                              public sealed record FirstProjection
                                              {
                                                  public ImmutableArray<FirstEntry> History { get; init; }
                                              }
                                          }
                                          namespace TestApp.Domain.Projections.Second
                                          {
                                              [GenerateProjectionEndpoints]
                                              [ProjectionPath("second")]
                                              public sealed record __SecondProjection__
                                              {
                                                  public ImmutableArray<__SecondEntry__> History { get; init; }
                                              }
                                          }
                                          """;
        string secondProjection = shareGeneratedNames ? "FirstProjection" : "SecondProjection";
        string secondEntry = shareGeneratedNames ? "FirstEntry" : "SecondEntry";
        string projectionSource = projectionTemplate
            .Replace("__SecondProjection__", secondProjection, StringComparison.Ordinal)
            .Replace("__SecondEntry__", secondEntry, StringComparison.Ordinal);
        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        Assert.Empty(
            outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        using MemoryStream assembly = new();
        Assert.True(outputCompilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken).Success);
        Assert.Equal(6, runResult.GeneratedTrees.Length);
        foreach ((string feature, string projection, string entry) in new[]
                 {
                     ("First", "FirstProjection", "FirstEntry"), ("Second", secondProjection, secondEntry),
                 })
        {
            string clientNamespace = $"TestApp.Client.Features.{feature}.Dtos";
            INamedTypeSymbol enumDto = outputCompilation.GetTypeByMetadataName($"{clientNamespace}.SharedSourceDto")!;
            Assert.NotNull(enumDto);
            INamedTypeSymbol entryDto = outputCompilation.GetTypeByMetadataName($"{clientNamespace}.{entry}Dto")!;
            Assert.NotNull(entryDto);
            IPropertySymbol kind = Assert.Single(entryDto.GetMembers("Kind").OfType<IPropertySymbol>());
            INamedTypeSymbol nullableKind = Assert.IsType<INamedTypeSymbol>(kind.Type, false);
            Assert.Equal(SpecialType.System_Nullable_T, nullableKind.OriginalDefinition.SpecialType);
            Assert.True(SymbolEqualityComparer.Default.Equals(enumDto, Assert.Single(nullableKind.TypeArguments)));
            foreach (string dtoName in new[] { projection + "Dto", entry + "Dto", "SharedSourceDto" })
            {
                Assert.Single(
                    runResult.GeneratedTrees.Where(tree =>
                        Path.GetFileName(tree.FilePath) == $"{clientNamespace}.{dtoName}.g.cs"));
            }
        }
    }

    /// <summary>
    ///     Generated DTOs compile when nullable and non-nullable enums occur on the projection and its history.
    /// </summary>
    [Fact]
    public void GeneratedEnumDtosCompileWithSharedAndFrameworkEnums()
    {
        const string projectionSource = """
                                        using System;
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum ResumeSource { Reminder = 0, Manual = 7 }
                                            public enum WorkflowState { Starting = 2, Ended = 9 }
                                            public sealed record RecoveryEntry
                                            {
                                                public ResumeSource? LastResumeSource { get; init; }
                                                public ResumeSource Source { get; init; }
                                                public DayOfWeek? LastDay { get; init; }
                                                public DayOfWeek Day { get; init; }
                                                public WorkflowState? LastWorkflow { get; init; }
                                                public WorkflowState Workflow { get; init; }
                                            }
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public ResumeSource? LastResumeSource { get; init; }
                                                public ResumeSource Source { get; init; }
                                                public DayOfWeek? LastDay { get; init; }
                                                public DayOfWeek Day { get; init; }
                                                public ImmutableArray<RecoveryEntry> History { get; init; }
                                            }
                                        }
                                        """;
        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        Assert.Empty(
            outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        using MemoryStream assembly = new();
        Assert.True(outputCompilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken).Success);
        INamedTypeSymbol frameworkEnum = outputCompilation.GetTypeByMetadataName("System.DayOfWeek")!;
        foreach (INamedTypeSymbol dto in MixedEnumDtoNames.Select(dtoName => Assert.Single(
                     outputCompilation.GetSymbolsWithName(
                             dtoName,
                             SymbolFilter.Type,
                             TestContext.Current.CancellationToken)
                         .OfType<INamedTypeSymbol>())))
        {
            IPropertySymbol day = Assert.Single(dto.GetMembers("Day").OfType<IPropertySymbol>());
            IPropertySymbol lastDay = Assert.Single(dto.GetMembers("LastDay").OfType<IPropertySymbol>());
            INamedTypeSymbol nullableDay = Assert.IsType<INamedTypeSymbol>(lastDay.Type, false);
            Assert.True(SymbolEqualityComparer.Default.Equals(frameworkEnum, day.Type));
            Assert.True(SymbolEqualityComparer.Default.Equals(frameworkEnum, Assert.Single(nullableDay.TypeArguments)));
        }

        Assert.Single(
            runResult.GeneratedTrees.Where(tree => tree.FilePath.EndsWith(
                ".ResumeSourceDto.g.cs",
                StringComparison.Ordinal)));
        Assert.Single(
            runResult.GeneratedTrees.Where(tree => tree.FilePath.EndsWith(
                ".WorkflowDto.g.cs",
                StringComparison.Ordinal)));
    }

    /// <summary>
    ///     Projection DTOs retain discovery metadata and nested dependencies in both discovery orders.
    /// </summary>
    /// <param name="nestedFirst">Whether the containing projection is discovered first.</param>
    /// <param name="projectionSuffix">Whether the projection uses the conventional name suffix.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void GeneratedNestedProjectionDtosKeepDiscoveryMetadata(
        bool nestedFirst,
        bool projectionSuffix
    )
    {
        const string containingProjection = """
                                            using System.Collections.Immutable;
                                            using Mississippi.Inlet.Generators.Abstractions;
                                            using Mississippi.Inlet.Abstractions;

                                            namespace TestApp.Domain.Projections.Banking
                                            {
                                                [GenerateProjectionEndpoints]
                                                [ProjectionPath("ledger")]
                                                public sealed record LedgerProjection
                                                {
                                                    public ImmutableArray<BalanceProjection> Balances { get; init; }
                                                }
                                            }
                                            """;
        const string nestedProjection = """
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Banking
                                        {
                                            public enum ResumeSource { Manual = 7 }
                                            public sealed record BalanceEntry
                                            {
                                                public ResumeSource? Source { get; init; }
                                            }
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("balance")]
                                            public sealed record BalanceProjection
                                            {
                                                public ImmutableArray<BalanceEntry> Entries { get; init; }
                                            }
                                        }
                                        """;
        string containingSource = projectionSuffix
            ? containingProjection
            : containingProjection.Replace("BalanceProjection", "Balance", StringComparison.Ordinal);
        string nestedSource = projectionSuffix
            ? nestedProjection
            : nestedProjection.Replace("BalanceProjection", "Balance", StringComparison.Ordinal);
        string projectionDtoName = projectionSuffix ? "BalanceProjectionDto" : "BalanceDto";
        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            nestedFirst
                ? RunGenerator(AttributeStubs, containingSource, nestedSource)
                : RunGenerator(AttributeStubs, nestedSource, containingSource);
        Assert.Empty(diagnostics);
        const string clientNamespace = "TestApp.Client.Features.Banking.Dtos";
        INamedTypeSymbol projectionDto = outputCompilation.GetTypeByMetadataName(
            clientNamespace + "." + projectionDtoName)!;
        Assert.NotNull(projectionDto);
        AttributeData projectionPath = Assert.Single(
            projectionDto.GetAttributes()
                .Where(attribute => attribute.AttributeClass?.ToDisplayString() ==
                                    "Mississippi.Inlet.Abstractions.ProjectionPathAttribute"));
        Assert.Equal("balance", projectionPath.ConstructorArguments[0].Value);
        Assert.Single(
            runResult.GeneratedTrees.Where(tree => tree.FilePath.EndsWith(
                "." + projectionDtoName + ".g.cs",
                StringComparison.Ordinal)));
        Assert.NotNull(outputCompilation.GetTypeByMetadataName(clientNamespace + ".BalanceEntryDto"));
        Assert.NotNull(outputCompilation.GetTypeByMetadataName(clientNamespace + ".ResumeSourceDto"));
        Assert.Empty(
            outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        using MemoryStream assemblyStream = new();
        Assert.True(
            outputCompilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken).Success);
    }

    /// <summary>
    ///     Nullable custom enum collection elements receive enum DTO declarations while framework enums remain unchanged.
    /// </summary>
    /// <param name="collectionType">The collection shape containing nullable enum elements.</param>
    /// <param name="nested">Whether only a nested history record contains the collection.</param>
    [Theory]
    [InlineData("ImmutableArray<__Element__>", false)]
    [InlineData("List<__Element__>", false)]
    [InlineData("__Element__[]", false)]
    [InlineData("ImmutableArray<__Element__>", true)]
    [InlineData("List<__Element__>", true)]
    [InlineData("__Element__[]", true)]
    public void GeneratedNullableEnumCollectionDtosCompile(
        string collectionType,
        bool nested
    )
    {
        ArgumentNullException.ThrowIfNull(collectionType);
        const string projectionTemplate = """
                                          using System;
                                          using System.Collections.Generic;
                                          using System.Collections.Immutable;
                                          using Mississippi.Inlet.Generators.Abstractions;
                                          using Mississippi.Inlet.Abstractions;
                                          using TestApp.Domain.Shared;

                                          namespace TestApp.Domain.Shared
                                          {
                                              public enum ResumeSource { Reminder = 0, Manual = 7 }
                                          }
                                          namespace TestApp.Domain.Projections.Sagas
                                          {
                                              [GenerateProjectionEndpoints]
                                              [ProjectionPath("saga-status")]
                                              public sealed record SagaStatusProjection
                                              {
                                                  public __CustomCollection__ Values { get; init; }
                                                  public __CustomCollection__ Again { get; init; }
                                                  public __FrameworkCollection__ Days { get; init; }
                                              }
                                          }
                                          """;
        string projectionSource = projectionTemplate
            .Replace(
                "__CustomCollection__",
                collectionType.Replace("__Element__", "ResumeSource?", StringComparison.Ordinal),
                StringComparison.Ordinal)
            .Replace(
                "__FrameworkCollection__",
                collectionType.Replace("__Element__", "DayOfWeek?", StringComparison.Ordinal),
                StringComparison.Ordinal);
        if (nested)
        {
            projectionSource = projectionSource
                .Replace("[GenerateProjectionEndpoints]", string.Empty, StringComparison.Ordinal)
                .Replace("[ProjectionPath(\"saga-status\")]", string.Empty, StringComparison.Ordinal)
                .Replace("SagaStatusProjection", "RecoveryEntry", StringComparison.Ordinal);
            projectionSource += """
                                namespace TestApp.Domain.Projections.Sagas
                                {
                                    [GenerateProjectionEndpoints]
                                    [ProjectionPath("saga-status")]
                                    public sealed record SagaStatusProjection
                                    {
                                        public ImmutableArray<RecoveryEntry> History { get; init; }
                                    }
                                }
                                """;
        }

        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        Assert.Empty(
            outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        using MemoryStream assembly = new();
        Assert.True(outputCompilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken).Success);
        const string dtoNamespace = "TestApp.Client.Features.Sagas.Dtos";
        INamedTypeSymbol enumDto = outputCompilation.GetTypeByMetadataName($"{dtoNamespace}.ResumeSourceDto")!;
        Assert.NotNull(enumDto);
        Assert.Equal(TypeKind.Enum, enumDto.TypeKind);
        string targetDtoName = nested ? "RecoveryEntryDto" : "SagaStatusProjectionDto";
        INamedTypeSymbol projectionDto = outputCompilation.GetTypeByMetadataName($"{dtoNamespace}.{targetDtoName}")!;
        Assert.NotNull(projectionDto);
        foreach (string propertyName in new[] { "Values", "Again", "Days" })
        {
            IPropertySymbol property = Assert.Single(projectionDto.GetMembers(propertyName).OfType<IPropertySymbol>());
            ITypeSymbol element = property.Type is IArrayTypeSymbol array
                ? array.ElementType
                : Assert.Single(Assert.IsType<INamedTypeSymbol>(property.Type, false).TypeArguments);
            INamedTypeSymbol nullableElement = Assert.IsType<INamedTypeSymbol>(element, false);
            Assert.Equal(SpecialType.System_Nullable_T, nullableElement.OriginalDefinition.SpecialType);
            ITypeSymbol expectedElement = propertyName == "Days"
                ? outputCompilation.GetTypeByMetadataName("System.DayOfWeek")!
                : enumDto;
            Assert.True(
                SymbolEqualityComparer.Default.Equals(expectedElement, Assert.Single(nullableElement.TypeArguments)));
        }

        Assert.Single(
            runResult.GeneratedTrees.Where(tree => tree.FilePath.EndsWith(
                ".ResumeSourceDto.g.cs",
                StringComparison.Ordinal)));
        Assert.DoesNotContain(
            runResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith(".DayOfWeekDto.g.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     A projection and an enum that normalize to one DTO name produce a diagnostic without generator failure.
    /// </summary>
    [Fact]
    public void GeneratedProjectionDtoNameCollisionProducesDiagnostic()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum WorkflowProjectionState { Starting = 2, Ended = 9 }
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("workflow")]
                                            public sealed record WorkflowProjection
                                            {
                                                public WorkflowProjectionState? State { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Diagnostic collision = Assert.Single(diagnostics);
        Assert.Equal("INLETDTO001", collision.Id);
        Assert.Equal(DiagnosticSeverity.Error, collision.Severity);
        string message = collision.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("TestApp.Client.Features.Sagas.Dtos.WorkflowProjectionDto", message, StringComparison.Ordinal);
        Assert.Contains(
            "global::TestApp.Domain.Projections.Sagas.WorkflowProjection",
            message,
            StringComparison.Ordinal);
        Assert.Contains(
            "global::TestApp.Domain.Projections.Sagas.WorkflowProjectionState",
            message,
            StringComparison.Ordinal);
        Assert.All(runResult.Results, result => Assert.Null(result.Exception));
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
}