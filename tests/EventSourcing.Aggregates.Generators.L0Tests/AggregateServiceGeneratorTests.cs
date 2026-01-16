using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.EventSourcing.Aggregates.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateServiceGenerator" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Generators")]
[AllureSubSuite("AggregateServiceGenerator")]
public sealed class AggregateServiceGeneratorTests
{
    /// <summary>
    ///     Verifies that the generator emits service and controller sources for aggregates with handlers.
    /// </summary>
    [Fact(DisplayName = "Generator Emits Service and Controller")]
    [AllureFeature("Source Generation")]
    public void GeneratorEmitsServiceInterfaceImplementationAndController()
    {
        // Arrange
        const string source = @"
using System;

namespace Mississippi.EventSourcing.Aggregates.Abstractions
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AggregateServiceAttribute : Attribute
    {
        public AggregateServiceAttribute(string route)
        {
        }

        public bool GenerateApi { get; set; }

        public string? Authorize { get; set; }
    }

    public abstract class CommandHandler<TCommand, TAggregate>
    {
    }

    public sealed class OperationResult
    {
    }
}

namespace Sample.Aggregates
{
    using Mississippi.EventSourcing.Aggregates.Abstractions;

    [AggregateService(""orders"", GenerateApi = true, Authorize = ""policy"")]
    public sealed class OrderAggregate
    {
    }

    public sealed class CreateOrder
    {
    }

    public sealed class CreateOrderHandler : CommandHandler<CreateOrder, OrderAggregate>
    {
    }
}
";

        // Act
        GeneratorDriverRunResult result = RunGenerator(source, new AggregateServiceGenerator());
        ImmutableArray<GeneratedSourceResult> sources = result.Results.Single().GeneratedSources;

        // Assert
        Assert.Contains(sources, generated => generated.HintName == "IOrderService.g.cs");
        Assert.Contains(sources, generated => generated.HintName == "OrderService.g.cs");
        Assert.Contains(sources, generated => generated.HintName == "OrderController.g.cs");

        string interfaceSource = sources.First(generated => generated.HintName == "IOrderService.g.cs")
            .SourceText.ToString();
        Assert.Contains("Task<OperationResult> CreateOrderAsync", interfaceSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that no sources are emitted when no handlers are discovered.
    /// </summary>
    [Fact(DisplayName = "Generator Skips Aggregates Without Handlers")]
    [AllureFeature("Source Generation")]
    public void GeneratorSkipsAggregatesWithoutHandlers()
    {
        // Arrange
        const string source = @"
using System;

namespace Mississippi.EventSourcing.Aggregates.Abstractions
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AggregateServiceAttribute : Attribute
    {
        public AggregateServiceAttribute(string route)
        {
        }
    }

    public abstract class CommandHandler<TCommand, TAggregate>
    {
    }
}

namespace Sample.Aggregates
{
    using Mississippi.EventSourcing.Aggregates.Abstractions;

    [AggregateService(""orders"")]
    public sealed class OrderAggregate
    {
    }
}
";

        // Act
        GeneratorDriverRunResult result = RunGenerator(source, new AggregateServiceGenerator());
        ImmutableArray<GeneratedSourceResult> sources = result.Results.Single().GeneratedSources;

        // Assert
        Assert.Empty(sources);
    }

    /// <summary>
    ///     Verifies that the generator can be instantiated.
    /// </summary>
    [Fact]
    [AllureFeature("Generator Instantiation")]
    public void CanInstantiateGenerator()
    {
        // Act
        AggregateServiceGenerator sut = new();

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
        AggregateServiceGenerator sut = new();

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
            "AggregateServiceGeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}