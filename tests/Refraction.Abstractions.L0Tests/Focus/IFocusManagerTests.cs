using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Abstractions.Focus;


namespace Mississippi.Refraction.Abstractions.L0Tests.Focus;

/// <summary>
///     Contract verification tests for <see cref="IFocusManager" />.
/// </summary>
[AllureSuite("Refraction.Abstractions")]
[AllureSubSuite("Focus")]
public sealed class IFocusManagerTests
{
    /// <summary>
    ///     Verifies the interface defines focus navigation methods.
    /// </summary>
    [Fact]
    [AllureFeature("IFocusManager")]
    public void IFocusManagerDefinesFocusNavigationMethods()
    {
        // Arrange
        Type interfaceType = typeof(IFocusManager);

        // Assert
        Assert.NotNull(interfaceType.GetMethod(nameof(IFocusManager.TryFocus)));
        Assert.NotNull(interfaceType.GetMethod(nameof(IFocusManager.FocusNext)));
        Assert.NotNull(interfaceType.GetMethod(nameof(IFocusManager.FocusPrevious)));
        Assert.NotNull(interfaceType.GetMethod(nameof(IFocusManager.ClearFocus)));
    }

    /// <summary>
    ///     Verifies TryFocus returns bool.
    /// </summary>
    [Fact]
    [AllureFeature("IFocusManager")]
    public void IFocusManagerTryFocusReturnsBool()
    {
        // Arrange
        Type interfaceType = typeof(IFocusManager);
        MethodInfo? method = interfaceType.GetMethod(nameof(IFocusManager.TryFocus));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }
}