using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;


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
}