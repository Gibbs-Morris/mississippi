using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.ActionEffects;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="ProjectionFetchResult" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("ActionEffects")]
[AllureSubSuite("ProjectionFetchResult")]
public sealed class ProjectionFetchResultTests
{
    /// <summary>
    ///     Create generic sets Data property.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateGenericSetsData()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "test",
        };

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 42);

        // Assert
        Assert.Same(data, result.Data);
    }

    /// <summary>
    ///     Create generic sets IsNotFound to false.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateGenericSetsIsNotFoundFalse()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "test",
        };

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 42);

        // Assert
        Assert.False(result.IsNotFound);
    }

    /// <summary>
    ///     Create generic sets Version property.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateGenericSetsVersion()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "test",
        };

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 42);

        // Assert
        Assert.Equal(42, result.Version);
    }

    /// <summary>
    ///     Create generic throws ArgumentNullException when data is null.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateGenericThrowsWhenDataIsNull()
    {
        // Arrange
        TestProjection? data = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionFetchResult.Create(data!, 42));
    }

    /// <summary>
    ///     Create object overload sets Data property.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateObjectSetsData()
    {
        // Arrange
        object data = new TestProjection
        {
            Name = "test",
        };

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 100);

        // Assert
        Assert.Same(data, result.Data);
    }

    /// <summary>
    ///     Create object overload sets Version property.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateObjectSetsVersion()
    {
        // Arrange
        object data = new TestProjection
        {
            Name = "test",
        };

        // Act
        ProjectionFetchResult result = ProjectionFetchResult.Create(data, 100);

        // Assert
        Assert.Equal(100, result.Version);
    }

    /// <summary>
    ///     Create object overload throws ArgumentNullException when data is null.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    public void CreateObjectThrowsWhenDataIsNull()
    {
        // Arrange
        object? data = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionFetchResult.Create(data!, 100));
    }

    /// <summary>
    ///     Data init property can be set via object initializer.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void DataInitPropertyCanBeSet()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "init-test",
        };

        // Act
        ProjectionFetchResult result = new()
        {
            Data = data,
        };

        // Assert
        Assert.Same(data, result.Data);
    }

    /// <summary>
    ///     IsNotFound init property can be set via object initializer.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsNotFoundInitPropertyCanBeSet()
    {
        // Act
        ProjectionFetchResult result = new()
        {
            IsNotFound = true,
        };

        // Assert
        Assert.True(result.IsNotFound);
    }

    /// <summary>
    ///     NotFound sentinel has IsNotFound set to true.
    /// </summary>
    [Fact]
    [AllureFeature("Sentinel")]
    public void NotFoundSentinelHasIsNotFoundTrue()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.True(result.IsNotFound);
    }

    /// <summary>
    ///     NotFound sentinel has null Data.
    /// </summary>
    [Fact]
    [AllureFeature("Sentinel")]
    public void NotFoundSentinelHasNullData()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Null(result.Data);
    }

    /// <summary>
    ///     NotFound sentinel has zero Version.
    /// </summary>
    [Fact]
    [AllureFeature("Sentinel")]
    public void NotFoundSentinelHasZeroVersion()
    {
        // Act
        ProjectionFetchResult result = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Equal(0, result.Version);
    }

    /// <summary>
    ///     NotFound sentinel is singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Sentinel")]
    public void NotFoundSentinelIsSingleton()
    {
        // Act
        ProjectionFetchResult first = ProjectionFetchResult.NotFound;
        ProjectionFetchResult second = ProjectionFetchResult.NotFound;

        // Assert
        Assert.Same(first, second);
    }

    /// <summary>
    ///     Version init property can be set via object initializer.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void VersionInitPropertyCanBeSet()
    {
        // Act
        ProjectionFetchResult result = new()
        {
            Version = 999,
        };

        // Assert
        Assert.Equal(999, result.Version);
    }
}