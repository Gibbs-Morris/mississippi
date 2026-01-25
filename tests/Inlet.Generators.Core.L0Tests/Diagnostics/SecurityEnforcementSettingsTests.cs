using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Generators.Core.Diagnostics;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="SecurityEnforcementSettings" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Security Enforcement Settings")]
public class SecurityEnforcementSettingsTests
{
    /// <summary>
    ///     Constructor should set properties correctly.
    /// </summary>
    [Fact]
    public void ConstructorSetsPropertiesCorrectly()
    {
        List<string> exemptTypes = ["MyApp.HealthCheck", "MyApp.MetricsProjection"];
        SecurityEnforcementSettings settings = new(true, exemptTypes);
        Assert.True(settings.TreatAnonymousAsError);
        Assert.Equal(2, settings.ExemptTypes.Count);
    }

    /// <summary>
    ///     Constructor should throw when exemptTypes is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenExemptTypesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SecurityEnforcementSettings(true, null!));
    }

    /// <summary>
    ///     IsTypeExempt should return false for null or empty type name.
    /// </summary>
    [Fact]
    public void IsTypeExemptReturnsFalseForNullOrEmptyTypeName()
    {
        SecurityEnforcementSettings settings = new(true, ["SomeType"]);
        Assert.False(settings.IsTypeExempt(null!));
        Assert.False(settings.IsTypeExempt(string.Empty));
    }

    /// <summary>
    ///     IsTypeExempt should return false when empty exempt list.
    /// </summary>
    [Fact]
    public void IsTypeExemptReturnsFalseWhenEmptyExemptList()
    {
        SecurityEnforcementSettings settings = new(true, []);
        Assert.False(settings.IsTypeExempt("MyApp.SomeType"));
    }

    /// <summary>
    ///     IsTypeExempt should return false when type is not in exempt list.
    /// </summary>
    [Fact]
    public void IsTypeExemptReturnsFalseWhenNotExempt()
    {
        SecurityEnforcementSettings settings = new(true, ["MyApp.HealthCheck", "MyApp.MetricsProjection"]);
        Assert.False(settings.IsTypeExempt("MyApp.BankAccount"));
        Assert.False(settings.IsTypeExempt("OtherApp.HealthCheck"));
    }

    /// <summary>
    ///     IsTypeExempt should return true for exact match.
    /// </summary>
    [Fact]
    public void IsTypeExemptReturnsTrueForExactMatch()
    {
        SecurityEnforcementSettings settings = new(true, ["MyApp.HealthCheck"]);
        Assert.True(settings.IsTypeExempt("MyApp.HealthCheck"));
    }

    /// <summary>
    ///     IsTypeExempt should return true for suffix match.
    /// </summary>
    [Fact]
    public void IsTypeExemptReturnsTrueForSuffixMatch()
    {
        SecurityEnforcementSettings settings = new(true, ["HealthCheck"]);
        Assert.True(settings.IsTypeExempt("MyApp.Domain.HealthCheck"));
        Assert.True(settings.IsTypeExempt("OtherApp.HealthCheck"));
    }

    /// <summary>
    ///     TreatAnonymousAsError should reflect constructor value.
    /// </summary>
    [Fact]
    public void TreatAnonymousAsErrorReflectsConstructorValue()
    {
        SecurityEnforcementSettings settingsTrue = new(true, []);
        SecurityEnforcementSettings settingsFalse = new(false, []);
        Assert.True(settingsTrue.TreatAnonymousAsError);
        Assert.False(settingsFalse.TreatAnonymousAsError);
    }
}