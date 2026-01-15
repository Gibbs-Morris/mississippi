using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Blazor.WebAssembly.Effects;
using Mississippi.Reservoir.Abstractions.Actions;

using Xunit;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="ProjectionActionFactory" />.
/// </summary>
[AllureSuite("Inlet.Blazor.WebAssembly")]
[AllureSubSuite("ProjectionActionFactory")]
public sealed class ProjectionActionFactoryTests
{
    /// <summary>
    ///     CreateError should return ProjectionErrorAction for the specified type.
    /// </summary>
    [Fact]
    [AllureFeature("CreateError")]
    public void CreateErrorReturnsProjectionErrorAction()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";
        Exception error = new InvalidOperationException("Test error");

        // Act
        IAction result = ProjectionActionFactory.CreateError(projectionType, entityId, error);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionErrorAction<TestProjection>>(result);
        ProjectionErrorAction<TestProjection> typedResult = (ProjectionErrorAction<TestProjection>)result;
        Assert.Equal(entityId, typedResult.EntityId);
        Assert.Same(error, typedResult.Error);
    }

    /// <summary>
    ///     CreateError should throw when projection type is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateError")]
    public void CreateErrorThrowsWhenProjectionTypeIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateError(null!, "entity-123", new InvalidOperationException()));
    }

    /// <summary>
    ///     CreateError should throw when entity ID is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateError")]
    public void CreateErrorThrowsWhenEntityIdIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateError(typeof(TestProjection), null!, new InvalidOperationException()));
    }

    /// <summary>
    ///     CreateError should throw when error is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateError")]
    public void CreateErrorThrowsWhenErrorIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateError(typeof(TestProjection), "entity-123", null!));
    }

    /// <summary>
    ///     CreateLoaded should return ProjectionLoadedAction for the specified type.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoaded")]
    public void CreateLoadedReturnsProjectionLoadedAction()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";
        TestProjection data = new() { Value = 42 };
        const long version = 5L;

        // Act
        IAction result = ProjectionActionFactory.CreateLoaded(projectionType, entityId, data, version);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(result);
        ProjectionLoadedAction<TestProjection> typedResult = (ProjectionLoadedAction<TestProjection>)result;
        Assert.Equal(entityId, typedResult.EntityId);
        Assert.Same(data, typedResult.Data);
        Assert.Equal(version, typedResult.Version);
    }

    /// <summary>
    ///     CreateLoaded should throw when projection type is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoaded")]
    public void CreateLoadedThrowsWhenProjectionTypeIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoaded(null!, "entity-123", new TestProjection(), 1L));
    }

    /// <summary>
    ///     CreateLoaded should throw when entity ID is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoaded")]
    public void CreateLoadedThrowsWhenEntityIdIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoaded(typeof(TestProjection), null!, new TestProjection(), 1L));
    }

    /// <summary>
    ///     CreateLoaded should allow null data.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoaded")]
    public void CreateLoadedAllowsNullData()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";
        const long version = 5L;

        // Act
        IAction result = ProjectionActionFactory.CreateLoaded(projectionType, entityId, null, version);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(result);
        ProjectionLoadedAction<TestProjection> typedResult = (ProjectionLoadedAction<TestProjection>)result;
        Assert.Null(typedResult.Data);
    }

    /// <summary>
    ///     CreateLoading should return ProjectionLoadingAction for the specified type.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoading")]
    public void CreateLoadingReturnsProjectionLoadingAction()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";

        // Act
        IAction result = ProjectionActionFactory.CreateLoading(projectionType, entityId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(result);
        ProjectionLoadingAction<TestProjection> typedResult = (ProjectionLoadingAction<TestProjection>)result;
        Assert.Equal(entityId, typedResult.EntityId);
    }

    /// <summary>
    ///     CreateLoading should throw when projection type is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoading")]
    public void CreateLoadingThrowsWhenProjectionTypeIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoading(null!, "entity-123"));
    }

    /// <summary>
    ///     CreateLoading should throw when entity ID is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateLoading")]
    public void CreateLoadingThrowsWhenEntityIdIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoading(typeof(TestProjection), null!));
    }

    /// <summary>
    ///     CreateUpdated should return ProjectionUpdatedAction for the specified type.
    /// </summary>
    [Fact]
    [AllureFeature("CreateUpdated")]
    public void CreateUpdatedReturnsProjectionUpdatedAction()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";
        TestProjection data = new() { Value = 99 };
        const long version = 10L;

        // Act
        IAction result = ProjectionActionFactory.CreateUpdated(projectionType, entityId, data, version);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(result);
        ProjectionUpdatedAction<TestProjection> typedResult = (ProjectionUpdatedAction<TestProjection>)result;
        Assert.Equal(entityId, typedResult.EntityId);
        Assert.Same(data, typedResult.Data);
        Assert.Equal(version, typedResult.Version);
    }

    /// <summary>
    ///     CreateUpdated should throw when projection type is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateUpdated")]
    public void CreateUpdatedThrowsWhenProjectionTypeIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateUpdated(null!, "entity-123", new TestProjection(), 1L));
    }

    /// <summary>
    ///     CreateUpdated should throw when entity ID is null.
    /// </summary>
    [Fact]
    [AllureFeature("CreateUpdated")]
    public void CreateUpdatedThrowsWhenEntityIdIsNull()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateUpdated(typeof(TestProjection), null!, new TestProjection(), 1L));
    }

    /// <summary>
    ///     CreateUpdated should allow null data.
    /// </summary>
    [Fact]
    [AllureFeature("CreateUpdated")]
    public void CreateUpdatedAllowsNullData()
    {
        // Arrange
        Type projectionType = typeof(TestProjection);
        const string entityId = "entity-123";
        const long version = 10L;

        // Act
        IAction result = ProjectionActionFactory.CreateUpdated(projectionType, entityId, null, version);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(result);
        ProjectionUpdatedAction<TestProjection> typedResult = (ProjectionUpdatedAction<TestProjection>)result;
        Assert.Null(typedResult.Data);
    }

    /// <summary>
    ///     Test projection type for factory tests.
    /// </summary>
    private sealed class TestProjection
    {
        /// <summary>
        ///     Gets or sets the value.
        /// </summary>
        public int Value { get; set; }
    }
}
