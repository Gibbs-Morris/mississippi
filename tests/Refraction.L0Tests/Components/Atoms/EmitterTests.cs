using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="Emitter" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class EmitterTests : BunitContext
{
    /// <summary>
    ///     Emitter has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     Emitter has OnActivate EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasOnActivateEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("OnActivate");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter has OnFocus EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasOnFocusEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("OnFocus");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Emitter)));
    }

    /// <summary>
    ///     Emitter State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterStateDefaultsToIdle()
    {
        // Arrange
        Emitter emitter = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, emitter.State);
    }

    /// <summary>
    ///     Emitter renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersWithDefaultState()
    {
        // Act
        using var cut = Render<Emitter>();

        // Assert
        string? dataState = cut.Find(".rf-emitter").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     Emitter renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersCustomState()
    {
        // Act
        using var cut = Render<Emitter>(p => p
            .Add(c => c.State, RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-emitter").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     Emitter renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersAdditionalAttributes()
    {
        // Act
        using var cut = Render<Emitter>(p => p
            .AddUnmatched("data-testid", "emitter-1"));

        // Assert
        Assert.Equal("emitter-1", cut.Find(".rf-emitter").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     Emitter renders seed indicator.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersSeedIndicator()
    {
        // Act
        using var cut = Render<Emitter>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-emitter__seed"));
    }
}