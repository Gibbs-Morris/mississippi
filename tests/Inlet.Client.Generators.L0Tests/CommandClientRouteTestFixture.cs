using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Compiles command effects against real generation attributes and explicit client support stubs.
/// </summary>
internal static class CommandClientRouteTestFixture
{
    /// <summary>
    ///     Minimal client support types for validating compiled generated effects.
    /// </summary>
    private const string ClientSupport = """
                                         namespace Mississippi.Common.Abstractions.Mapping
                                         {
                                             public interface IMapper<TSource, TDestination> { }
                                         }
                                         namespace Mississippi.Inlet.Client.Abstractions.ActionEffects
                                         {
                                             public abstract class CommandActionEffectBase<TAction, TRequest, TState, TExecuting, TSucceeded, TFailed>
                                             {
                                                 protected CommandActionEffectBase(
                                                     System.Net.Http.HttpClient http,
                                                     Mississippi.Common.Abstractions.Mapping.IMapper<TAction, TRequest> mapper,
                                                     System.TimeProvider? timeProvider) { }
                                                 protected abstract string AggregateRoutePrefix { get; }
                                                 protected abstract string Route { get; }
                                             }
                                         }
                                         namespace TestApp.Client.Features.OrderAggregate.Actions
                                         {
                                             public sealed record PlaceOrderAction;
                                             public sealed record PlaceOrderExecutingAction;
                                             public sealed record PlaceOrderSucceededAction;
                                             public sealed record PlaceOrderFailedAction;
                                         }
                                         namespace TestApp.Client.Features.OrderAggregate.Dtos
                                         {
                                             public sealed class PlaceOrderRequestDto { }
                                         }
                                         namespace TestApp.Client.Features.OrderAggregate.State
                                         {
                                             public sealed class OrderAggregateState { }
                                         }
                                         """;

    /// <summary>
    ///     Runs the effect generator with a local or referenced aggregate and validates both compilations.
    /// </summary>
    /// <param name="routePrefix">The configured aggregate route prefix.</param>
    /// <param name="useReferencedAssembly">Whether the domain is supplied as an emitted assembly reference.</param>
    /// <param name="markAggregate">Whether the aggregate has the endpoint generation marker.</param>
    /// <param name="includeSecondMarkedAggregate">Whether the command namespace has ambiguous ownership.</param>
    /// <returns>The generated compilation and its single effect syntax tree.</returns>
    public static (Compilation Compilation, SyntaxTree Effect) Run(
        string? routePrefix,
        bool useReferencedAssembly,
        bool markAggregate = true,
        bool includeSecondMarkedAggregate = false
    )
    {
        string prefixLiteral = routePrefix is null ? "null" : SymbolDisplay.FormatLiteral(routePrefix, true);
        string marker = markAggregate
            ? "[GenerateAggregateEndpoints(RoutePrefix = " + prefixLiteral + ")]"
            : string.Empty;
        string secondAggregate = includeSecondMarkedAggregate
            ? "[GenerateAggregateEndpoints(RoutePrefix = \"other-order\")] public sealed record OtherOrderAggregate;"
            : string.Empty;
        string domainSource = """
                              using Mississippi.Inlet.Generators.Abstractions;
                              namespace TestApp.Domain.Aggregates.Order
                              {
                                  SECOND_AGGREGATE
                                  AGGREGATE_MARKER
                                  public sealed record OrderAggregate;
                              }
                              namespace TestApp.Domain.Aggregates.Order.Commands
                              {
                                  [GenerateCommand(Route = "submit-order")]
                                  public sealed record PlaceOrder;
                              }
                              namespace TestApp.Domain.Aggregates.Other
                              {
                                  [GenerateAggregateEndpoints(RoutePrefix = "unrelated-prefix")]
                                  public sealed record OtherAggregate;
                              }
                              """.Replace("AGGREGATE_MARKER", marker, StringComparison.Ordinal)
            .Replace("SECOND_AGGREGATE", secondAggregate, StringComparison.Ordinal);
        List<MetadataReference> references = GetReferences();
        List<SyntaxTree> trees = [Parse(ClientSupport)];
        if (useReferencedAssembly)
        {
            CSharpCompilation domain = CreateCompilation("TestApp.Domain", [Parse(domainSource)], references);
            Assert.Empty(
                domain.GetDiagnostics(TestContext.Current.CancellationToken)
                    .Where(d => d.Severity == DiagnosticSeverity.Error));
            using MemoryStream stream = new();
            EmitResult emission = domain.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(emission.Success, string.Join(Environment.NewLine, emission.Diagnostics));
            references.Add(MetadataReference.CreateFromImage(stream.ToArray()));
        }
        else
        {
            trees.Add(Parse(domainSource));
        }

        CSharpCompilation input = CreateCompilation("TestApp.Client", trees, references);
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(d => d.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CommandClientActionEffectsGenerator().AsSourceGenerator()],
            parseOptions: new(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(d => d.Severity == DiagnosticSeverity.Error));
        SyntaxTree effect = Assert.Single(driver.GetRunResult().GeneratedTrees);
        return (output, effect);
    }

    /// <summary>
    ///     Creates a nullable-enabled library compilation for the domain or client fixture.
    /// </summary>
    private static CSharpCompilation CreateCompilation(
        string name,
        IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references
    ) =>
        CSharpCompilation.Create(
            name,
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

    /// <summary>
    ///     References framework libraries and the real marker assembly for independent compiler validation.
    /// </summary>
    private static List<MetadataReference> GetReferences()
    {
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Net.Http.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "netstandard.dll")),
            MetadataReference.CreateFromFile(typeof(GenerateAggregateEndpointsAttribute).Assembly.Location),
        ];
    }

    /// <summary>
    ///     Uses the same preview language version for input and generated syntax.
    /// </summary>
    private static SyntaxTree Parse(
        string source
    ) =>
        CSharpSyntaxTree.ParseText(source, new(LanguageVersion.Preview));
}