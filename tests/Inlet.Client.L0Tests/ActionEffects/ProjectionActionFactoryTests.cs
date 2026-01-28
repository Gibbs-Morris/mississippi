using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="ProjectionActionFactory" />.
/// </summary>
public sealed class ProjectionActionFactoryTests
{
    /// <summary>
    ///     CreateError returns ProjectionErrorAction with correct generic type.
    /// </summary>
    [Fact]
    public void CreateErrorReturnsCorrectType()
    {
        // Arrange
        Exception error = new InvalidOperationException("test error");

        // Act
        IAction action = ProjectionActionFactory.CreateError(typeof(TestProjection), "entity-1", error);

        // Assert
        Assert.IsType<ProjectionErrorAction<TestProjection>>(action);
    }

    /// <summary>
    ///     CreateError sets EntityId property.
    /// </summary>
    [Fact]
    public void CreateErrorSetsEntityId()
    {
        // Arrange
        Exception error = new InvalidOperationException("test error");

        // Act
        IAction action = ProjectionActionFactory.CreateError(typeof(TestProjection), "entity-123", error);

        // Assert
        ProjectionErrorAction<TestProjection>
            typedAction = Assert.IsType<ProjectionErrorAction<TestProjection>>(action);
        Assert.Equal("entity-123", typedAction.EntityId);
    }

    /// <summary>
    ///     CreateError sets Error property.
    /// </summary>
    [Fact]
    public void CreateErrorSetsError()
    {
        // Arrange
        Exception error = new InvalidOperationException("specific error message");

        // Act
        IAction action = ProjectionActionFactory.CreateError(typeof(TestProjection), "entity-1", error);

        // Assert
        ProjectionErrorAction<TestProjection>
            typedAction = Assert.IsType<ProjectionErrorAction<TestProjection>>(action);
        Assert.Same(error, typedAction.Error);
    }

