using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionEndpointsGenerator" />.
/// </summary>
public class ProjectionEndpointsGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute { }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAuthorizationAttribute : Attribute
                                              {
                                                  public string? Policy { get; set; }
                                                  public string? Roles { get; set; }
                                                  public string? AuthenticationSchemes { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAllowAnonymousAttribute : Attribute
                                              {
                                              }
                                          }

                                          namespace Mississippi.Inlet.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class ProjectionPathAttribute : Attribute
                                              {
                                                  public ProjectionPathAttribute(string path) => Path = path;
                                                  public string Path { get; }
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
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];

        // Add netstandard if available (for compatibility)
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

        // Run the generator
        ProjectionEndpointsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated controller should call base constructor correctly.
    /// </summary>
    [Fact]
    public void GeneratedControllerCallsBaseConstructorCorrectly()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(": base(uxProjectionGrainFactory, mapper, logger)", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have constructor with correct dependencies.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectConstructorDependencies()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "IUxProjectionGrainFactory uxProjectionGrainFactory",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "IMapper<AccountBalanceProjection, AccountBalanceDto> mapper",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains("ILogger<AccountBalanceController> logger", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct route attribute.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectRouteAttribute()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Route(\"api/projections/account-balance/{entityId}\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct route for projection path.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectRouteForProjectionPath()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.TransactionHistory
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("transactions/history")]
                                            public sealed record TransactionHistoryProjection
                                            {
                                                public int Count { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Route(\"api/projections/transactions/history/{entityId}\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasXmlDocumentation()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("/// <summary>", controllerSource, StringComparison.Ordinal);
        Assert.Contains("Controller for the AccountBalance projection.", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should inherit from UxProjectionControllerBase.
    /// </summary>
    [Fact]
    public void GeneratedControllerInheritsFromUxProjectionControllerBase()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "UxProjectionControllerBase<AccountBalanceProjection, AccountBalanceDto>",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should be sealed partial class.
    /// </summary>
    [Fact]
    public void GeneratedControllerIsSealedPartialClass()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "public sealed partial class AccountBalanceController",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Collections of custom types should generate nested DTOs and mappers with their properties.
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
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        SyntaxTree nestedDtoTree = Assert.Single(
            runResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("TransactionRecordDto.g.cs", StringComparison.Ordinal));
        string nestedDtoSource = nestedDtoTree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("public sealed record TransactionRecordDto", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("public required decimal Amount { get; init; }", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("public required string Description { get; init; }", nestedDtoSource, StringComparison.Ordinal);
        SyntaxTree nestedMapperTree = Assert.Single(
            runResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("TransactionRecordDtoMapper.g.cs", StringComparison.Ordinal));
        string nestedMapperSource = nestedMapperTree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains(
            "IMapper<TransactionRecord, TransactionRecordDto>",
            nestedMapperSource,
            StringComparison.Ordinal);
        Assert.Contains("Amount = source.Amount", nestedMapperSource, StringComparison.Ordinal);
        Assert.Contains("Description = source.Description", nestedMapperSource, StringComparison.Ordinal);
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TimestampDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("CreatedAt", dtoSource, StringComparison.Ordinal);
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IdentifierDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Id", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle nullable properties.
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("OptionalBalance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("OptionalName", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have JsonRequired attributes.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasJsonRequiredAttributes()
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("[JsonRequired]", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include properties from source projection.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesPropertiesFromProjection()
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
                                                public string AccountName { get; init; } = string.Empty;
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("AccountName", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should be public sealed record with braces syntax.
    /// </summary>
    [Fact]
    public void GeneratedDtoIsPublicSealedRecordWithBraces()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.MultiProperty
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("multi-property")]
                                            public sealed record MultiPropertyProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string Name { get; init; } = string.Empty;
                                                public int Count { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiPropertyDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record MultiPropertyDto", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Name", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Count", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesCorrectNamingConvention()
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record AccountBalanceDto", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use required modifier for properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesRequiredModifierForProperties()
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public required decimal Balance", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Compiled mappers preserve nullable values and framework enum identities at both projection levels.
    /// </summary>
    /// <param name="resumeValue">The nullable source enum value.</param>
    /// <param name="nestedFirst">Whether nested-only discovery precedes the top-level projection.</param>
    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData(7, false)]
    [InlineData(7, true)]
    public void GeneratedEnumMappersCompileAndPreserveValues(
        int? resumeValue,
        bool nestedFirst
    )
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
        const string historyProjectionSource = """
                                               using System.Collections.Immutable;
                                               using Mississippi.Inlet.Generators.Abstractions;
                                               using Mississippi.Inlet.Abstractions;
                                               namespace TestApp.Domain.Projections.Sagas
                                               {
                                                   [GenerateProjectionEndpoints]
                                                   [ProjectionPath("history-only")]
                                                   public sealed record HistoryOnlyProjection
                                                   {
                                                       public ImmutableArray<RecoveryEntry> History { get; init; }
                                                   }
                                               }
                                               """;
        const string mappingSource = """
                                     namespace Microsoft.Extensions.DependencyInjection
                                     {
                                         public interface IServiceCollection { }
                                     }
                                     namespace Mississippi.Common.Abstractions.Mapping
                                     {
                                         public interface IMapper<in TFrom, out TTo> { TTo Map(TFrom input); }
                                         public interface IEnumerableMapper<in TFrom, out TTo>
                                             : IMapper<System.Collections.Generic.IEnumerable<TFrom>,
                                                 System.Collections.Generic.IEnumerable<TTo>> { }
                                         public static class MappingRegistrations
                                         {
                                             public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMapper<TFrom, TTo, TMapper>(
                                                 this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                                                 where TMapper : IMapper<TFrom, TTo> => services;
                                             public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddIEnumerableMapper(
                                                 this Microsoft.Extensions.DependencyInjection.IServiceCollection services) => services;
                                         }
                                     }
                                     namespace TestAssembly.Verification
                                     {
                                         using System;
                                         using System.Collections.Generic;
                                         using System.Collections.Immutable;
                                         using System.Linq;
                                         using Mississippi.Common.Abstractions.Mapping;
                                         using TestApp.Domain.Projections.Sagas;
                                         using TestAssembly.Controllers.Projections;
                                         using TestAssembly.Controllers.Projections.Mappers;

                                         internal sealed class RecoveryEntriesMapper : IEnumerableMapper<RecoveryEntry, RecoveryEntryDto>
                                         {
                                             public IEnumerable<RecoveryEntryDto> Map(IEnumerable<RecoveryEntry> input)
                                                 => input.Select(entry => new RecoveryEntryDtoMapper().Map(entry));
                                         }
                                         public static class EnumMappingProbe
                                         {
                                             public static int?[] Map(int? value)
                                             {
                                                 ResumeSource? resume = value.HasValue ? (ResumeSource)value.Value : null;
                                                 DayOfWeek? day = value.HasValue ? DayOfWeek.Friday : null;
                                                 RecoveryEntry entry = new()
                                                 {
                                                     LastResumeSource = resume,
                                                     Source = ResumeSource.Manual,
                                                     LastDay = day,
                                                     Day = DayOfWeek.Monday,
                                                     LastWorkflow = value.HasValue ? WorkflowState.Ended : null,
                                                     Workflow = WorkflowState.Starting,
                                                 };
                                                 SagaStatusProjection source = new()
                                                 {
                                                     LastResumeSource = resume,
                                                     Source = ResumeSource.Manual,
                                                     LastDay = day,
                                                     Day = DayOfWeek.Monday,
                                                     History = [entry],
                                                 };
                                                 SagaStatusDto dto = new SagaStatusProjectionMapper(
                                                     new ResumeSourceDtoMapper(), new RecoveryEntriesMapper()).Map(source);
                                                 DayOfWeek retainedDay = dto.Day;
                                                 DayOfWeek? retainedLastDay = dto.LastDay;
                                                 return [(int?)dto.LastResumeSource, (int)dto.Source,
                                                     (int?)dto.History[0].LastResumeSource, (int)dto.History[0].Source,
                                                     (int)retainedDay, (int?)retainedLastDay,
                                                     (int)dto.History[0].Day, (int?)dto.History[0].LastDay,
                                                     (int?)dto.History[0].LastWorkflow, (int)dto.History[0].Workflow];
                                             }
                                         }
                                     }
                                     """;
        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            nestedFirst
                ? RunGenerator(AttributeStubs, historyProjectionSource, projectionSource)
                : RunGenerator(AttributeStubs, projectionSource, historyProjectionSource);
        Assert.Empty(diagnostics);
        SyntaxTree[] hostTrees = runResult.GeneratedTrees.Where(tree =>
                tree.FilePath.Contains("Controller.g.cs", StringComparison.Ordinal))
            .ToArray();
        Compilation mappingCompilation = outputCompilation.RemoveSyntaxTrees(hostTrees)
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText(mappingSource, cancellationToken: TestContext.Current.CancellationToken))
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonRequiredAttribute).Assembly.Location));
        Assert.Empty(
            mappingCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Single(
            runResult.GeneratedTrees.Where(tree => tree.FilePath.EndsWith(
                "ResumeSourceDtoMapper.g.cs",
                StringComparison.Ordinal)));
        using MemoryStream assemblyStream = new();
        Assert.True(
            mappingCompilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken).Success);
        assemblyStream.Position = 0;
        AssemblyLoadContext context = new(nameof(GeneratedEnumMappersCompileAndPreserveValues), true);
        try
        {
            Assembly assembly = context.LoadFromStream(assemblyStream);
            MethodInfo map = assembly.GetType("TestAssembly.Verification.EnumMappingProbe")!.GetMethod("Map")!;
            int?[] actual = Assert.IsType<int?[]>(map.Invoke(null, [resumeValue]));
            int? expectedDay = resumeValue.HasValue ? 5 : null;
            int? expectedWorkflow = resumeValue.HasValue ? 9 : null;
            Assert.Equal(
                new[] { resumeValue, 7, resumeValue, 7, 1, expectedDay, 1, expectedDay, expectedWorkflow, 2 },
                actual);
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     Generated files should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveAutoGeneratedHeader()
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
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText(TestContext.Current.CancellationToken).ToString();
            Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated files should have correct namespace transformation.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveCorrectNamespace()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);

        // Output namespace is derived from the compilation target root namespace.
        Assert.Contains("namespace TestAssembly.Controllers.Projections;", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveGeneratedCodeAttribute()
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
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText(TestContext.Current.CancellationToken).ToString();
            Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated files should have nullable enabled.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveNullableEnabled()
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
        IEnumerable<string> generatedCodes = runResult.GeneratedTrees.Select(tree => tree.GetText().ToString());
        foreach (string generatedCode in generatedCodes)
        {
            Assert.Contains("#nullable enable", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated mapper should have Map method with ArgumentNullException check.
    /// </summary>
    [Fact]
    public void GeneratedMapperHasNullCheckInMapMethod()
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("ArgumentNullException.ThrowIfNull(source);", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should implement IMapper interface.
    /// </summary>
    [Fact]
    public void GeneratedMapperImplementsIMapperInterface()
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("IMapper<AccountBalanceProjection, AccountBalanceDto>", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should be internal sealed class.
    /// </summary>
    [Fact]
    public void GeneratedMapperIsInternalSealedClass()
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("internal sealed class AccountBalanceProjectionMapper", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should map properties correctly.
    /// </summary>
    [Fact]
    public void GeneratedMapperMapsPropertiesCorrectly()
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
                                                public string AccountName { get; init; } = string.Empty;
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("Balance = source.Balance", mapperSource, StringComparison.Ordinal);
        Assert.Contains("AccountName = source.AccountName", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper registrations should have correct extension method.
    /// </summary>
    [Fact]
    public void GeneratedMapperRegistrationsHasCorrectExtensionMethod()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains("AddAccountBalanceProjectionMappers", registrationsSource, StringComparison.Ordinal);
        Assert.Contains("this IServiceCollection services", registrationsSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper registrations should register mapper with AddMapper.
    /// </summary>
    [Fact]
    public void GeneratedMapperRegistrationsUsesAddMapper()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<AccountBalanceProjection, AccountBalanceDto, AccountBalanceProjectionMapper>",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mappers also preserve nullable enum values inside nested projection records.
    /// </summary>
    [Fact]
    public void GeneratedNestedProjectionMapperPreservesNullableEnum()
    {
        const string projectionSource = """
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum ResumeSource
                                            {
                                                Reminder = 0,
                                                Manual = 1,
                                            }

                                            public sealed record RecoveryEntry
                                            {
                                                public ResumeSource? LastResumeSource { get; init; }
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public ImmutableArray<RecoveryEntry> Entries { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.EndsWith("RecoveryEntryDtoMapper.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "LastResumeSource = source.LastResumeSource.HasValue ? (ResumeSourceDto?)source.LastResumeSource.Value : null",
            mapperSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection analysis should warn when authentication schemes metadata contains empty entries.
    /// </summary>
    [Fact]
    public void GeneratedProjectionAnalysisWarnsForMalformedAuthenticationSchemesMetadata()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(AuthenticationSchemes = "Bearer,,ApiKey")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Contains(
            runResult.Diagnostics,
            diagnostic => (diagnostic.Id == "INLETAUTH001") && (diagnostic.Severity == DiagnosticSeverity.Warning));
    }

    /// <summary>
    ///     Generated projection controller should include both allow-anonymous and authorize metadata when both are
    ///     configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAllowAnonymousAndAuthorizeWhenBothConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(Policy = "projection-read")]
                                            [GenerateAllowAnonymous]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"projection-read\")]", controllerSource, StringComparison.Ordinal);
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic => diagnostic.Id == "INLETAUTH003");
    }

    /// <summary>
    ///     Generated projection controller should include allow-anonymous metadata when configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAllowAnonymousMetadataWhenConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAllowAnonymous]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection controller should include authorize metadata when configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAuthorizeMetadataWhenConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(Policy = "projection-read", Roles = "reader")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Authorize(Policy = \"projection-read\", Roles = \"reader\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection controller should remain auth-neutral by default.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerRemainsAuthNeutralByDefault()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.DoesNotContain("[Authorize", controllerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTOs should include enum DTOs and mappers for top-level enum properties.
    /// </summary>
    [Fact]
    public void GeneratedProjectionIncludesEnumDtoAndMapper()
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
        string? enumMapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SagaPhaseDtoMapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(enumMapperSource);
        Assert.Contains("IMapper<SagaPhase, SagaPhaseDto>", enumMapperSource, StringComparison.Ordinal);
        string? registrationsSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("ProjectionMapperRegistrations", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<SagaPhase, SagaPhaseDto, SagaPhaseDtoMapper>();",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection DTOs and mappers preserve nullable custom enum properties.
    /// </summary>
    [Fact]
    public void GeneratedProjectionIncludesNullableEnumDtoAndMapper()
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
        string? enumDtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("ResumeSourceDto.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(enumDtoSource);
        Assert.Contains("public enum ResumeSourceDto", enumDtoSource, StringComparison.Ordinal);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("SagaStatusDto.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Nullable<ResumeSourceDto> LastResumeSource", dtoSource, StringComparison.Ordinal);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.EndsWith("SagaStatusProjectionMapper.g.cs", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "LastResumeSource = source.LastResumeSource.HasValue ? (ResumeSourceDto?)source.LastResumeSource.Value : null",
            mapperSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should be internal static class.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsIsInternalStaticClass()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "internal static class AccountBalanceProjectionMapperRegistrations",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should return services for method chaining.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsReturnsServicesForChaining()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains("return services;", registrationsSource, StringComparison.Ordinal);
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

        // Should generate 4 files (DTO, Mapper, Registrations, Controller)
        Assert.Equal(4, runResult.GeneratedTrees.Length);
    }

    /// <summary>
    ///     Generator should ignore static properties.
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("WithStaticDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StaticValue", dtoSource, StringComparison.Ordinal);
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

        // Should generate: DTO, Mapper, MapperRegistrations, Controller
        Assert.Equal(4, runResult.GeneratedTrees.Length);
    }

    /// <summary>
    ///     Mapper namespace should include Mappers suffix.
    /// </summary>
    [Fact]
    public void MapperNamespaceIncludesMappersSuffix()
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "namespace TestAssembly.Controllers.Projections.Mappers;",
            mapperSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple projections should generate separate controllers.
    /// </summary>
    [Fact]
    public void MultipleProjectionsGenerateSeparateControllers()
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

        // Each projection generates 4 files (DTO, Mapper, Registrations, Controller)
        Assert.Equal(8, runResult.GeneratedTrees.Length);
        bool hasAccountBalanceController = runResult.GeneratedTrees.Any(t =>
            t.FilePath.Contains("AccountBalanceController", StringComparison.Ordinal));
        bool hasTransactionHistoryController = runResult.GeneratedTrees.Any(t =>
            t.FilePath.Contains("TransactionHistoryController", StringComparison.Ordinal));
        Assert.True(hasAccountBalanceController);
        Assert.True(hasTransactionHistoryController);
    }
}