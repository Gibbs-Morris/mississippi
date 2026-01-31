using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaControllerGenerator" />.
/// </summary>
public class SagaControllerGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                                  public Type? InputType { get; set; }
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Sagas.Abstractions
                                          {
                                              public interface ISagaDefinition
                                              {
                                                  static abstract string SagaName { get; }
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

        // Use "TestApp.Server" as assembly name - the generator will use this as the target root namespace
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        SagaControllerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Controller should have Start and GetStatus endpoints.
    /// </summary>
    [Fact]
    public void ControllerHasStartAndGetStatusEndpoints()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedSource = runResult.GeneratedTrees[0].GetText().ToString();

        // Verify endpoints
        Assert.Contains("[HttpPost(\"{sagaId:guid}\")]", generatedSource, StringComparison.Ordinal);
        Assert.Contains("public async Task<ActionResult> StartAsync", generatedSource, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{sagaId:guid}/status\")]", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "public async Task<ActionResult<SagaStatusProjection>> GetStatusAsync",
            generatedSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Controller should inject ISagaOrchestrator, IMapper, and ILogger.
    /// </summary>
    [Fact]
    public void ControllerInjectsDependencies()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedSource = runResult.GeneratedTrees[0].GetText().ToString();

        // Verify dependencies
        Assert.Contains("ISagaOrchestrator orchestrator", generatedSource, StringComparison.Ordinal);
        Assert.Contains("IMapper<", generatedSource, StringComparison.Ordinal);
        Assert.Contains("ILogger<TransferFundsSagaController> logger", generatedSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Saga without ISagaDefinition should not generate controller.
    /// </summary>
    [Fact]
    public void DoesNotGenerateControllerForNonSagaDefinition()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     No controller should be generated when InputType is not specified.
    /// </summary>
    [Fact]
    public void DoesNotGenerateControllerWhenInputTypeNotSpecified()
    {
        const string sagaSource = """
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      [GenerateSagaEndpoints]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Controller should be generated for saga with GenerateSagaEndpoints and InputType.
    /// </summary>
    [Fact]
    public void GeneratesControllerForSagaWithInputType()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required string SourceAccountId { get; init; }
                                          public required string DestinationAccountId { get; init; }
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Single(runResult.GeneratedTrees);
        bool hasController = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "TransferFundsSagaController",
            StringComparison.Ordinal));
        Assert.True(hasController, "Expected TransferFundsSagaController to be generated");
    }

    /// <summary>
    ///     Multiple sagas should generate multiple controllers.
    /// </summary>
    [Fact]
    public void GeneratesMultipleControllersForMultipleSagas()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }

                                  namespace TestApp.Domain.Sagas.AccountSetup
                                  {
                                      public sealed record AccountSetupSagaInput
                                      {
                                          public required string AccountName { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(AccountSetupSagaInput))]
                                      public sealed record AccountSetupSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "AccountSetup";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasTransferFunds = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "TransferFundsSagaController",
            StringComparison.Ordinal));
        bool hasAccountSetup = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "AccountSetupSagaController",
            StringComparison.Ordinal));
        Assert.True(hasTransferFunds, "Expected TransferFundsSagaController");
        Assert.True(hasAccountSetup, "Expected AccountSetupSagaController");
    }

    /// <summary>
    ///     Route prefix should be kebab-case.
    /// </summary>
    [Fact]
    public void RoutePrefixIsKebabCase()
    {
        const string sagaSource = """
                                  using System;
                                  using Mississippi.Inlet.Generators.Abstractions;
                                  using Mississippi.EventSourcing.Sagas.Abstractions;

                                  namespace TestApp.Domain.Sagas.TransferFunds
                                  {
                                      public sealed record TransferFundsSagaInput
                                      {
                                          public required decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
                                      public sealed record TransferFundsSagaState : ISagaDefinition
                                      {
                                          public static string SagaName => "TransferFunds";
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Single(runResult.GeneratedTrees);
        string generatedSource = runResult.GeneratedTrees[0].GetText().ToString();

        // Verify route uses kebab-case
        Assert.Contains("[Route(\"api/sagas/transfer-funds\")]", generatedSource, StringComparison.Ordinal);
    }
}