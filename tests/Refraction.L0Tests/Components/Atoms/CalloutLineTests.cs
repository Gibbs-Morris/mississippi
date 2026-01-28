using System;
using System.Reflection;


using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="CalloutLine" /> component.
/// </summary>
public sealed class CalloutLineTests : BunitContext
{
    /// <summary>
    ///     CalloutLine does not render label when empty.
    /// </summary>
    [Fact]
        public void CalloutLineDoesNotRenderLabelWhenEmpty()
    {
        // Act
        using IRenderedComponent<CalloutLine> cut = Render<CalloutLine>(p => p.Add(c => c.Label, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-callout-line__label"));
    }

    /// <summary>
    ///     CalloutLine has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
        public void CalloutLineHasAdditionalAttributesParameterWithCaptureUnmatchedValues()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     CalloutLine has Label parameter.
    /// </summary>
    [Fact]
        public void CalloutLineHasLabelParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("Label");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     CalloutLine has State parameter.
    /// </summary>
    [Fact]
        public void CalloutLineHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     CalloutLine inherits from ComponentBase.
    /// </summary>
    [Fact]
        public void CalloutLineInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(CalloutLine)));
    }

    /// <summary>
    ///     CalloutLine renders additional attributes.
    /// </summary>
    [Fact]
        public void CalloutLineRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<CalloutLine>
            cut = Render<CalloutLine>(p => p.AddUnmatched("data-testid", "callout-1"));

        // Assert
        Assert.Equal("callout-1", cut.Find(".rf-callout-line").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     CalloutLine renders custom state.
    /// </summary>
    [Fact]
        public void CalloutLineRendersCustomState()
    {
        // Act
        using IRenderedComponent<CalloutLine> cut = Render<CalloutLine>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-callout-line").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     CalloutLine renders label when provided.
    /// </summary>
    [Fact]
        public void CalloutLineRendersLabelWhenProvided()
    {
        // Act
        using IRenderedComponent<CalloutLine> cut = Render<CalloutLine>(p => p.Add(c => c.Label, "Test Label"));

        // Assert
        string textContent = cut.Find(".rf-callout-line__label").TextContent;
        Assert.Contains("Test Label", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     CalloutLine renders with default state.
    /// </summary>
    [Fact]
        public void CalloutLineRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<CalloutLine> cut = Render<CalloutLine>();

        // Assert
        string? dataState = cut.Find(".rf-callout-line").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     CalloutLine State defaults to Idle.
    /// </summary>
    [Fact]
        public void CalloutLineStateDefaultsToIdle()
    {
        // Arrange
        CalloutLine component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}