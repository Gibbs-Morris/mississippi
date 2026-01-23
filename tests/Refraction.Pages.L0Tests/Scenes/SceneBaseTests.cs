using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Pages.Scenes;


namespace Mississippi.Refraction.Pages.L0Tests.Scenes;

/// <summary>
///     Tests for <see cref="SceneBase{TState}" />.
/// </summary>
[AllureSuite("Refraction.Pages")]
[AllureSubSuite("Scenes")]
public sealed class SceneBaseTests
{
    /// <summary>
    ///     SceneBase has protected State property.
    /// </summary>
    [Fact]
    [AllureFeature("SceneBase")]
    public void SceneBaseHasProtectedStateProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Act
        PropertyInfo? stateProperty = sceneBaseType.GetProperty(
            "State",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(stateProperty);
    }

    /// <summary>
    ///     SceneBase implements IDisposable.
    /// </summary>
    [Fact]
    [AllureFeature("SceneBase")]
    public void SceneBaseImplementsIDisposable()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(typeof(IDisposable).IsAssignableFrom(sceneBaseType));
    }

    /// <summary>
    ///     SceneBase is abstract.
    /// </summary>
    [Fact]
    [AllureFeature("SceneBase")]
    public void SceneBaseIsAbstract()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(sceneBaseType.IsAbstract);
    }
}