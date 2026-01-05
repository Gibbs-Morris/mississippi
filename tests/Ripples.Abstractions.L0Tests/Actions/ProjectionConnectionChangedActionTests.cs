using System;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for <see cref="ProjectionConnectionChangedAction{T}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.Abstractions")]
[AllureSuite("Actions")]
[AllureSubSuite("ProjectionConnectionChangedAction")]
public sealed class ProjectionConnectionChangedActionTests
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
            Assert.Throws<ArgumentNullException>(() =>
                new ProjectionConnectionChangedAction<TestProjection>(null!, true));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should set all properties when given valid parameters.
    /// </summary>
    /// <param name="isConnected">The connection state to test.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [AllureFeature("Action Creation")]
    public void ConstructorWithValidParametersSetsAllProperties(
        bool isConnected
    )
    {
        // Arrange & Act
        ProjectionConnectionChangedAction<TestProjection> action = new(TestEntityId, isConnected);

        // Assert
        Assert.Equal(TestEntityId, action.EntityId);
        Assert.Equal(isConnected, action.IsConnected);
    }

    /// <summary>
    ///     Action should implement IRippleAction interface.
    /// </summary>
    [Fact]
    [AllureFeature("Action Identity")]
    public void ImplementsIRippleAction()
    {
        // Arrange & Act
        ProjectionConnectionChangedAction<TestProjection> action = new(TestEntityId, true);

        // Assert
        Assert.IsType<ProjectionConnectionChangedAction<TestProjection>>(action, false);
    }

    /// <summary>
    ///     ProjectionType property should return the correct generic type argument.
    /// </summary>
    [Fact]
    [AllureFeature("Action Creation")]
    public void ProjectionTypeReturnsCorrectType()
    {
        // Arrange & Act
        ProjectionConnectionChangedAction<TestProjection> action = new(TestEntityId, true);

        // Assert
        Assert.Equal(typeof(TestProjection), action.ProjectionType);
    }
}