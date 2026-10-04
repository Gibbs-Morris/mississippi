using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandClientActionEffectsGenerator" />.
/// </summary>
public class CommandClientActionEffectsGeneratorTests
{
    /// <summary>
    ///     Minimal stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string? Route { get; set; }
                                                  public string HttpMethod { get; set; } = "POST";
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
        CommandClientActionEffectsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated action effect file should have correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectFileHasCorrectName()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.Contains("PlaceOrderActionEffect.g.cs", runResult.GeneratedTrees[0].FilePath, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated action effect should have aggregate route prefix.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectHasAggregateRoutePrefix()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("AggregateRoutePrefix =>", generatedCode, StringComparison.Ordinal);
        Assert.Contains("/api/aggregates/order", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated action effect should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedActionEffectHasAutoGeneratedHeader()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.NotEmpty(runResult.GeneratedTrees);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated effect should have route property.
    /// </summary>
    [Fact]
    public void GeneratedEffectHasRouteProperty()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("protected override string Route =>", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     An aggregate's configured route must be reflected in the compiled effect and its documentation.
    /// </summary>
    /// <param name="useReferencedAssembly">Whether the domain is a referenced assembly.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEffectHonorsConfiguredAggregateRoutePrefix(
        bool useReferencedAssembly
    )
    {
        (Compilation compilation, SyntaxTree effect) = CommandClientRouteTestFixture.Run(
            "special-order",
            useReferencedAssembly);
        PropertyDeclarationSyntax prefix = effect.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(p => p.Identifier.ValueText == "AggregateRoutePrefix");
        PropertyDeclarationSyntax route = effect.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(p => p.Identifier.ValueText == "Route");
        SemanticModel model = compilation.GetSemanticModel(effect);
        Assert.Equal(
            "/api/aggregates/special-order",
            model.GetConstantValue(prefix.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Value);
        Assert.Equal(
            "submit-order",
            model.GetConstantValue(route.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Value);
        Assert.Contains(
            "/api/aggregates/special-order/{entityId}/submit-order",
            effect.GetText(TestContext.Current.CancellationToken).ToString(),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated effect should be internal sealed class.
    /// </summary>
    [Fact]
    public void GeneratedEffectIsInternalSealedClass()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string generatedCode = runResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("internal sealed class PlaceOrderActionEffect", generatedCode, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple endpoint-marked aggregates must not select an arbitrary route override.
    /// </summary>
    /// <param name="useReferencedAssembly">Whether the domain is a referenced assembly.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEffectKeepsDefaultRouteForAmbiguousAggregateNamespace(
        bool useReferencedAssembly
    )
    {
        (Compilation compilation, SyntaxTree effect) = CommandClientRouteTestFixture.Run(
            "special-order",
            useReferencedAssembly,
            includeSecondMarkedAggregate: true);
        PropertyDeclarationSyntax prefix = effect.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(p => p.Identifier.ValueText == "AggregateRoutePrefix");
        SemanticModel model = compilation.GetSemanticModel(effect);
        Assert.Equal(
            "/api/aggregates/order",
            model.GetConstantValue(prefix.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Value);
    }

    /// <summary>
    ///     Empty aggregate prefixes and unrelated marked types must retain the existing route fallback.
    /// </summary>
    /// <param name="routePrefix">The unset or empty prefix.</param>
    /// <param name="useReferencedAssembly">Whether the domain is a referenced assembly.</param>
    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("", true)]
    public void GeneratedEffectKeepsDefaultRouteForEmptyAggregatePrefix(
        string? routePrefix,
        bool useReferencedAssembly
    )
    {
        (Compilation compilation, SyntaxTree effect) = CommandClientRouteTestFixture.Run(
            routePrefix,
            useReferencedAssembly);
        PropertyDeclarationSyntax prefix = effect.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(p => p.Identifier.ValueText == "AggregateRoutePrefix");
        SemanticModel model = compilation.GetSemanticModel(effect);
        Assert.Equal(
            "/api/aggregates/order",
            model.GetConstantValue(prefix.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Value);
    }

    /// <summary>
    ///     Commands without an endpoint-marked aggregate must preserve the existing generated route.
    /// </summary>
    /// <param name="useReferencedAssembly">Whether the domain is a referenced assembly.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEffectKeepsDefaultRouteWithoutMarkedAggregate(
        bool useReferencedAssembly
    )
    {
        (Compilation compilation, SyntaxTree effect) = CommandClientRouteTestFixture.Run(
            "ignored-prefix",
            useReferencedAssembly,
            false);
        PropertyDeclarationSyntax prefix = effect.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(p => p.Identifier.ValueText == "AggregateRoutePrefix");
        SemanticModel model = compilation.GetSemanticModel(effect);
        Assert.Equal(
            "/api/aggregates/order",
            model.GetConstantValue(prefix.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Value);
    }

    /// <summary>
    ///     Generator should produce no output when no commands are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoCommands()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public class RegularClass
                                  {
                                      public string Name { get; set; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Multiple commands should generate separate action effects.
    /// </summary>
    [Fact]
    public void MultipleCommandsGenerateSeparateEffects()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand]
                                         public sealed record PlaceOrder
                                         {
                                             public string ProductId { get; init; }
                                         }

                                         [GenerateCommand]
                                         public sealed record CancelOrder
                                         {
                                             public string Reason { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        bool hasPlaceOrderActionEffect = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "PlaceOrderActionEffect",
            StringComparison.Ordinal));
        bool hasCancelOrderActionEffect = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "CancelOrderActionEffect",
            StringComparison.Ordinal));
        Assert.True(hasPlaceOrderActionEffect);
        Assert.True(hasCancelOrderActionEffect);
    }
}