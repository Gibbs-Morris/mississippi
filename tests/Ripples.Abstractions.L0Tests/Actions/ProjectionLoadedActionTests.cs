using System;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for <see cref="ProjectionLoadedAction{T}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.Abstractions")]
[AllureSuite("Actions")]
[AllureSubSuite("ProjectionLoadedAction")]
public sealed class ProjectionLoadedActionTests
{
    private const string TestEntityId = "test-entity-123";

    private const long TestVersion = 42L;

    private sealed record TestProjection(string Name);

    /// <summary>
    ///     Constructor should allow null data.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ConstructorWithNullDataSetsDataToNull()
    {
        // Arrange & Act
        ProjectionLoadedAction<TestProjection> action = new(TestEntityId, null, TestVersion);

        // Assert
        Assert.Equal(TestEntityId, action.EntityId);
        Assert.Null(action.Data);
        Assert.Equal(TestVersion, action.Version);
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void ConstructorWithNullEntityIdThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ProjectionLoadedAction<TestProjection>(null!, new("Test"), TestVersion));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should set all properties when given valid parameters.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ConstructorWithValidParametersSetsAllProperties()
    {
        // Arrange
        TestProjection data = new("Test");

        // Act
        ProjectionLoadedAction<TestProjection> action = new(TestEntityId, data, TestVersion);

        // Assert
        Assert.Equal(TestEntityId, action.EntityId);
        Assert.Equal(data, action.Data);
        Assert.Equal(TestVersion, action.Version);
    }

    /// <summary>
    ///     Action should implement IRippleAction interface.
    /// </summary>
    [Fact]
    [AllureFeature("Action Identity")]
    public void ImplementsIRippleAction()
    {
        // Arrange & Act
        ProjectionLoadedAction<TestProjection> action = new(TestEntityId, null, TestVersion);

        // Assert
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(action, false);
    }
}