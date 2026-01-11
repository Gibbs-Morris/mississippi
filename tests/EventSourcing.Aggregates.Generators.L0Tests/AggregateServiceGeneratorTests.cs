using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;


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
}