    /// <summary>
    ///     CreateError throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void CreateErrorThrowsWhenEntityIdIsNull()
    {
        // Arrange
        Exception error = new InvalidOperationException("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateError(typeof(TestProjection), null!, error));
    }

    /// <summary>
    ///     CreateError throws ArgumentNullException when error is null.
    /// </summary>
    [Fact]
    public void CreateErrorThrowsWhenErrorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateError(typeof(TestProjection), "entity-1", null!));
    }

    /// <summary>
    ///     CreateError throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    public void CreateErrorThrowsWhenProjectionTypeIsNull()
    {
        // Arrange
        Exception error = new InvalidOperationException("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionActionFactory.CreateError(null!, "entity-1", error));
    }

    /// <summary>
    ///     CreateLoaded allows null data.
    /// </summary>
    [Fact]
    public void CreateLoadedAllowsNullData()
    {
        // Act
        IAction action = ProjectionActionFactory.CreateLoaded(typeof(TestProjection), "entity-1", null, 1);

        // Assert
        ProjectionLoadedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
        Assert.Null(typedAction.Data);
    }

    /// <summary>
    ///     CreateLoaded returns ProjectionLoadedAction with correct generic type.
    /// </summary>
    [Fact]
    public void CreateLoadedReturnsCorrectType()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "loaded",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateLoaded(typeof(TestProjection), "entity-1", data, 1);

        // Assert
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
    }

    /// <summary>
    ///     CreateLoaded sets Data property.
    /// </summary>
    [Fact]
    public void CreateLoadedSetsData()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "data-value",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateLoaded(typeof(TestProjection), "entity-1", data, 1);

        // Assert
        ProjectionLoadedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
        Assert.Same(data, typedAction.Data);
    }

    /// <summary>
    ///     CreateLoaded sets EntityId property.
    /// </summary>
    [Fact]
    public void CreateLoadedSetsEntityId()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "loaded",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateLoaded(typeof(TestProjection), "entity-456", data, 1);

        // Assert
        ProjectionLoadedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
        Assert.Equal("entity-456", typedAction.EntityId);
    }

    /// <summary>
    ///     CreateLoaded sets Version property.
    /// </summary>
    [Fact]
    public void CreateLoadedSetsVersion()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "loaded",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateLoaded(typeof(TestProjection), "entity-1", data, 42);

        // Assert
        ProjectionLoadedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
        Assert.Equal(42, typedAction.Version);
    }

    /// <summary>
    ///     CreateLoaded throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void CreateLoadedThrowsWhenEntityIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoaded(typeof(TestProjection), null!, null, 1));
    }

    /// <summary>
    ///     CreateLoaded throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    public void CreateLoadedThrowsWhenProjectionTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionActionFactory.CreateLoaded(null!, "entity-1", null, 1));
    }

    /// <summary>
    ///     CreateLoading returns ProjectionLoadingAction with correct generic type.
    /// </summary>
    [Fact]
    public void CreateLoadingReturnsCorrectType()
    {
        // Act
        IAction action = ProjectionActionFactory.CreateLoading(typeof(TestProjection), "entity-1");

        // Assert
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(action);
    }

    /// <summary>
    ///     CreateLoading sets EntityId property.
    /// </summary>
    [Fact]
    public void CreateLoadingSetsEntityId()
    {
        // Act
        IAction action = ProjectionActionFactory.CreateLoading(typeof(TestProjection), "entity-789");

        // Assert
        ProjectionLoadingAction<TestProjection> typedAction =
            Assert.IsType<ProjectionLoadingAction<TestProjection>>(action);
        Assert.Equal("entity-789", typedAction.EntityId);
    }

    /// <summary>
    ///     CreateLoading throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void CreateLoadingThrowsWhenEntityIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateLoading(typeof(TestProjection), null!));
    }

    /// <summary>
    ///     CreateLoading throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    public void CreateLoadingThrowsWhenProjectionTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionActionFactory.CreateLoading(null!, "entity-1"));
    }

    /// <summary>
    ///     CreateUpdated allows null data.
    /// </summary>
    [Fact]
    public void CreateUpdatedAllowsNullData()
    {
        // Act
        IAction action = ProjectionActionFactory.CreateUpdated(typeof(TestProjection), "entity-1", null, 2);

        // Assert
        ProjectionUpdatedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
        Assert.Null(typedAction.Data);
    }

    /// <summary>
    ///     CreateUpdated returns ProjectionUpdatedAction with correct generic type.
    /// </summary>
    [Fact]
    public void CreateUpdatedReturnsCorrectType()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "updated",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateUpdated(typeof(TestProjection), "entity-1", data, 2);

        // Assert
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
    }

    /// <summary>
    ///     CreateUpdated sets Data property.
    /// </summary>
    [Fact]
    public void CreateUpdatedSetsData()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "updated-data",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateUpdated(typeof(TestProjection), "entity-1", data, 2);

        // Assert
        ProjectionUpdatedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
        Assert.Same(data, typedAction.Data);
    }

    /// <summary>
    ///     CreateUpdated sets EntityId property.
    /// </summary>
    [Fact]
    public void CreateUpdatedSetsEntityId()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "updated",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateUpdated(typeof(TestProjection), "entity-updated", data, 2);

        // Assert
        ProjectionUpdatedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
        Assert.Equal("entity-updated", typedAction.EntityId);
    }

    /// <summary>
    ///     CreateUpdated sets Version property.
    /// </summary>
    [Fact]
    public void CreateUpdatedSetsVersion()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "updated",
        };

        // Act
        IAction action = ProjectionActionFactory.CreateUpdated(typeof(TestProjection), "entity-1", data, 99);

        // Assert
        ProjectionUpdatedAction<TestProjection> typedAction =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
        Assert.Equal(99, typedAction.Version);
    }

    /// <summary>
    ///     CreateUpdated throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void CreateUpdatedThrowsWhenEntityIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ProjectionActionFactory.CreateUpdated(typeof(TestProjection), null!, null, 2));
    }

    /// <summary>
    ///     CreateUpdated throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    public void CreateUpdatedThrowsWhenProjectionTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProjectionActionFactory.CreateUpdated(null!, "entity-1", null, 2));
    }
}