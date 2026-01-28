using System;
using System.Reflection;


using Mississippi.Refraction.Abstractions.Motion;


namespace Mississippi.Refraction.Abstractions.L0Tests.Motion;

/// <summary>
///     Contract verification tests for <see cref="IMotionPreferences" />.
/// </summary>
public sealed class IMotionPreferencesTests
{
    /// <summary>
    ///     Verifies the interface defines motion preference properties.
    /// </summary>
    [Fact]
        public void IMotionPreferencesDefinesMotionPreferenceProperties()
    {
        // Arrange
        Type interfaceType = typeof(IMotionPreferences);

        // Assert
        Assert.NotNull(interfaceType.GetProperty(nameof(IMotionPreferences.ReduceMotion)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IMotionPreferences.DurationMultiplier)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IMotionPreferences.EnableEntranceAnimations)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IMotionPreferences.EnableExitAnimations)));
    }

    /// <summary>
    ///     Verifies DurationMultiplier is double type.
    /// </summary>
    [Fact]
        public void IMotionPreferencesDurationMultiplierIsDouble()
    {
        // Arrange
        Type interfaceType = typeof(IMotionPreferences);
        PropertyInfo? prop = interfaceType.GetProperty(nameof(IMotionPreferences.DurationMultiplier));

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(double), prop!.PropertyType);
    }
}