using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Blazor.WebAssembly.ActionEffects;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="ProjectionFetchResult" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Action Effects")]
[AllureSubSuite("ProjectionFetchResult")]
public sealed class ProjectionFetchResultTests
{
    /// <summary>
    ///     Test projection for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name, int Value);

    /// <summary>
    ///     Verifies that Create with generic type sets Data correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateGenericSetsDataCorrectly()
    {
        // Arrange
        TestProjection data = new("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 5L);

        // Assert
        Assert.Equal(data, result.Data);
    }

    /// <summary>
    ///     Verifies that Create with generic type sets IsNotFound to false.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateGenericSetsIsNotFoundFalse()
    {
        // Arrange
        TestProjection data = new("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 1L);

        // Assert
        Assert.False(result.IsNotFound);
    }

    /// <summary>
    ///     Verifies that Create with generic type sets Version correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateGenericSetsVersionCorrectly()
    {
        // Arrange
        TestProjection data = new("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 123L);

        // Assert
        Assert.Equal(123L, result.Version);
    }

    /// <summary>
    ///     Verifies that Create with generic type throws on null data.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateGenericThrowsOnNullData()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionFetchResult.Create<TestProjection>(null!, 1L));
    }

    /// <summary>
    ///     Verifies that Create with object type sets Data correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateObjectSetsDataCorrectly()
    {
        // Arrange
        object data = new TestProjection("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 5L);

        // Assert
        Assert.Equal(data, result.Data);
    }

    /// <summary>
    ///     Verifies that Create with object type sets IsNotFound to false.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateObjectSetsIsNotFoundFalse()
    {
        // Arrange
        object data = new TestProjection("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 1L);

        // Assert
        Assert.False(result.IsNotFound);
    }

    /// <summary>
    ///     Verifies that Create with object type sets Version correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateObjectSetsVersionCorrectly()
    {
        // Arrange
        object data = new TestProjection("test", 42);

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 456L);

        // Assert
        Assert.Equal(456L, result.Version);
    }

    /// <summary>
    ///     Verifies that Create with object type throws on null data.
    /// </summary>
    [Fact]
    [AllureFeature("Create Factory")]
    public void CreateObjectThrowsOnNullData()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionFetchResult.Create(null!, 1L));
    }

    /// <summary>
    ///     Verifies that the NotFound sentinel has IsNotFound set to true.
    /// </summary>
    [Fact]
    [AllureFeature("NotFound Sentinel")]
    public void NotFoundSentinelHasIsNotFoundTrue()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.True(result.IsNotFound);
    }

    /// <summary>
    ///     Verifies that the NotFound sentinel has null Data.
    /// </summary>
    [Fact]
    [AllureFeature("NotFound Sentinel")]
    public void NotFoundSentinelHasNullData()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Null(result.Data);
    }

    /// <summary>
    ///     Verifies that the NotFound sentinel has Version equal to zero.
    /// </summary>
    [Fact]
    [AllureFeature("NotFound Sentinel")]
    public void NotFoundSentinelHasVersionZero()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Equal(0, result.Version);
    }

    /// <summary>
    ///     Verifies that the NotFound sentinel is a singleton instance.
    /// </summary>
    [Fact]
    [AllureFeature("NotFound Sentinel")]
    public void NotFoundSentinelIsSingletonInstance()
    {
        // Act
        ProjectionFetchResult first = ProjectionFetchResult.NotFound;
        ProjectionFetchResult second = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Same(first, second);
    }
}