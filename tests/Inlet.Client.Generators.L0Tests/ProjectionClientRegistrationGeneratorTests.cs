using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionClientRegistrationGenerator" />.
/// </summary>
public sealed class ProjectionClientRegistrationGeneratorTests
{
    private const string ProjectionStubs = """
                                           namespace Mississippi.Inlet.Abstractions
                                           {
                                               using System;

                                               [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                               public sealed class ProjectionPathAttribute : Attribute
                                               {
                                                   public ProjectionPathAttribute(string path)
                                                   {
                                                   }
                                               }
                                           }

                                           namespace Microsoft.Extensions.DependencyInjection
                                           {
                                           }

                                           namespace Mississippi.Reservoir.Abstractions
                                           {
                                               using System;

                                               public interface IReservoirBuilder
                                               {
                                                   IReservoirBuilder AddFeatureState<TState>(Action<IReservoirFeatureBuilder<TState>> configure)
                                                       where TState : class, new();
                                               }

                                               public interface IReservoirFeatureBuilder<TState>
                                                   where TState : class, new()
                                               {
                                                   IReservoirFeatureBuilder<TState> AddReducer<TAction>(Func<TState, TAction, TState> reduce);
                                               }
                                           }

                                           namespace Mississippi.Inlet.Client.Abstractions.Actions
                                           {
                                               public sealed class ProjectionConnectionChangedAction<T>
                                               {
                                               }

                                               public sealed class ProjectionErrorAction<T>
                                               {
                                               }

                                               public sealed class ProjectionLoadedAction<T>
                                               {
                                               }

                                               public sealed class ProjectionLoadingAction<T>
                                               {
                                               }

                                               public sealed class ProjectionUpdatedAction<T>
                                               {
                                               }
                                           }

                                           namespace Mississippi.Inlet.Client.Abstractions.State
                                           {
                                               public sealed class ProjectionsFeatureState
                                               {
                                               }
                                           }

                                           namespace Mississippi.Inlet.Client.Reducers
                                           {
                                               using Mississippi.Inlet.Client.Abstractions.Actions;
                                               using Mississippi.Inlet.Client.Abstractions.State;

                                               public static class ProjectionsReducer
                                               {
                                                   public static ProjectionsFeatureState ReduceConnectionChanged<T>(
                                                       ProjectionsFeatureState state,
                                                       ProjectionConnectionChangedAction<T> action
                                                   ) => state;

                                                   public static ProjectionsFeatureState ReduceError<T>(
                                                       ProjectionsFeatureState state,
                                                       ProjectionErrorAction<T> action
                                                   ) => state;

                                                   public static ProjectionsFeatureState ReduceLoaded<T>(
                                                       ProjectionsFeatureState state,
                                                       ProjectionLoadedAction<T> action
                                                   ) => state;

                                                   public static ProjectionsFeatureState ReduceLoading<T>(
                                                       ProjectionsFeatureState state,
                                                       ProjectionLoadingAction<T> action
                                                   ) => state;

                                                   public static ProjectionsFeatureState ReduceUpdated<T>(
                                                       ProjectionsFeatureState state,
                                                       ProjectionUpdatedAction<T> action
                                                   ) => state;
                                               }
                                           }
                                           """;

    private static string GetRuntimeAssemblyPath(
        string runtimeDirectory,
        string fileName
    ) =>
        runtimeDirectory + Path.DirectorySeparatorChar + fileName;

    private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult
        RunResult) RunGenerator(
            params string[] sources
        )
    {
        SyntaxTree[] syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source)).ToArray();
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(GetRuntimeAssemblyPath(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(GetRuntimeAssemblyPath(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(GetRuntimeAssemblyPath(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];
        string netstandardPath = GetRuntimeAssemblyPath(runtimeDirectory, "netstandard.dll");
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
        ProjectionClientRegistrationGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated projection registrations should compile without relying on implicit usings.
    /// </summary>
    [Fact]
    public void GeneratedProjectionRegistrationCompilesWithoutImplicitUsings()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceDto;
                                        }
                                        """;
        (Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(ProjectionStubs, projectionSource);
        string generatedCode = runResult.GeneratedTrees.Single().GetText().ToString();
        Diagnostic[] relevantDiagnostics =
        [
            .. diagnostics.Concat(outputCompilation.GetDiagnostics())
                .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error),
        ];
        Assert.Contains(
            "global::System.ArgumentNullException.ThrowIfNull(builder);",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Empty(relevantDiagnostics);
    }
}