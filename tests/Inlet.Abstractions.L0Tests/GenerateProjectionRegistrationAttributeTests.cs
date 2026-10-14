using System;
using System.Reflection;

using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Abstractions.L0Tests;

/// <summary>
///     Tests the projection runtime marker's declaration contract.
/// </summary>
public sealed class GenerateProjectionRegistrationAttributeTests
{
    /// <summary>
    ///     Runtime opt-in belongs to one class declaration without inherited settings.
    /// </summary>
    [Fact]
    public void MarkerAppliesOnceToTheDeclaringClass()
    {
        Type markerType = typeof(GenerateProjectionRegistrationAttribute);
        AttributeUsageAttribute? usage = markerType.GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
        Assert.Empty(markerType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(markerType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }
}