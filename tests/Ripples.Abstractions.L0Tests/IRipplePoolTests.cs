namespace Mississippi.Ripples.Abstractions.L0Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Xunit;

/// <summary>
/// Tests for <see cref="IRipplePool{T}"/> interface contract.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("IRipplePool")]
public sealed class IRipplePoolTests
{
    /// <summary>
    /// Verifies that IRipplePool defines a GetOrCreate method returning IRipple.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void GetOrCreateMethodIsDefined()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);
        var method = type.GetMethod(nameof(IRipplePool<object>.GetOrCreate));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(IRipple<object>), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    /// <summary>
    /// Verifies that IRipplePool defines a MarkVisible method.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void MarkVisibleMethodIsDefined()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);
        var method = type.GetMethod(nameof(IRipplePool<object>.MarkVisible));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    /// <summary>
    /// Verifies that IRipplePool defines a MarkHidden method.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void MarkHiddenMethodIsDefined()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);
        var method = type.GetMethod(nameof(IRipplePool<object>.MarkHidden));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    /// <summary>
    /// Verifies that IRipplePool defines a PrefetchAsync method.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void PrefetchAsyncMethodIsDefined()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);
        var method = type.GetMethod(nameof(IRipplePool<object>.PrefetchAsync));

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(IEnumerable<string>), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    /// <summary>
    /// Verifies that IRipplePool defines a Stats property.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void StatsPropertyIsDefined()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);
        var property = type.GetProperty(nameof(IRipplePool<object>.Stats));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(RipplePoolStats), property!.PropertyType);
        Assert.True(property.CanRead);
    }

    /// <summary>
    /// Verifies that IRipplePool implements IAsyncDisposable.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void ImplementsIAsyncDisposable()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<object>);

        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(type));
    }

    /// <summary>
    /// Verifies that IRipplePool has a class constraint on its generic parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Interface Contract")]
    public void HasClassConstraintOnGenericParameter()
    {
        // Arrange & Act
        var type = typeof(IRipplePool<>);
        var genericArg = type.GetGenericArguments()[0];

        // Assert
        Assert.NotEqual(GenericParameterAttributes.None, genericArg.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint);
    }
}
