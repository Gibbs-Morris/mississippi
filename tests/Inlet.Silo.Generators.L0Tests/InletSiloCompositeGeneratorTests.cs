using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Silo.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="InletSiloCompositeGenerator" />.
/// </summary>
public sealed class InletSiloCompositeGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Assembly)]
                                              public sealed class GenerateInletSiloCompositeAttribute : Attribute
                                              {
                                                  public string? AppName { get; set; }

                                                  public string StreamProviderName { get; set; } = "mississippi-streaming";
                                              }

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                              }
                                          }

                                          namespace Mississippi.Inlet.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class ProjectionPathAttribute : Attribute
                                              {
                                                  public ProjectionPathAttribute(string path)
                                                  {
                                                      Path = path;
                                                  }

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
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.Immutable.dll")),
        ];
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Silo",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        InletSiloCompositeGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated composite registration should include silo entry points and layered registrations.
    /// </summary>
    [Fact]
    public void GeneratedCompositeIncludesSiloEntryPoints()
    {
        const string compositeSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;
                                       using Mississippi.Inlet.Abstractions;

                                       [assembly: GenerateInletSiloComposite(AppName = "TestApp")]

                                       namespace TestApp.Domain.Aggregates.Account.Commands
                                       {
                                           [GenerateCommand]
                                           public sealed record OpenAccount
                                           {
                                           }
                                       }

                                       namespace TestApp.Domain.Projections.AccountBalance
                                       {
                                           [GenerateProjectionEndpoints]
                                           [ProjectionPath("accounts/balance")]
                                           public sealed record AccountBalanceProjection
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, compositeSource);
        SyntaxTree tree = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith(
            "TestAppSiloRegistrations.g.cs",
            StringComparison.Ordinal));
        string generatedCode = tree.GetText().ToString();
        Assert.Contains("public static MississippiSiloBuilder AddTestAppSilo", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public static WebApplication UseTestAppSilo", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.HostBuilder.AddTestAppObservability();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.HostBuilder.AddTestAppAspireResources();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddTestAppDomain();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddTestAppEventSourcing();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("builder.HostBuilder.AddTestAppOrleansSilo();", generatedCode, StringComparison.Ordinal);
        Assert.Contains("app.MapTestAppHealthCheck();", generatedCode, StringComparison.Ordinal);
    }
}