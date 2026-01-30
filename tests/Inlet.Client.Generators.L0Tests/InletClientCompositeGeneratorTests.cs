using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="InletClientCompositeGenerator" />.
/// </summary>
public sealed class InletClientCompositeGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Assembly)]
                                              public sealed class GenerateInletClientCompositeAttribute : Attribute
                                              {
                                                  public GenerateInletClientCompositeAttribute(string appName)
                                                  {
                                                      AppName = appName;
                                                  }

                                                  public string AppName { get; }

                                                  public string HubPath { get; set; } = "/hubs/inlet";
                                              }

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                              }

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
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
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        InletClientCompositeGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated composite registration should include configuration overload.
    /// </summary>
    [Fact]
    public void GeneratedCompositeIncludesConfigurableOverload()
    {
        const string compositeSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;
                                       using Mississippi.Inlet.Abstractions;

                                       [assembly: GenerateInletClientComposite("TestApp")]

                                       namespace TestApp.Domain.Projections.Accounts
                                       {
                                           [GenerateProjectionEndpoints]
                                           [ProjectionPath("accounts/balance")]
                                           public sealed record BalanceProjection
                                           {
                                           }
                                       }
                                       """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, compositeSource);
        SyntaxTree tree = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith(
            "TestAppInletRegistrations.g.cs",
            StringComparison.Ordinal));
        string generatedCode = tree.GetText().ToString();
        Assert.Contains("Action<InletBlazorSignalRBuilder>? configureSignalR", generatedCode, StringComparison.Ordinal);
        Assert.Contains("configureSignalR?.Invoke(signalR);", generatedCode, StringComparison.Ordinal);
        Assert.Contains("return AddTestAppInlet(services, null);", generatedCode, StringComparison.Ordinal);
    }
}