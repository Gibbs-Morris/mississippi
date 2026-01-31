using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaServerDtoGenerator" />.
/// </summary>
public class SagaServerDtoGeneratorTests
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
        SagaServerDtoGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Nothing should be generated without ISagaDefinition implementation.
    /// </summary>
    [Fact]
    public void DoesNotGenerateForNonSagaDefinition()
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
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Nothing should be generated without InputType.
    /// </summary>
    [Fact]
    public void DoesNotGenerateWhenInputTypeNotSpecified()
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
    ///     DTO properties should have JSON property name attributes with camelCase.
    /// </summary>
    [Fact]
    public void DtoHasJsonPropertyNameAttributes()
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
        SyntaxTree dtoTree = runResult.GeneratedTrees.First(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto",
            StringComparison.Ordinal));
        string dtoSource = dtoTree.GetText().ToString();

        // Verify JSON property names are camelCase
        Assert.Contains("[JsonPropertyName(\"sourceAccountId\")]", dtoSource, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"amount\")]", dtoSource, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"correlationId\")]", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     DTO should include all input properties plus CorrelationId.
    /// </summary>
    [Fact]
    public void DtoIncludesAllPropertiesAndCorrelationId()
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
        SyntaxTree dtoTree = runResult.GeneratedTrees.First(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto",
            StringComparison.Ordinal));
        string dtoSource = dtoTree.GetText().ToString();

        // Verify properties from input
        Assert.Contains("SourceAccountId", dtoSource, StringComparison.Ordinal);
        Assert.Contains("DestinationAccountId", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Amount", dtoSource, StringComparison.Ordinal);

        // Verify CorrelationId is added
        Assert.Contains("CorrelationId", dtoSource, StringComparison.Ordinal);
        Assert.Contains("string? CorrelationId", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     DTO, Mapper, and Registration should be generated for saga with InputType.
    /// </summary>
    [Fact]
    public void GeneratesDtoMapperAndRegistration()
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

        // 1 DTO + 1 Mapper + 1 Registration = 3 files
        Assert.Equal(3, runResult.GeneratedTrees.Length);
        bool hasDto = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto",
            StringComparison.Ordinal));
        bool hasMapper = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaDtoMapper",
            StringComparison.Ordinal));
        bool hasRegistration = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "SagaServerRegistrations",
            StringComparison.Ordinal));
        Assert.True(hasDto, "Expected StartTransferFundsSagaDto to be generated");
        Assert.True(hasMapper, "Expected StartTransferFundsSagaDtoMapper to be generated");
        Assert.True(hasRegistration, "Expected SagaServerRegistrations to be generated");
    }

    /// <summary>
    ///     Multiple sagas should generate separate sets of files.
    /// </summary>
    [Fact]
    public void GeneratesFilesForMultipleSagas()
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

        // 2 sagas × 3 files (DTO + Mapper + Registration) = 6 files
        Assert.Equal(6, runResult.GeneratedTrees.Length);
        bool hasTransferFundsDto = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto",
            StringComparison.Ordinal));
        bool hasAccountSetupDto = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "StartAccountSetupSagaDto",
            StringComparison.Ordinal));
        Assert.True(hasTransferFundsDto, "Expected StartTransferFundsSagaDto");
        Assert.True(hasAccountSetupDto, "Expected StartAccountSetupSagaDto");
    }

    /// <summary>
    ///     Handles nullable properties correctly.
    /// </summary>
    [Fact]
    public void HandlesNullableProperties()
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
                                          public string? Notes { get; init; }
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
        SyntaxTree dtoTree = runResult.GeneratedTrees.First(t => t.FilePath.Contains(
            "StartTransferFundsSagaDto",
            StringComparison.Ordinal));
        string dtoSource = dtoTree.GetText().ToString();

        // Required properties should be marked required
        Assert.Contains("required decimal Amount", dtoSource, StringComparison.Ordinal);

        // Nullable properties should not be required
        Assert.Contains("string? Notes", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Mapper should implement IMapper interface.
    /// </summary>
    [Fact]
    public void MapperImplementsIMapperInterface()
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
        SyntaxTree mapperTree = runResult.GeneratedTrees.First(t => t.FilePath.Contains(
            "StartTransferFundsSagaDtoMapper",
            StringComparison.Ordinal));
        string mapperSource = mapperTree.GetText().ToString();

        // Verify IMapper implementation
        Assert.Contains(
            "IMapper<StartTransferFundsSagaDto, TransferFundsSagaInput>",
            mapperSource,
            StringComparison.Ordinal);
        Assert.Contains("public TransferFundsSagaInput Map(", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Registration should add mapper to DI.
    /// </summary>
    [Fact]
    public void RegistrationAddsMapperToDependencyInjection()
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
        SyntaxTree registrationTree = runResult.GeneratedTrees.First(t => t.FilePath.Contains(
            "SagaServerRegistrations",
            StringComparison.Ordinal));
        string registrationSource = registrationTree.GetText().ToString();

        // Verify extension method and registration
        Assert.Contains(
            "public static IServiceCollection AddTransferFundsSagaServer(",
            registrationSource,
            StringComparison.Ordinal);
        Assert.Contains("this IServiceCollection services", registrationSource, StringComparison.Ordinal);
        Assert.Contains(
            "AddSingleton<IMapper<StartTransferFundsSagaDto, TransferFundsSagaInput>, StartTransferFundsSagaDtoMapper>",
            registrationSource,
            StringComparison.Ordinal);
    }
}