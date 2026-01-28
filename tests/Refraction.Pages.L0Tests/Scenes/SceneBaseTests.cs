using System;
using System.Reflection;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Pages.Scenes;


namespace Mississippi.Refraction.Pages.L0Tests.Scenes;

/// <summary>
///     Tests for <see cref="SceneBase{TState}" />.
/// </summary>
public sealed class SceneBaseTests
{
    /// <summary>
    ///     SceneBase has Dispose method.
    /// </summary>
    [Fact]
    public void SceneBaseHasDisposeMethod()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Act
        MethodInfo? disposeMethod = sceneBaseType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public);

        // Assert
        Assert.NotNull(disposeMethod);
    }

    /// <summary>
    ///     SceneBase has protected HasError property.
    /// </summary>
    [Fact]
    public void SceneBaseHasProtectedHasErrorProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Act
        PropertyInfo? hasErrorProperty = sceneBaseType.GetProperty(
            "HasError",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(hasErrorProperty);
    }

    /// <summary>
    ///     SceneBase has protected IsLoading property.
    /// </summary>
    [Fact]
    public void SceneBaseHasProtectedIsLoadingProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Act
        PropertyInfo? isLoadingProperty = sceneBaseType.GetProperty(
            "IsLoading",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(isLoadingProperty);
    }

    /// <summary>
    ///     SceneBase has protected State property.
    /// </summary>
    [Fact]
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
    ///     SceneBase has protected Store property with Inject attribute.
    /// </summary>
    [Fact]
    public void SceneBaseHasProtectedStorePropertyWithInject()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Act
        PropertyInfo? storeProperty = sceneBaseType.GetProperty(
            "Store",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(storeProperty);
        InjectAttribute? injectAttr = storeProperty!.GetCustomAttribute<InjectAttribute>();
        Assert.NotNull(injectAttr);
    }

    /// <summary>
    ///     SceneBase implements IDisposable.
    /// </summary>
    [Fact]
    public void SceneBaseImplementsIDisposable()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(typeof(IDisposable).IsAssignableFrom(sceneBaseType));
    }

    /// <summary>
    ///     SceneBase inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void SceneBaseInheritsFromComponentBase()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(sceneBaseType));
    }

    /// <summary>
    ///     SceneBase is abstract.
    /// </summary>
    [Fact]
    public void SceneBaseIsAbstract()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(sceneBaseType.IsAbstract);
    }

    /// <summary>
    ///     SceneBase is generic with one type parameter.
    /// </summary>
    [Fact]
    public void SceneBaseIsGenericWithOneTypeParameter()
    {
        // Arrange
        Type sceneBaseType = typeof(SceneBase<>);

        // Assert
        Assert.True(sceneBaseType.IsGenericTypeDefinition);
        Assert.Single(sceneBaseType.GetGenericArguments());
    }
}