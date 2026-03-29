using System;
using System.Reflection;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.StateManagement.Scenes;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Tests for <see cref="ReservoirSceneBase{TState}" />.
/// </summary>
public sealed class ReservoirSceneBaseTests
{
    /// <summary>
    ///     ReservoirSceneBase has Dispose method.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseHasDisposeMethod()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Act
        MethodInfo? disposeMethod = sceneBaseType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public);

        // Assert
        Assert.NotNull(disposeMethod);
    }

    /// <summary>
    ///     ReservoirSceneBase has protected HasError property.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseHasProtectedHasErrorProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Act
        PropertyInfo? hasErrorProperty = sceneBaseType.GetProperty(
            "HasError",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(hasErrorProperty);
    }

    /// <summary>
    ///     ReservoirSceneBase has protected IsLoading property.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseHasProtectedIsLoadingProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Act
        PropertyInfo? isLoadingProperty = sceneBaseType.GetProperty(
            "IsLoading",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(isLoadingProperty);
    }

    /// <summary>
    ///     ReservoirSceneBase has protected State property.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseHasProtectedStateProperty()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Act
        PropertyInfo? stateProperty = sceneBaseType.GetProperty(
            "State",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(stateProperty);
    }

    /// <summary>
    ///     ReservoirSceneBase has protected Store property with Inject attribute.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseHasProtectedStorePropertyWithInject()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

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
    ///     ReservoirSceneBase implements IDisposable.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseImplementsIDisposable()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Assert
        Assert.True(typeof(IDisposable).IsAssignableFrom(sceneBaseType));
    }

    /// <summary>
    ///     ReservoirSceneBase inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseInheritsFromComponentBase()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(sceneBaseType));
    }

    /// <summary>
    ///     ReservoirSceneBase is abstract.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseIsAbstract()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Assert
        Assert.True(sceneBaseType.IsAbstract);
    }

    /// <summary>
    ///     ReservoirSceneBase is generic with one type parameter.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseIsGenericWithOneTypeParameter()
    {
        // Arrange
        Type sceneBaseType = typeof(ReservoirSceneBase<>);

        // Assert
        Assert.True(sceneBaseType.IsGenericTypeDefinition);
        Assert.Single(sceneBaseType.GetGenericArguments());
    }
}