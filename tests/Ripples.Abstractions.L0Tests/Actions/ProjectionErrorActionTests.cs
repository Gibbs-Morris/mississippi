using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for <see cref="ProjectionErrorAction{T}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.Abstractions")]
[AllureSuite("Actions")]
[AllureSubSuite("ProjectionErrorAction")]
public sealed class ProjectionErrorActionTests
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
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new ProjectionErrorAction<TestProjection>(null!, error));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when error is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void ConstructorWithNullErrorThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new ProjectionErrorAction<TestProjection>(TestEntityId, null!));
        Assert.Equal("error", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should set all properties when given valid parameters.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ConstructorWithValidParametersSetsAllProperties()
    {
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act
        ProjectionErrorAction<TestProjection> action = new(TestEntityId, error);

        // Assert
        Assert.Equal(TestEntityId, action.EntityId);
        Assert.Same(error, action.Error);
    }

    /// <summary>
    ///     Action should implement IRippleAction interface.
    /// </summary>
    [Fact]
    [AllureFeature("Action Identity")]
    public void ImplementsIRippleAction()
    {
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act
        ProjectionErrorAction<TestProjection> action = new(TestEntityId, error);

        // Assert
        Assert.IsType<ProjectionErrorAction<TestProjection>>(action, false);
    }

    /// <summary>
    ///     ProjectionType property should return the correct generic type argument.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ProjectionTypeReturnsCorrectType()
    {
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act
        ProjectionErrorAction<TestProjection> action = new(TestEntityId, error);

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }
}