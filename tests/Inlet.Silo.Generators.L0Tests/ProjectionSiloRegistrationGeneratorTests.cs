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
///     Tests for <see cref="ProjectionSiloRegistrationGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Silo Generators")]
[AllureSubSuite("Projection Silo Registration Generator")]
public class ProjectionSiloRegistrationGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeAndBaseStubs = """
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

                                                 namespace Mississippi.EventSourcing.Reducers.Abstractions
                                                 {
                                                     public abstract class EventReducerBase<TEvent, TProjection>
                                                         where TProjection : class
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

        // Use "TestApp.Silo" as assembly name - the generator will use this as the target root namespace
        // when no RootNamespace MSBuild property is available
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Silo",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        ProjectionSiloRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated class should have correct extension method name.
    /// </summary>
    [Fact]
    public void GeneratedClassHasCorrectExtensionMethodName()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "public static IServiceCollection AddAccountBalanceProjection(",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated class name should follow naming convention.
    /// </summary>
    [Fact]
    public void GeneratedClassNameFollowsNamingConvention()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "public static class AccountBalanceProjectionRegistrations",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFileHasAutoGeneratedHeader()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have correct namespace transformation.
    /// </summary>
    [Fact]
    public void GeneratedFileHasCorrectNamespace()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // Domain.Projections.* → Silo.Registrations
        Assert.Contains("namespace TestApp.Silo.Registrations;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFileHasGeneratedCodeAttribute()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated file should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedFileHasXmlDocumentation()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("/// <summary>", generatedCode, StringComparison.Ordinal);
        Assert.Contains(
            "Extension methods for registering AccountBalance projection services.",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should add snapshot state converter.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsAddsSnapshotStateConverter()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "services.AddSnapshotStateConverter<AccountBalanceProjection>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should call AddUxProjections.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsCallsAddUxProjections()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("services.AddUxProjections();", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsFileHasCorrectName()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        Assert.Contains(
            "AccountBalanceProjectionRegistrations.g.cs",
            runResult.GeneratedTrees[0].FilePath,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should handle multiple reducers for same projection.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsHandlesMultipleReducers()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceIncreased { }
                                            public sealed record BalanceDecreased { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceIncreasedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceIncreased, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }

                                            public class BalanceDecreasedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceDecreased, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();

        // Both reducers should be registered
        Assert.Contains(
            "services.AddReducer<BalanceIncreased, AccountBalanceProjection, BalanceIncreasedReducer>();",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "services.AddReducer<BalanceDecreased, AccountBalanceProjection, BalanceDecreasedReducer>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should include using statements for namespaces.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsIncludesUsingStatements()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Reducers;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.Snapshots;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("using Mississippi.EventSourcing.UxProjections;", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should register reducers.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsRegistersReducers()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains(
            "services.AddReducer<BalanceUpdated, AccountBalanceProjection, BalanceUpdatedReducer>();",
            generatedCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should return services for chaining.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsReturnsServicesForChaining()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("return services;", generatedCode, StringComparison.Ordinal);
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
            RunGenerator(AttributeAndBaseStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce no output when projection has no reducers.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenProjectionHasNoReducers()
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
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce output when projection has reducers.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWhenProjectionHasReducers()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        Assert.Single(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Multiple projections should generate separate registration files.
    /// </summary>
    [Fact]
    public void MultipleProjectionsGenerateSeparateRegistrationFiles()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Projection.Abstractions;
                                        using Mississippi.EventSourcing.Reducers.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Events
                                        {
                                            public sealed record BalanceUpdated { }
                                        }

                                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                                        {
                                            public class BalanceUpdatedReducer : EventReducerBase<TestApp.Domain.Projections.AccountBalance.Events.BalanceUpdated, TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection>
                                            {
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

                                        namespace TestApp.Domain.Projections.TransactionHistory.Events
                                        {
                                            public sealed record TransactionAdded { }
                                        }

                                        namespace TestApp.Domain.Projections.TransactionHistory.Reducers
                                        {
                                            public class TransactionAddedReducer : EventReducerBase<TestApp.Domain.Projections.TransactionHistory.Events.TransactionAdded, TestApp.Domain.Projections.TransactionHistory.TransactionHistoryProjection>
                                            {
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeAndBaseStubs, projectionSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasAccountBalanceRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "AccountBalanceProjectionRegistrations",
            StringComparison.Ordinal));
        bool hasTransactionHistoryRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "TransactionHistoryProjectionRegistrations",
            StringComparison.Ordinal));
        Assert.True(hasAccountBalanceRegistrations);
        Assert.True(hasTransactionHistoryRegistrations);
    }
}