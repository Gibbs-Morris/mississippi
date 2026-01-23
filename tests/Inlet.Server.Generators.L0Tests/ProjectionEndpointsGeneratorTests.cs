using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionEndpointsGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Server Generators")]
[AllureSubSuite("Projection Endpoints Generator")]
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
                                          }

                                          namespace Mississippi.Inlet.Projection.Abstractions
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
    ///     Generated controller should have constructor with correct dependencies.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectConstructorDependencies()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Route(\"api/projections/account-balance/{entityId}\")]",
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "public sealed partial class AccountBalanceController",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have JsonRequired attributes.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasJsonRequiredAttributes()
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText()
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
                                        using Mississippi.Inlet.Projection.Abstractions;

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
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("AccountName", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesCorrectNamingConvention()
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
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record AccountBalanceDto", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveAutoGeneratedHeader()
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
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText().ToString();
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
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(controllerSource);

        // Domain → Server.Controllers.Projections
        Assert.Contains(
            "namespace TestApp.Server.Controllers.Projections;",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveGeneratedCodeAttribute()
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
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText().ToString();
            Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText()
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("IMapper<AccountBalanceProjection, AccountBalanceDto>", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper registrations should have correct extension method.
    /// </summary>
    [Fact]
    public void GeneratedMapperRegistrationsHasCorrectExtensionMethod()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText()
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
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<AccountBalanceProjection, AccountBalanceDto, AccountBalanceProjectionMapper>",
            registrationsSource,
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
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "namespace TestApp.Server.Controllers.Projections.Mappers;",
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