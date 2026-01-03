using System;
using System.Reflection;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="IRipple{T}" /> interface contract.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("IRipple")]
public sealed class IRippleTests
{
    /// <summary>
    ///     Verifies that IRipple defines a Changed event using EventHandler.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void ChangedEventIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        EventInfo? eventInfo = type.GetEvent(nameof(IRipple<object>.Changed));

        // Assert
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler), eventInfo!.EventHandlerType);
    }

    /// <summary>
    ///     Verifies that IRipple defines a Current property of generic type T.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void CurrentPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.Current));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(type.GetGenericArguments()[0], property!.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    /// <summary>
    ///     Verifies that IRipple defines an ErrorOccurred event using EventHandler of RippleErrorEventArgs.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void ErrorOccurredEventIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        EventInfo? eventInfo = type.GetEvent(nameof(IRipple<object>.ErrorOccurred));

        // Assert
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler<RippleErrorEventArgs>), eventInfo!.EventHandlerType);
    }

    /// <summary>
    ///     Verifies that IRipple has a class constraint on its generic parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void HasClassConstraintOnGenericParameter()
    {
        // Arrange & Act
        Type type = typeof(IRipple<>);
        Type genericArg = type.GetGenericArguments()[0];

        // Assert
        Assert.NotEqual(
            GenericParameterAttributes.None,
            genericArg.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint);
    }

    /// <summary>
    ///     Verifies that IRipple implements IAsyncDisposable.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void ImplementsIAsyncDisposable()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);

        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(type));
    }

    /// <summary>
    ///     Verifies that IRipple defines an IsConnected property of bool type.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void IsConnectedPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.IsConnected));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
    }

    /// <summary>
    ///     Verifies that IRipple defines an IsLoaded property of bool type.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void IsLoadedPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.IsLoaded));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
    }

    /// <summary>
    ///     Verifies that IRipple defines an IsLoading property of bool type.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void IsLoadingPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.IsLoading));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
    }

    /// <summary>
    ///     Verifies that IRipple defines a LastError property of Exception type.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void LastErrorPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.LastError));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(Exception), property!.PropertyType);
    }

    /// <summary>
    ///     Verifies that IRipple defines a RefreshAsync method returning Task.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void RefreshAsyncMethodIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        MethodInfo? method = type.GetMethod(nameof(IRipple<object>.RefreshAsync));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    /// <summary>
    ///     Verifies that IRipple defines a SubscribeAsync method returning Task.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void SubscribeAsyncMethodIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        MethodInfo? method = type.GetMethod(nameof(IRipple<object>.SubscribeAsync));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    /// <summary>
    ///     Verifies that IRipple defines an UnsubscribeAsync method returning Task.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void UnsubscribeAsyncMethodIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        MethodInfo? method = type.GetMethod(nameof(IRipple<object>.UnsubscribeAsync));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    /// <summary>
    ///     Verifies that IRipple defines a Version property of nullable long type.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void VersionPropertyIsDefined()
    {
        // Arrange & Act
        Type type = typeof(IRipple<object>);
        PropertyInfo? property = type.GetProperty(nameof(IRipple<object>.Version));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(long?), property!.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }
}