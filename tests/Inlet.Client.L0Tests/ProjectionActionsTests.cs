using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for projection actions.
/// </summary>
public sealed class ProjectionActionsTests
{
    /// <summary>
    ///     ProjectionConnectionChangedAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void ProjectionConnectionChangedActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "conn-entity";

        // Act
        ProjectionConnectionChangedAction<TestProjection> action = new(entityId, true);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor sets IsConnected to false.
    /// </summary>
    [Fact]
    public void ProjectionConnectionChangedActionConstructorSetsIsConnectedFalse()
    {
        // Act
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-123", false);

        // Assert
        Assert.False(action.IsConnected);
    }

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor sets IsConnected to true.
    /// </summary>
    [Fact]
    public void ProjectionConnectionChangedActionConstructorSetsIsConnectedTrue()
    {
        // Act
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-123", true);

        // Assert
        Assert.True(action.IsConnected);
    }

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void ProjectionConnectionChangedActionConstructorSetsProjectionType()
    {
        // Act
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-123", true);

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void ProjectionConnectionChangedActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionConnectionChangedAction<TestProjection>(null!, true));
    }

    /// <summary>
    ///     ProjectionErrorAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void ProjectionErrorActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "entity-error";
        Exception error = new InvalidOperationException("Test error");

        // Act
        ProjectionErrorAction<TestProjection> action = new(entityId, error);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     ProjectionErrorAction constructor sets Error.
    /// </summary>
    [Fact]
    public void ProjectionErrorActionConstructorSetsError()
    {
        // Arrange
        Exception error = new InvalidOperationException("Test error");

        // Act
        ProjectionErrorAction<TestProjection> action = new("entity-123", error);

        // Assert
        Assert.Same(error, action.Error);
    }

    /// <summary>
    ///     ProjectionErrorAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void ProjectionErrorActionConstructorSetsProjectionType()
    {
        // Arrange
        Exception error = new InvalidOperationException("Test");

        // Act
        ProjectionErrorAction<TestProjection> action = new("entity-123", error);

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     ProjectionErrorAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void ProjectionErrorActionConstructorThrowsOnNullEntityId()
    {
        // Arrange
        Exception error = new InvalidOperationException("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionErrorAction<TestProjection>(null!, error));
    }

    /// <summary>
    ///     ProjectionErrorAction constructor throws on null error.
    /// </summary>
    [Fact]
    public void ProjectionErrorActionConstructorThrowsOnNullError()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionErrorAction<TestProjection>("entity-123", null!));
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor allows null Data.
    /// </summary>
    [Fact]
    public void ProjectionLoadedActionConstructorAllowsNullData()
    {
        // Act
        ProjectionLoadedAction<TestProjection> action = new("entity-123", null, 1);

        // Assert
        Assert.Null(action.Data);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor sets Data.
    /// </summary>
    [Fact]
    public void ProjectionLoadedActionConstructorSetsData()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "Test Data",
        };

        // Act
        ProjectionLoadedAction<TestProjection> action = new("entity-123", data, 1);

        // Assert
        Assert.Same(data, action.Data);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void ProjectionLoadedActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "entity-456";
        TestProjection data = new()
        {
            Name = "Test",
        };

        // Act
        ProjectionLoadedAction<TestProjection> action = new(entityId, data, 1);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor sets Version.
    /// </summary>
    [Fact]
    public void ProjectionLoadedActionConstructorSetsVersion()
    {
        // Arrange
        long version = 42;

        // Act
        ProjectionLoadedAction<TestProjection> action = new("entity-123", new(), version);

        // Assert
        Assert.Equal(version, action.Version);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void ProjectionLoadedActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionLoadedAction<TestProjection>(null!, null, 1));
    }

    /// <summary>
    ///     ProjectionLoadingAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void ProjectionLoadingActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "entity-123";

        // Act
        ProjectionLoadingAction<TestProjection> action = new(entityId);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     ProjectionLoadingAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void ProjectionLoadingActionConstructorSetsProjectionType()
    {
        // Act
        ProjectionLoadingAction<TestProjection> action = new("entity-123");

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     ProjectionLoadingAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void ProjectionLoadingActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionLoadingAction<TestProjection>(null!));
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor sets Data.
    /// </summary>
    [Fact]
    public void ProjectionUpdatedActionConstructorSetsData()
    {
        // Arrange
        TestProjection data = new()
        {
            Name = "Updated Data",
        };

        // Act
        ProjectionUpdatedAction<TestProjection> action = new("entity-123", data, 2);

        // Assert
        Assert.Same(data, action.Data);
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void ProjectionUpdatedActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "entity-789";

        // Act
        ProjectionUpdatedAction<TestProjection> action = new(entityId, new(), 2);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor sets Version.
    /// </summary>
    [Fact]
    public void ProjectionUpdatedActionConstructorSetsVersion()
    {
        // Arrange
        long version = 99;

        // Act
        ProjectionUpdatedAction<TestProjection> action = new("entity-123", new(), version);

        // Assert
        Assert.Equal(version, action.Version);
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void ProjectionUpdatedActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionUpdatedAction<TestProjection>(null!, null, 1));
    }

    /// <summary>
    ///     RefreshProjectionAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void RefreshProjectionActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "refresh-entity";

        // Act
        RefreshProjectionAction<TestProjection> action = new(entityId);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     RefreshProjectionAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void RefreshProjectionActionConstructorSetsProjectionType()
    {
        // Act
        RefreshProjectionAction<TestProjection> action = new("entity-123");

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     RefreshProjectionAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void RefreshProjectionActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RefreshProjectionAction<TestProjection>(null!));
    }

    /// <summary>
    ///     SubscribeToProjectionAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void SubscribeToProjectionActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "sub-entity";

        // Act
        SubscribeToProjectionAction<TestProjection> action = new(entityId);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     SubscribeToProjectionAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void SubscribeToProjectionActionConstructorSetsProjectionType()
    {
        // Act
        SubscribeToProjectionAction<TestProjection> action = new("entity-123");

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     SubscribeToProjectionAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void SubscribeToProjectionActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SubscribeToProjectionAction<TestProjection>(null!));
    }

    /// <summary>
    ///     UnsubscribeFromProjectionAction constructor sets EntityId.
    /// </summary>
    [Fact]
    public void UnsubscribeFromProjectionActionConstructorSetsEntityId()
    {
        // Arrange
        string entityId = "unsub-entity";

        // Act
        UnsubscribeFromProjectionAction<TestProjection> action = new(entityId);

        // Assert
        Assert.Equal(entityId, action.EntityId);
    }

    /// <summary>
    ///     UnsubscribeFromProjectionAction constructor sets ProjectionType.
    /// </summary>
    [Fact]
    public void UnsubscribeFromProjectionActionConstructorSetsProjectionType()
    {
        // Act
        UnsubscribeFromProjectionAction<TestProjection> action = new("entity-123");

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }

    /// <summary>
    ///     UnsubscribeFromProjectionAction constructor throws on null entityId.
    /// </summary>
    [Fact]
    public void UnsubscribeFromProjectionActionConstructorThrowsOnNullEntityId()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnsubscribeFromProjectionAction<TestProjection>(null!));
    }
}