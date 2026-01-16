using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.EventSourcing.UxProjections.Api.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionApiGenerator" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections API Generators")]
[AllureSubSuite("ProjectionApiGenerator")]
public sealed class ProjectionApiGeneratorTests
{
    /// <summary>
    ///     Verifies that the generator emits controller, DTO, and batch request sources.
    /// </summary>
    [Fact(DisplayName = "Generator Emits Controller, DTO, and Batch Request")]
    [AllureFeature("Source Generation")]
    public void GeneratorEmitsControllerDtoAndBatchRequest()
    {
        // Arrange
        const string source = @"
using System;

namespace Mississippi.EventSourcing.UxProjections.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UxProjectionAttribute : Attribute
    {
        public UxProjectionAttribute(string route) { }

        public bool IsBatchEnabled { get; set; }

        public string? Authorize { get; set; }
    }
}

namespace Sample.Projections
{
    using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;

    [UxProjection(""widgets"", IsBatchEnabled = true, Authorize = ""policy"")]
    public sealed class WidgetProjection
    {
        public string Id { get; init; } = string.Empty;

        public int Count { get; init; }
    }
}
";

        // Act
        GeneratorDriverRunResult result = RunGenerator(source, new ProjectionApiGenerator());
        ImmutableArray<GeneratedSourceResult> sources = result.Results.Single().GeneratedSources;

        // Assert
        Assert.Contains(sources, generated => generated.HintName == "WidgetProjectionController.g.cs");
        Assert.Contains(sources, generated => generated.HintName == "WidgetProjectionDto.g.cs");
        Assert.Contains(sources, generated => generated.HintName == "BatchProjectionRequest.g.cs");

        string controllerSource = sources.First(generated => generated.HintName == "WidgetProjectionController.g.cs")
            .SourceText.ToString();
        Assert.Contains("WidgetProjectionController", controllerSource, StringComparison.Ordinal);
        Assert.Contains("api/projections/widgets", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the generator can be instantiated.
    /// </summary>
    [Fact]
    [AllureFeature("Generator Instantiation")]
    public void CanInstantiateGenerator()
    {
        // Act
        ProjectionApiGenerator sut = new();

        // Assert
        Assert.NotNull(sut);
    }

    /// <summary>
    ///     Verifies that the generator implements IIncrementalGenerator.
    /// </summary>
    [Fact]
    [AllureFeature("Generator Interface")]
    public void ImplementsIIncrementalGenerator()
    {
        // Act
        ProjectionApiGenerator sut = new();

        // Assert
        Assert.True(sut is IIncrementalGenerator);
    }

    private static GeneratorDriverRunResult RunGenerator(
        string source,
        IIncrementalGenerator generator
    )
    {
        CSharpCompilation compilation = CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static CSharpCompilation CreateCompilation(
        string source
    )
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.CSharp12));
        List<MetadataReference> references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location))
            .ToList();
        return CSharpCompilation.Create(
            "ProjectionApiGeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}