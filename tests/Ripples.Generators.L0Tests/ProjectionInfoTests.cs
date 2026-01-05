using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.Ripples.Generators.Models;


namespace Mississippi.Ripples.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionInfo" />.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Generators")]
[AllureSubSuite("ProjectionInfo")]
public sealed class ProjectionInfoTests
{
    /// <summary>
    ///     Verifies that all properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Property Assignment")]
    public void AllPropertiesCanBeSet()
    {
        // Arrange
        string[] tags = ["tag1", "tag2"];

        // Act
        ProjectionInfo sut = new()
        {
            FullTypeName = "MyNamespace.MyProjection",
            TypeName = "MyProjection",
            Namespace = "MyNamespace",
            Route = "my-projections",
            EnableBatch = false,
            Authorize = "AdminPolicy",
            BrookName = "my-brook",
            Tags = tags,
        };

        // Assert
        sut.FullTypeName.Should().Be("MyNamespace.MyProjection");
        sut.TypeName.Should().Be("MyProjection");
        sut.Namespace.Should().Be("MyNamespace");
        sut.Route.Should().Be("my-projections");
        sut.EnableBatch.Should().BeFalse();
        sut.Authorize.Should().Be("AdminPolicy");
        sut.BrookName.Should().Be("my-brook");
        sut.Tags.Should().BeEquivalentTo(tags);
    }

    /// <summary>
    ///     Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void DefaultValuesAreSetCorrectly()
    {
        // Act
        ProjectionInfo sut = new();

        // Assert
        sut.FullTypeName.Should().BeEmpty();
        sut.TypeName.Should().BeEmpty();
        sut.Namespace.Should().BeEmpty();
        sut.Route.Should().BeEmpty();
        sut.EnableBatch.Should().BeTrue();
        sut.Authorize.Should().BeNull();
        sut.BrookName.Should().BeNull();
        sut.Tags.Should().BeNull();
    }
}