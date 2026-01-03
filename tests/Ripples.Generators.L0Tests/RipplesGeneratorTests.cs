using Allure.Net.Commons;
using Allure.Xunit.Attributes;

using FluentAssertions;

using Xunit;


namespace Mississippi.Ripples.Generators.L0Tests;

/// <summary>
/// Tests for <see cref="RipplesGenerator"/> helper methods and code generation.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Generators")]
[AllureSubSuite("RipplesGenerator")]
public sealed class RipplesGeneratorTests
{
    /// <summary>
    /// Verifies that the generator can be instantiated.
    /// </summary>
    [Fact]
    [AllureFeature("Generator Instantiation")]
    public void CanInstantiateGenerator()
    {
        // Act
        RipplesGenerator sut = new();

        // Assert
        sut.Should().NotBeNull();
    }
}
