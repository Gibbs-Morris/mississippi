using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="SmokeConfirm" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Organisms")]
public sealed class SmokeConfirmTests
{
    /// <summary>
    ///     SmokeConfirm has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     SmokeConfirm has CancelText parameter with default Cancel.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasCancelTextParameterWithDefaultCancel()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("CancelText");
        SmokeConfirm component = new();

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Cancel", component.CancelText);
    }

    /// <summary>
    ///     SmokeConfirm has ConfirmText parameter with default Confirm.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasConfirmTextParameterWithDefaultConfirm()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("ConfirmText");
        SmokeConfirm component = new();

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Confirm", component.ConfirmText);
    }

    /// <summary>
    ///     SmokeConfirm has Consequence parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasConsequenceParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("Consequence");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     SmokeConfirm has OnCancel EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasOnCancelEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("OnCancel");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop!.PropertyType);
    }

    /// <summary>
    ///     SmokeConfirm has OnConfirm EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasOnConfirmEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("OnConfirm");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop!.PropertyType);
    }

    /// <summary>
    ///     SmokeConfirm has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SmokeConfirm has Title parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmHasTitleParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SmokeConfirm).GetProperty("Title");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SmokeConfirm inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(SmokeConfirm)));
    }

    /// <summary>
    ///     SmokeConfirm State defaults to Latent.
    /// </summary>
    [Fact]
    [AllureFeature("SmokeConfirm")]
    public void SmokeConfirmStateDefaultsToLatent()
    {
        // Arrange
        SmokeConfirm component = new();

        // Assert
        Assert.Equal(RefractionStates.Latent, component.State);
    }
}