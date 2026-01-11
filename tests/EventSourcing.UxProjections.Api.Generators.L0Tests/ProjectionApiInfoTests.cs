using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.UxProjections.Api.Generators.Models;


namespace Mississippi.EventSourcing.UxProjections.Api.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionApiInfo" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections API Generators")]
[AllureSubSuite("ProjectionApiInfo")]
public sealed class ProjectionApiInfoTests
{
    /// <summary>
    ///     Verifies that Authorize is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", sut.Authorize);
    }

    /// <summary>
    ///     Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void DefaultValuesAreSetCorrectly()
    {
        // Act
        ProjectionApiInfo sut = new();

        // Assert
        Assert.Equal(string.Empty, sut.FullTypeName);
        Assert.Equal(string.Empty, sut.TypeName);
        Assert.Equal(string.Empty, sut.Namespace);
        Assert.Equal(string.Empty, sut.Route);
        Assert.True(sut.IsBatchEnabled);
        Assert.Null(sut.Authorize);
        Assert.Null(sut.Tags);
    }

    /// <summary>
    ///     Verifies that FullTypeName is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void FullTypeNameIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            FullTypeName = "MyNamespace.MyProjection",
        };

        // Assert
        Assert.Equal("MyNamespace.MyProjection", sut.FullTypeName);
    }

    /// <summary>
    ///     Verifies that IsBatchEnabled is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsBatchEnabledIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            IsBatchEnabled = false,
        };

        // Assert
        Assert.False(sut.IsBatchEnabled);
    }

    /// <summary>
    ///     Verifies that Namespace is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void NamespaceIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            Namespace = "MyNamespace",
        };

        // Assert
        Assert.Equal("MyNamespace", sut.Namespace);
    }

    /// <summary>
    ///     Verifies that Route is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void RouteIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            Route = "users",
        };

        // Assert
        Assert.Equal("users", sut.Route);
    }

    /// <summary>
    ///     Verifies that Tags is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            Tags = ["Users", "API"],
        };

        // Assert
        Assert.NotNull(sut.Tags);
        Assert.Equal(2, sut.Tags.Length);
        Assert.Contains("Users", sut.Tags);
        Assert.Contains("API", sut.Tags);
    }

    /// <summary>
    ///     Verifies that TypeName is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TypeNameIsSettable()
    {
        // Arrange
        ProjectionApiInfo sut = new()
        {
            TypeName = "MyProjection",
        };

        // Assert
        Assert.Equal("MyProjection", sut.TypeName);
    }
}