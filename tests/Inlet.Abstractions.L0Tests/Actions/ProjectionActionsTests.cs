using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions.Actions;


namespace Mississippi.Inlet.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for projection action classes.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Abstractions")]
[AllureSuite("Actions")]
[AllureSubSuite("ProjectionActions")]
public sealed class ProjectionActionsTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionConnectionChangedAction")]
    public void ProjectionConnectionChangedActionConstructorSetsProperties()
    {
        // Act
        ProjectionConnectionChangedAction<TestProjection> sut = new("entity-1", true);

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.True(sut.IsConnected);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     ProjectionConnectionChangedAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionConnectionChangedActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionConnectionChangedAction<TestProjection>(null!, true));
    }

    /// <summary>
    ///     ProjectionErrorAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionErrorAction")]
    public void ProjectionErrorActionConstructorSetsProperties()
    {
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act
        ProjectionErrorAction<TestProjection> sut = new("entity-1", error);

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(error, sut.Error);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     ProjectionErrorAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionErrorActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectionErrorAction<TestProjection>(null!, new InvalidOperationException()));
    }

    /// <summary>
    ///     ProjectionErrorAction constructor should throw for null error.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionErrorActionWithNullErrorThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionErrorAction<TestProjection>("entity-1", null!));
    }

    /// <summary>
    ///     ProjectionLoadedAction can have null data.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionLoadedAction")]
    public void ProjectionLoadedActionAllowsNullData()
    {
        // Act
        ProjectionLoadedAction<TestProjection> sut = new("entity-1", null, 0L);

        // Assert
        Assert.Null(sut.Data);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionLoadedAction")]
    public void ProjectionLoadedActionConstructorSetsProperties()
    {
        // Arrange
        TestProjection data = new("Test");

        // Act
        ProjectionLoadedAction<TestProjection> sut = new("entity-1", data, 5L);

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(data, sut.Data);
        Assert.Equal(5L, sut.Version);
    }

    /// <summary>
    ///     ProjectionLoadedAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionLoadedActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionLoadedAction<TestProjection>(null!, null, 0));
    }

    /// <summary>
    ///     ProjectionLoadingAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionLoadingAction")]
    public void ProjectionLoadingActionConstructorSetsProperties()
    {
        // Act
        ProjectionLoadingAction<TestProjection> sut = new("entity-1");

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     ProjectionLoadingAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionLoadingActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionLoadingAction<TestProjection>(null!));
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionUpdatedAction")]
    public void ProjectionUpdatedActionConstructorSetsProperties()
    {
        // Arrange
        TestProjection data = new("Test");

        // Act
        ProjectionUpdatedAction<TestProjection> sut = new("entity-1", data, 10L);

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(data, sut.Data);
        Assert.Equal(10L, sut.Version);
    }

    /// <summary>
    ///     ProjectionUpdatedAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ProjectionUpdatedActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectionUpdatedAction<TestProjection>(null!, null, 0));
    }

    /// <summary>
    ///     RefreshProjectionAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("RefreshProjectionAction")]
    public void RefreshProjectionActionConstructorSetsProperties()
    {
        // Act
        RefreshProjectionAction<TestProjection> sut = new("entity-1");

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     RefreshProjectionAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void RefreshProjectionActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RefreshProjectionAction<TestProjection>(null!));
    }

    /// <summary>
    ///     SubscribeToProjectionAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("SubscribeToProjectionAction")]
    public void SubscribeToProjectionActionConstructorSetsProperties()
    {
        // Act
        SubscribeToProjectionAction<TestProjection> sut = new("entity-1");

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     SubscribeToProjectionAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SubscribeToProjectionActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SubscribeToProjectionAction<TestProjection>(null!));
    }

    /// <summary>
    ///     UnsubscribeFromProjectionAction constructor should set properties.
    /// </summary>
    [Fact]
    [AllureFeature("UnsubscribeFromProjectionAction")]
    public void UnsubscribeFromProjectionActionConstructorSetsProperties()
    {
        // Act
        UnsubscribeFromProjectionAction<TestProjection> sut = new("entity-1");

        // Assert
        Assert.Equal("entity-1", sut.EntityId);
        Assert.Equal(typeof(TestProjection), sut.ProjectionType);
    }

    /// <summary>
    ///     UnsubscribeFromProjectionAction constructor should throw for null entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void UnsubscribeFromProjectionActionWithNullEntityIdThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnsubscribeFromProjectionAction<TestProjection>(null!));
    }
}