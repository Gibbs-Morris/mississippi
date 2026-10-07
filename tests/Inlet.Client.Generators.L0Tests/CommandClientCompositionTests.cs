using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Verifies that generated command features compile with their domain composition across namespace layouts.
/// </summary>
public sealed class CommandClientCompositionTests
{
    private const string FrameworkStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              public sealed class GenerateCommandAttribute : System.Attribute { }
                                          }
                                          namespace Microsoft.Extensions.DependencyInjection
                                          {
                                              public interface IServiceCollection { }
                                          }
                                          namespace Mississippi.Common.Abstractions.Mapping
                                          {
                                              public interface IMapper<in TInput, out TOutput> { TOutput Map(TInput input); }
                                              public static class MapperRegistrations
                                              {
                                                  public static void AddMapper<TInput, TOutput, TMapper>(
                                                      this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                                                      where TMapper : class, IMapper<TInput, TOutput> { }
                                              }
                                          }
                                          namespace Mississippi.Reservoir.Abstractions.Actions
                                          {
                                              public interface IAction { }
                                          }
                                          namespace Mississippi.Reservoir.Abstractions.State
                                          {
                                              public interface IFeatureState { static abstract string FeatureKey { get; } }
                                          }
                                          namespace Mississippi.Reservoir.Abstractions
                                          {
                                              public interface IActionEffect<TState> { }
                                              public interface IReservoirBuilder
                                              {
                                                  Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
                                              }
                                              public interface IFeatureBuilder<TState>
                                              {
                                                  IFeatureBuilder<TState> AddReducer<TAction>(System.Func<TState, TAction, TState> reducer)
                                                      where TAction : Actions.IAction;
                                                  IFeatureBuilder<TState> AddActionEffect<TEffect>() where TEffect : class, IActionEffect<TState>;
                                              }
                                              public static class ReservoirRegistrations
                                              {
                                                  public static IReservoirBuilder AddFeatureState<TState>(
                                                      this IReservoirBuilder builder, System.Action<IFeatureBuilder<TState>> configure)
                                                      where TState : class, State.IFeatureState => builder;
                                              }
                                          }
                                          namespace Mississippi.Hosting.Client
                                          {
                                              public sealed class ClientBuilder
                                              {
                                                  public void Reservoir(System.Action<Mississippi.Reservoir.Abstractions.IReservoirBuilder> configure) { }
                                              }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions.Actions
                                          {
                                              public interface ICommandAction : Mississippi.Reservoir.Abstractions.Actions.IAction { }
                                              public interface ICommandExecutingAction<TAction> : Mississippi.Reservoir.Abstractions.Actions.IAction { }
                                              public interface ICommandSucceededAction<TAction> : Mississippi.Reservoir.Abstractions.Actions.IAction { }
                                              public interface ICommandFailedAction<TAction> : Mississippi.Reservoir.Abstractions.Actions.IAction { }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions.State
                                          {
                                              public abstract record AggregateCommandStateBase;
                                              public interface IAggregateCommandState : Mississippi.Reservoir.Abstractions.State.IFeatureState { }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions
                                          {
                                              public static class AggregateCommandStateReducers
                                              {
                                                  public static TState ReduceCommandExecuting<TState, TAction>(TState state, TAction action) => state;
                                                  public static TState ReduceCommandSucceeded<TState, TAction>(TState state, TAction action) => state;
                                                  public static TState ReduceCommandFailed<TState, TAction>(TState state, TAction action) => state;
                                              }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions.ActionEffects
                                          {
                                              public abstract class CommandActionEffectBase<TAction, TDto, TState, TExecuting, TSucceeded, TFailed>
                                                  : Mississippi.Reservoir.Abstractions.IActionEffect<TState>
                                              {
                                                  protected CommandActionEffectBase(
                                                      System.Net.Http.HttpClient httpClient,
                                                      Mississippi.Common.Abstractions.Mapping.IMapper<TAction, TDto> mapper,
                                                      System.TimeProvider? timeProvider) { }
                                                  protected abstract string AggregateRoutePrefix { get; }
                                                  protected abstract string Route { get; }
                                              }
                                          }
                                          """;

    private static void AssertNoCompilationErrors(
        Compilation compilation
    ) =>
        Assert.Empty(
            compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

    private static PortableExecutableReference CompileReference(
        CSharpCompilation compilation
    )
    {
        AssertNoCompilationErrors(compilation);
        using MemoryStream stream = new();
        EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        string source,
        IEnumerable<MetadataReference> references
    ) =>
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

    /// <summary>
    ///     Every accepted command namespace emits a complete, compilable feature and a domain call to that feature.
    /// </summary>
    /// <param name="commandNamespace">The namespace containing the command.</param>
    /// <param name="targetProperty">The target namespace property to configure.</param>
    /// <param name="isReferencedCommand">Whether the command comes from a referenced assembly.</param>
    [Theory]
    [InlineData("TestApp.Aggregates.Account.Commands", "build_property.RootNamespace", false)]
    [InlineData("TestApp.Aggregates.Account.Commands", "build_property.RootNamespace", true)]
    [InlineData("TestApp.Aggregates.Account.Commands", "build_property.AssemblyName", false)]
    [InlineData("TestApp.Aggregates.Account.Commands", "build_property.AssemblyName", true)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account.Commands", "build_property.RootNamespace", false)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account.Commands", "build_property.RootNamespace", true)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account.Commands", "build_property.AssemblyName", false)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account.Commands", "build_property.AssemblyName", true)]
    [InlineData("TestApp.Domain.Aggregates.Account.Commands", "build_property.RootNamespace", false)]
    [InlineData("TestApp.Domain.Aggregates.Account.Commands", "build_property.RootNamespace", true)]
    [InlineData("TestApp.Domain.Aggregates.Account.Commands", "build_property.AssemblyName", false)]
    [InlineData("TestApp.Domain.Aggregates.Account.Commands", "build_property.AssemblyName", true)]
    public void AcceptedCommandNamespacesGenerateCompilableClientComposition(
        string commandNamespace,
        string targetProperty,
        bool isReferencedCommand
    )
    {
        const string targetNamespace = "Consumer.Ui";
        string command = $$"""
                           namespace {{commandNamespace}}
                           {
                               [Mississippi.Inlet.Generators.Abstractions.GenerateCommand]
                               public sealed record OpenAccount(decimal Balance);
                           }
                           """;
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(typeof(HttpClient).Assembly.Location),
        ];
        references.Add(CompileReference(CreateCompilation("Framework", FrameworkStubs, references)));
        if (isReferencedCommand)
        {
            references.Add(CompileReference(CreateCompilation("Input.Domain", command, references)));
            command = string.Empty;
        }

        CSharpCompilation input = CreateCompilation("Unconfigured.Client", command, references);
        AssertNoCompilationErrors(input);
        Dictionary<string, string> options = new()
        {
            [targetProperty] = targetNamespace,
        };
        if (targetProperty == "build_property.RootNamespace")
        {
            options["build_property.AssemblyName"] = "Ignored.Assembly";
        }

        IIncrementalGenerator[] generators =
        [
            new CommandClientDtoGenerator(),
            new CommandClientActionsGenerator(),
            new CommandClientMappersGenerator(),
            new CommandClientActionEffectsGenerator(),
            new CommandClientStateGenerator(),
            new CommandClientReducersGenerator(),
            new CommandClientRegistrationGenerator(),
            new DomainClientRegistrationGenerator(),
        ];
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(generator => generator.AsSourceGenerator()),
            optionsProvider: new TestAnalyzerConfigOptionsProvider(options));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        Assert.Empty(diagnostics);
        Assert.All(runResult.Results, result => Assert.Null(result.Exception));
        Assert.All(
            runResult.Results.Select((result, index) => (generators[index].GetType().Name, result.GeneratedSources)),
            result => Assert.True(
                result.GeneratedSources.Length > 0,
                result.Name + " did not generate its command artifacts."));
        AssertNoCompilationErrors(output);
        Assert.Equal(11, runResult.GeneratedTrees.Length);
        string featureNamespace = targetNamespace + ".Features.AccountAggregate";
        INamedTypeSymbol state = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName(featureNamespace + ".State.AccountAggregateState"),
            false);
        INamedTypeSymbol effect = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName(featureNamespace + ".ActionEffects.OpenAccountActionEffect"),
            false);
        Assert.True(SymbolEqualityComparer.Default.Equals(state, effect.BaseType!.TypeArguments[2]));
        INamedTypeSymbol reducers = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName(featureNamespace + ".Reducers.AccountAggregateReducers"),
            false);
        Assert.Equal(3, reducers.GetMembers().OfType<IMethodSymbol>().Count(method => !method.IsImplicitlyDeclared));
        SyntaxTree domainTree = Assert.Single(
                runResult.Results.SelectMany(result => result.GeneratedSources),
                result => result.HintName == "DomainFeatureRegistrations.g.cs")
            .SyntaxTree;
        InvocationExpressionSyntax featureCall = Assert.Single(
            domainTree.GetRoot(TestContext.Current.CancellationToken)
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>(),
            invocation => invocation.Expression is MemberAccessExpressionSyntax member &&
                          (member.Name.Identifier.ValueText == "AddAccountAggregateFeature"));
        IMethodSymbol registration = Assert.IsType<IMethodSymbol>(
            output.GetSemanticModel(domainTree)
                .GetSymbolInfo(featureCall, TestContext.Current.CancellationToken)
                .Symbol,
            false);
        Assert.Equal(
            featureNamespace + ".AccountAggregateFeatureRegistration",
            registration.ContainingType.ToDisplayString());
    }
}