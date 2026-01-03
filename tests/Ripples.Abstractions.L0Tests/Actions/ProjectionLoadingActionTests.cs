using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for <see cref="ProjectionLoadingAction{T}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.Abstractions")]
[AllureSuite("Actions")]
[AllureSubSuite("ProjectionLoadingAction")]
public sealed class ProjectionLoadingActionTests
{
    private const string TestEntityId = "test-entity-123";

    private sealed record TestProjection(string Name);

    /// <summary>
    ///     Constructor should throw ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void ConstructorWithNullEntityIdThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new ProjectionLoadingAction<TestProjection>(null!));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should set EntityId when given a valid entity identifier.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ConstructorWithValidEntityIdSetsEntityId()
    {
        // Arrange & Act
        ProjectionLoadingAction<TestProjection> action = new(TestEntityId);

        // Assert
        Assert.Equal(TestEntityId, action.EntityId);
    }

    /// <summary>
    ///     Action should implement IRippleAction interface.
    /// </summary>
    [Fact]
    [AllureFeature("Action Identity")]
    public void ImplementsIRippleAction()
    {
        // Arrange & Act
        ProjectionLoadingAction<TestProjection> action = new(TestEntityId);

        // Assert
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(action, false);
    }

    /// <summary>
    ///     ProjectionType property should return the correct generic type argument.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ProjectionTypeReturnsCorrectType()
    {
        // Arrange & Act
        ProjectionLoadingAction<TestProjection> action = new(TestEntityId);

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }
}