using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="SagaControllerGenerator" />.
/// </summary>
public sealed class SagaControllerGeneratorTests
{
    /// <summary>
    ///     Minimal stubs required for saga generator tests.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateSagaEndpointsAttribute : Attribute
                                              {
                                                  public Type? InputType { get; set; }
                                                  public string? FeatureKey { get; set; }
                                                  public string? RoutePrefix { get; set; }
                                              }
                                          }

                                          namespace Mississippi.EventSourcing.Sagas.Abstractions
                                          {
                                              public interface ISagaState
                                              {
                                              }
                                          }
                                          """;

    private static CSharpCompilation CreateCompilation(
        params string[] sources
    )
    {
        SyntaxTree[] syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        return CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
    }

    private static List<object> GetSagasFromCompilation(
        Compilation compilation
    )
    {
        MethodInfo method = typeof(SagaControllerGenerator).GetMethod(
                                "GetSagasFromCompilation",
                                BindingFlags.NonPublic | BindingFlags.Static) ??
                            throw new InvalidOperationException("GetSagasFromCompilation not found.");
        IEnumerable<object> sagas = (IEnumerable<object>)method.Invoke(null, new object[] { compilation })!;
        return sagas.ToList();
    }

    private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult
        RunResult) RunGenerator(
            params string[] sources
        )
    {
        SyntaxTree[] syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        SagaControllerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Verifies controller and DTO generation for property-based inputs.
    /// </summary>
    [Fact]
    public void GeneratesControllerAndDtoForPropertyInput()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed class TransferInput
                                      {
                                          public string AccountId { get; init; }
                                          public decimal Amount { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("StartTransferSagaDto.g.cs", StringComparison.Ordinal));
        Assert.Contains(
            runResult.GeneratedTrees,
            tree => tree.FilePath.Contains("TransferSagaController.g.cs", StringComparison.Ordinal));
        string controllerCode = runResult.GeneratedTrees.First(tree =>
                tree.FilePath.Contains("TransferSagaController.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("api/sagas/transfer", controllerCode, StringComparison.Ordinal);
        Assert.Contains("AccountId = request.AccountId", controllerCode, StringComparison.Ordinal);
        Assert.Contains("Amount = request.Amount", controllerCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies controller output includes input namespace imports when needed.
    /// </summary>
    [Fact]
    public void GeneratesControllerWithInputNamespaceImport()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Inputs
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }
                                  }

                                  namespace TestApp.Domain.Sagas
                                  {
                                      using TestApp.Domain.Inputs;

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string controllerCode = runResult.GeneratedTrees.First(tree =>
                tree.FilePath.Contains("TransferSagaController.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("using TestApp.Domain.Inputs;", controllerCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies DTO generation handles required and nullable properties.
    /// </summary>
    [Fact]
    public void GeneratesDtoWithRequiredAndNullableProperties()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed class TransferInput
                                      {
                                          public required string AccountId { get; init; }
                                          public string? Notes { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string dtoCode = runResult.GeneratedTrees.First(tree =>
                tree.FilePath.Contains("StartTransferSagaDto.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("[JsonRequired]", dtoCode, StringComparison.Ordinal);
        Assert.Contains("public required string AccountId", dtoCode, StringComparison.Ordinal);
        Assert.Contains("public string? Notes", dtoCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies positional input mapping in generated controller.
    /// </summary>
    [Fact]
    public void GeneratesPositionalInputMapping()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput(string AccountId, decimal Amount);

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string controllerCode = runResult.GeneratedTrees.First(tree =>
                tree.FilePath.Contains("TransferSagaController.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains(
            "new TransferInput(request.AccountId, request.Amount)",
            controllerCode,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies saga info accessors can be read via reflection.
    /// </summary>
    [Fact]
    public void SagaInfoAccessorsAreReadable()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput(string AccountId);

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput), FeatureKey = "transfer")]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        Compilation compilation = CreateCompilation(AttributeStubs, sagaSource);
        object sagaInfo = Assert.Single(GetSagasFromCompilation(compilation));
        Type sagaInfoType = sagaInfo.GetType();
        string featureKey = (string)sagaInfoType.GetProperty("FeatureKey")!.GetValue(sagaInfo)!;
        object inputType = sagaInfoType.GetProperty("InputType")!.GetValue(sagaInfo)!;
        object sagaStateType = sagaInfoType.GetProperty("SagaStateType")!.GetValue(sagaInfo)!;
        Assert.Equal("transfer", featureKey);
        Assert.NotNull(inputType);
        Assert.NotNull(sagaStateType);
    }

    /// <summary>
    ///     Verifies sagas are skipped when attribute symbols are missing.
    /// </summary>
    [Fact]
    public void SkipsSagaWhenAttributeTypeMissing()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) = RunGenerator(sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies sagas without input types are skipped.
    /// </summary>
    [Fact]
    public void SkipsSagaWithoutInputType()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      [GenerateSagaEndpoints]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies non-saga types are ignored.
    /// </summary>
    [Fact]
    public void SkipsTypeWithoutSagaInterface()
    {
        const string sagaSource = """
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput))]
                                      public sealed record TransferSagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Verifies explicit route prefixes are honored.
    /// </summary>
    [Fact]
    public void UsesExplicitRoutePrefix()
    {
        const string sagaSource = """
                                  using Mississippi.EventSourcing.Sagas.Abstractions;
                                  using Mississippi.Inlet.Generators.Abstractions;

                                  namespace TestApp.Domain.Sagas
                                  {
                                      public sealed record TransferInput
                                      {
                                          public string AccountId { get; init; }
                                      }

                                      [GenerateSagaEndpoints(InputType = typeof(TransferInput), RoutePrefix = "custom-route")]
                                      public sealed record TransferSagaState : ISagaState
                                      {
                                      }
                                  }
                                  """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, sagaSource);
        string controllerCode = runResult.GeneratedTrees.First(tree =>
                tree.FilePath.Contains("TransferSagaController.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();
        Assert.Contains("api/sagas/custom-route", controllerCode, StringComparison.Ordinal);
    }
}