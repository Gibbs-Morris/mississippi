using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Verifies distinct and compilable server artifacts for commands with reused names.
/// </summary>
public sealed class CommandSourceIdentityTests
{
    private const string FrameworkStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              public sealed class GenerateCommandAttribute : System.Attribute { }
                                              public sealed class GenerateAggregateEndpointsAttribute : System.Attribute { }
                                          }
                                          namespace Microsoft.AspNetCore.Mvc
                                          {
                                              public sealed class RouteAttribute(string template) : System.Attribute { }
                                              public sealed class HttpPostAttribute(string template) : System.Attribute { }
                                              public sealed class FromRouteAttribute : System.Attribute { }
                                              public sealed class FromBodyAttribute : System.Attribute { }
                                              public sealed class ActionResult<T> { }
                                          }
                                          namespace Microsoft.Extensions.Logging
                                          {
                                              public interface ILogger<out T> { }
                                          }
                                          namespace Mississippi.DomainModeling.Abstractions
                                          {
                                              public sealed class OperationResult { }
                                              public interface IGenericAggregateGrain<TAggregate> where TAggregate : class
                                              {
                                                  System.Threading.Tasks.Task<OperationResult> ExecuteAsync(object command, System.Threading.CancellationToken cancellationToken);
                                              }
                                              public interface IAggregateGrainFactory
                                              {
                                                  IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(string entityId) where TAggregate : class;
                                              }
                                          }
                                          namespace Mississippi.DomainModeling.Gateway
                                          {
                                              public abstract class AggregateControllerBase<TAggregate> where TAggregate : class
                                              {
                                                  protected AggregateControllerBase(Microsoft.Extensions.Logging.ILogger<AggregateControllerBase<TAggregate>> logger) { }
                                                  protected System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ActionResult<Mississippi.DomainModeling.Abstractions.OperationResult>> ExecuteAsync<TCommand>(
                                                      string entityId, TCommand command,
                                                      System.Func<string, TCommand, System.Threading.CancellationToken, System.Threading.Tasks.Task<Mississippi.DomainModeling.Abstractions.OperationResult>> serviceMethod,
                                                      System.Threading.CancellationToken cancellationToken) where TCommand : class =>
                                                      System.Threading.Tasks.Task.FromResult(new Microsoft.AspNetCore.Mvc.ActionResult<Mississippi.DomainModeling.Abstractions.OperationResult>());
                                              }
                                          }
                                          namespace System.Text.Json.Serialization
                                          {
                                              public sealed class JsonPropertyNameAttribute(string name) : System.Attribute { }
                                              public sealed class JsonRequiredAttribute : System.Attribute { }
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
                                                  public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMapper<TInput, TOutput, TMapper>(
                                                      this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                                                      where TMapper : IMapper<TInput, TOutput> => services;
                                              }
                                          }
                                          """;

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result)
        RunGenerator(
            string firstAggregate,
            string secondAggregate,
            bool includeControllers = false
        )
    {
        string commands = $$"""
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
        string aggregates = includeControllers
            ? $$"""
                namespace TestApp.Domain.Aggregates.{{firstAggregate}}
                {
                    [Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpoints]
                    public sealed record {{firstAggregate}}Aggregate;
                }
                namespace TestApp.Domain.Aggregates.{{secondAggregate}}
                {
                    [Mississippi.Inlet.Generators.Abstractions.GenerateAggregateEndpoints]
                    public sealed record {{secondAggregate}}Aggregate;
                }
                """
            : string.Empty;
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
        ];
        CSharpCompilation input = CSharpCompilation.Create(
            "TestApp.Server",
            [
                CSharpSyntaxTree.ParseText(FrameworkStubs), CSharpSyntaxTree.ParseText(commands),
                CSharpSyntaxTree.ParseText(aggregates),
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        ISourceGenerator[] generators = includeControllers
            ?
            [
                new CommandServerDtoGenerator().AsSourceGenerator(),
                new AggregateControllerGenerator().AsSourceGenerator(),
                new DomainServerRegistrationGenerator().AsSourceGenerator(),
            ]
            : [new CommandServerDtoGenerator().AsSourceGenerator()];
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics);
        return (output, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Controllers and domain registrations must compile against the distinct command contracts.
    /// </summary>
    /// <param name="firstAggregate">The first aggregate namespace segment.</param>
    /// <param name="secondAggregate">The second aggregate namespace segment.</param>
    /// <param name="firstRoutePrefix">The first aggregate's existing route prefix.</param>
    /// <param name="secondRoutePrefix">The second aggregate's existing route prefix.</param>
    [Theory]
    [InlineData("Account", "Ledger", "account", "ledger")]
    [InlineData("Account", "AccountController", "account", "account-controller")]
    public void ComposedServerGeneratorsKeepControllerAndWireContracts(
        string firstAggregate,
        string secondAggregate,
        string firstRoutePrefix,
        string secondRoutePrefix
    )
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(firstAggregate, secondAggregate, true);
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(9, result.GeneratedTrees.Length);
        Assert.NotNull(output.GetTypeByMetadataName("TestApp.Server.Controllers.Mappers.DomainServerRegistrations"));
        foreach ((string aggregate, string routePrefix) in new[]
                 {
                     (firstAggregate, firstRoutePrefix), (secondAggregate, secondRoutePrefix),
                 })
        {
            INamedTypeSymbol controller = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName($"TestApp.Server.Controllers.Aggregates.{aggregate}Controller"),
                false);
            INamedTypeSymbol dto = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName($"TestApp.Server.Controllers.Aggregates.Commands.{aggregate}.TransferDto"),
                false);
            INamedTypeSymbol command = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName($"TestApp.Domain.Aggregates.{aggregate}.Commands.Transfer"),
                false);
            IMethodSymbol action = Assert.Single(controller.GetMembers("TransferAsync").OfType<IMethodSymbol>());
            Assert.True(SymbolEqualityComparer.Default.Equals(dto, action.Parameters[1].Type));
            INamedTypeSymbol constructorMapper = Assert.IsType<INamedTypeSymbol>(
                Assert.Single(controller.InstanceConstructors).Parameters[1].Type,
                false);
            Assert.True(SymbolEqualityComparer.Default.Equals(dto, constructorMapper.TypeArguments[0]));
            Assert.True(SymbolEqualityComparer.Default.Equals(command, constructorMapper.TypeArguments[1]));
            AttributeData route = Assert.Single(
                controller.GetAttributes(),
                attribute => attribute.AttributeClass?.Name == "RouteAttribute");
            Assert.Equal($"api/aggregates/{routePrefix}/{{entityId}}", route.ConstructorArguments[0].Value);
            AttributeData httpPost = Assert.Single(
                action.GetAttributes(),
                attribute => attribute.AttributeClass?.Name == "HttpPostAttribute");
            Assert.Equal("transfer", httpPost.ConstructorArguments[0].Value);
        }
    }

    /// <summary>
    ///     Reused command names must emit independent DTOs and mappers without losing namespace boundaries.
    /// </summary>
    /// <param name="firstAggregate">The first aggregate namespace segment.</param>
    /// <param name="secondAggregate">The second aggregate namespace segment.</param>
    [Theory]
    [InlineData("Account", "Ledger")]
    [InlineData("A.B", "A_B")]
    public void ReusedCommandNamesGenerateCompilableServerArtifacts(
        string firstAggregate,
        string secondAggregate
    )
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(firstAggregate, secondAggregate);
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(6, result.GeneratedTrees.Length);
        foreach (string aggregate in new[] { firstAggregate, secondAggregate })
        {
            string outputNamespace = $"TestApp.Server.Controllers.Aggregates.Commands.{aggregate}";
            INamedTypeSymbol dto = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(outputNamespace + ".TransferDto"),
                false);
            INamedTypeSymbol mapper = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName(outputNamespace + ".Mappers.TransferDtoMapper"),
                false);
            INamedTypeSymbol command = Assert.IsType<INamedTypeSymbol>(
                output.GetTypeByMetadataName($"TestApp.Domain.Aggregates.{aggregate}.Commands.Transfer"),
                false);
            Assert.True(SymbolEqualityComparer.Default.Equals(dto, mapper.Interfaces[0].TypeArguments[0]));
            Assert.True(SymbolEqualityComparer.Default.Equals(command, mapper.Interfaces[0].TypeArguments[1]));
        }
    }
}