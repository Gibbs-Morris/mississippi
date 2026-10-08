using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;


namespace Mississippi.Inlet.Client.Generators.L0Tests.Commands;

/// <summary>
///     Verifies that command client features retain their Commands namespace requirement.
/// </summary>
public sealed class CommandClientNamespaceGateTests
{
    /// <summary>
    ///     Compiles a valid input assembly for referenced command discovery.
    /// </summary>
    /// <param name="compilation">The input assembly to compile.</param>
    /// <returns>The emitted assembly reference.</returns>
    private static PortableExecutableReference CompileReference(
        CSharpCompilation compilation
    )
    {
        using MemoryStream stream = new();
        EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    /// <summary>
    ///     Commands outside the Commands subnamespace must not produce client features without matching endpoints.
    /// </summary>
    /// <param name="commandNamespace">The namespace containing the misplaced command.</param>
    /// <param name="isReferencedCommand">Whether the command is discovered from a referenced assembly.</param>
    [Theory]
    [InlineData("TestApp.Aggregates.Account", false)]
    [InlineData("TestApp.Aggregates.Account", true)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account", false)]
    [InlineData("TestApp.CoreDomainLogic.Aggregates.Account", true)]
    [InlineData("TestApp.Domain.Aggregates.Account", false)]
    [InlineData("TestApp.Domain.Aggregates.Account", true)]
    public void CommandFeaturesRequireCommandsNamespace(
        string commandNamespace,
        bool isReferencedCommand
    )
    {
        const string attributeSource = """
                                       namespace Mississippi.Inlet.Generators.Abstractions
                                       {
                                           public sealed class GenerateCommandAttribute : System.Attribute { }
                                       }
                                       """;
        string commandSource = $$"""
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
        ];
        CSharpCompilation contracts = CSharpCompilation.Create(
            "Contracts",
            [CSharpSyntaxTree.ParseText(attributeSource, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));
        references.Add(CompileReference(contracts));
        if (isReferencedCommand)
        {
            CSharpCompilation domain = CSharpCompilation.Create(
                "Input.Domain",
                [CSharpSyntaxTree.ParseText(commandSource, cancellationToken: TestContext.Current.CancellationToken)],
                references,
                new(OutputKind.DynamicallyLinkedLibrary));
            references.Add(CompileReference(domain));
            commandSource = string.Empty;
        }

        CSharpCompilation input = CSharpCompilation.Create(
            "Consumer.Client",
            [CSharpSyntaxTree.ParseText(commandSource, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        IIncrementalGenerator[] generators =
        [
            new CommandClientActionEffectsGenerator(),
            new CommandClientStateGenerator(),
            new CommandClientReducersGenerator(),
            new CommandClientRegistrationGenerator(),
        ];
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(generator => generator.AsSourceGenerator()));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        Assert.Empty(diagnostics);
        Assert.All(runResult.Results, result => Assert.Null(result.Exception));
        Assert.All(runResult.Results, result => Assert.Empty(result.GeneratedSources));
        Assert.Empty(runResult.GeneratedTrees);
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}