using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Verifies that command artifacts with reused names remain distinct and compile together.
/// </summary>
public sealed class CommandSourceIdentityTests
{
    private const string FrameworkStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              public sealed class GenerateCommandAttribute : System.Attribute { }
                                          }
                                          namespace Mississippi.Common.Abstractions.Mapping
                                          {
                                              public interface IMapper<in TInput, out TOutput> { TOutput Map(TInput input); }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions.Actions
                                          {
                                              public interface ICommandAction { }
                                              public interface ICommandExecutingAction<TAction> { }
                                              public interface ICommandSucceededAction<TAction> { }
                                              public interface ICommandFailedAction<TAction> { }
                                          }
                                          namespace Mississippi.Inlet.Client.Abstractions.ActionEffects
                                          {
                                              public abstract class CommandActionEffectBase<TAction, TDto, TState, TExecuting, TSucceeded, TFailed>
                                              {
                                                  protected CommandActionEffectBase(
                                                      System.Net.Http.HttpClient httpClient,
                                                      Mississippi.Common.Abstractions.Mapping.IMapper<TAction, TDto> mapper,
                                                      System.TimeProvider? timeProvider) { }
                                                  protected abstract string AggregateRoutePrefix { get; }
                                                  protected abstract string Route { get; }
                                              }
                                          }
                                          namespace TestApp.Client.Features.AccountAggregate.State
                                          {
                                              internal sealed record AccountAggregateState;
                                          }
                                          namespace TestApp.Client.Features.LedgerAggregate.State
                                          {
                                              internal sealed record LedgerAggregateState;
                                          }
                                          """;

    private static string CreateCommands(
        string firstAggregate,
        string secondAggregate
    ) =>
        $$"""
          namespace TestApp.Domain.Aggregates.{{firstAggregate}}.Commands
          {
              [Mississippi.Inlet.Generators.Abstractions.GenerateCommand]
              public sealed record Transfer(decimal Amount);
          }
          namespace TestApp.Domain.Aggregates.{{secondAggregate}}.Commands
          {
              [Mississippi.Inlet.Generators.Abstractions.GenerateCommand]
              public sealed record Transfer(int Entries);
          }
          """;

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result)
        RunGenerators(
            string commands,
            params IIncrementalGenerator[] generators
        )
    {
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(HttpClient).Assembly.Location),
        ];
        CSharpCompilation input = CSharpCompilation.Create(
            "TestApp.Client",
            [CSharpSyntaxTree.ParseText(FrameworkStubs), CSharpSyntaxTree.ParseText(commands)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver =
            CSharpGeneratorDriver.Create(generators.Select(generator => generator.AsSourceGenerator()));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics);
        return (output, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Namespace separators and underscores must retain distinct DTO source identities.
    /// </summary>
    /// <param name="firstAggregate">The first aggregate namespace segment.</param>
    /// <param name="secondAggregate">The second aggregate namespace segment.</param>
    [Theory]
    [InlineData("Account", "Ledger")]
    [InlineData("A.B", "A_B")]
    public void DtoSourcesKeepAggregateNamespaceIdentity(
        string firstAggregate,
        string secondAggregate
    )
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) = RunGenerators(
            CreateCommands(firstAggregate, secondAggregate),
            new CommandClientDtoGenerator());
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(2, result.GeneratedTrees.Length);
        INamedTypeSymbol first = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName($"TestApp.Client.Features.{firstAggregate}Aggregate.Dtos.TransferRequestDto"),
            false);
        INamedTypeSymbol second = Assert.IsType<INamedTypeSymbol>(
            output.GetTypeByMetadataName($"TestApp.Client.Features.{secondAggregate}Aggregate.Dtos.TransferRequestDto"),
            false);
        Assert.Equal(
            SpecialType.System_Decimal,
            Assert.Single(first.GetMembers("Amount").OfType<IPropertySymbol>()).Type.SpecialType);
        Assert.Equal(
            SpecialType.System_Int32,
            Assert.Single(second.GetMembers("Entries").OfType<IPropertySymbol>()).Type.SpecialType);
    }

    /// <summary>
    ///     Each command's DTO, actions, mapper and effect must compile and retain the matching payload.
    /// </summary>
    [Fact]
    public void ReusedCommandNamesGenerateCompilableClientArtifacts()
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) = RunGenerators(
            CreateCommands("Account", "Ledger"),
            new CommandClientDtoGenerator(),
            new CommandClientActionsGenerator(),
            new CommandClientMappersGenerator(),
            new CommandClientActionEffectsGenerator());
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(14, result.GeneratedTrees.Length);
        foreach (string aggregate in new[] { "Account", "Ledger" })
        {
            string featureNamespace = $"TestApp.Client.Features.{aggregate}Aggregate";
            INamedTypeSymbol dto = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(featureNamespace + ".Dtos.TransferRequestDto"),
                false);
            INamedTypeSymbol action = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(featureNamespace + ".Actions.TransferAction"),
                false);
            INamedTypeSymbol mapper = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(featureNamespace + ".Mappers.TransferActionMapper"),
                false);
            INamedTypeSymbol effect = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(featureNamespace + ".ActionEffects.TransferActionEffect"),
                false);
            Assert.True(SymbolEqualityComparer.Default.Equals(action, mapper.Interfaces[0].TypeArguments[0]));
            Assert.True(SymbolEqualityComparer.Default.Equals(dto, mapper.Interfaces[0].TypeArguments[1]));
            Assert.True(SymbolEqualityComparer.Default.Equals(action, effect.BaseType!.TypeArguments[0]));
            Assert.True(SymbolEqualityComparer.Default.Equals(dto, effect.BaseType.TypeArguments[1]));
        }
    }
}