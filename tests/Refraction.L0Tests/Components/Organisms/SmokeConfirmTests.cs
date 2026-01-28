using System;
using System.Reflection;


using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="SmokeConfirm" /> component.
/// </summary>
public sealed class SmokeConfirmTests : BunitContext
{
    /// <summary>
    ///     SmokeConfirm does not render consequence element when empty.
    /// </summary>
    [Fact]
        public void SmokeConfirmDoesNotRenderConsequenceWhenEmpty()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Consequence, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-smoke-confirm__consequence"));
    }

    /// <summary>
    ///     SmokeConfirm does not render title element when empty.
    /// </summary>
    [Fact]
        public void SmokeConfirmDoesNotRenderTitleWhenEmpty()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-smoke-confirm__title"));
    }

    /// <summary>
    ///     SmokeConfirm has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
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
        public void SmokeConfirmInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(SmokeConfirm)));
    }

    /// <summary>
    ///     SmokeConfirm invokes OnCancel when cancel button is clicked.
    /// </summary>
    [Fact]
        public void SmokeConfirmInvokesOnCancelWhenCancelButtonClicked()
    {
        // Arrange
        bool wasCancelled = false;
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(
            c => c.OnCancel,
            () => { wasCancelled = true; }));

        // Act
        cut.Find(".rf-smoke-confirm__cancel").Click();

        // Assert
        Assert.True(wasCancelled);
    }

    /// <summary>
    ///     SmokeConfirm invokes OnConfirm when confirm button is clicked.
    /// </summary>
    [Fact]
        public void SmokeConfirmInvokesOnConfirmWhenConfirmButtonClicked()
    {
        // Arrange
        bool wasConfirmed = false;
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(
            c => c.OnConfirm,
            () => { wasConfirmed = true; }));

        // Act
        cut.Find(".rf-smoke-confirm__confirm").Click();

        // Assert
        Assert.True(wasConfirmed);
    }

    /// <summary>
    ///     SmokeConfirm renders action buttons.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersActionButtons()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-smoke-confirm__cancel"));
        Assert.NotEmpty(cut.FindAll(".rf-smoke-confirm__confirm"));
    }

    /// <summary>
    ///     SmokeConfirm renders additional attributes.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.AddUnmatched(
            "data-testid",
            "confirm-1"));

        // Assert
        Assert.Equal("confirm-1", cut.Find(".rf-smoke-confirm").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     SmokeConfirm renders aria-labelledby attribute when title is provided.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersAriaLabelledByWhenTitleProvided()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, "Confirm Delete"));

        // Assert
        string? ariaLabelledby = cut.Find(".rf-smoke-confirm").GetAttribute("aria-labelledby");
        Assert.NotNull(ariaLabelledby);
        Assert.StartsWith("rf-smoke-confirm-title-", ariaLabelledby, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SmokeConfirm renders consequence when provided.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersConsequenceWhenProvided()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(
            c => c.Consequence,
            "This action is irreversible"));

        // Assert
        string textContent = cut.Find(".rf-smoke-confirm__consequence").TextContent;
        Assert.Contains("This action is irreversible", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SmokeConfirm renders custom cancel text.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersCustomCancelText()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.CancelText, "Abort"));

        // Assert
        string textContent = cut.Find(".rf-smoke-confirm__cancel").TextContent;
        Assert.Equal("Abort", textContent);
    }

    /// <summary>
    ///     SmokeConfirm renders custom confirm text.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersCustomConfirmText()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.ConfirmText, "Proceed"));

        // Assert
        string textContent = cut.Find(".rf-smoke-confirm__confirm").TextContent;
        Assert.Equal("Proceed", textContent);
    }

    /// <summary>
    ///     SmokeConfirm renders custom state.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersCustomState()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-smoke-confirm").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     SmokeConfirm renders title when provided.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersTitleWhenProvided()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, "Test Title"));

        // Assert
        string textContent = cut.Find(".rf-smoke-confirm__title").TextContent;
        Assert.Contains("Test Title", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SmokeConfirm renders with default state.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>();

        // Assert
        string? dataState = cut.Find(".rf-smoke-confirm").GetAttribute("data-state");
        Assert.Equal("latent", dataState);
    }

    /// <summary>
    ///     SmokeConfirm renders with dialog role for accessibility.
    /// </summary>
    [Fact]
        public void SmokeConfirmRendersWithDialogRoleForAccessibility()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>();

        // Assert
        string? role = cut.Find(".rf-smoke-confirm").GetAttribute("role");
        Assert.Equal("dialog", role);
    }

    /// <summary>
    ///     SmokeConfirm State defaults to Latent.
    /// </summary>
    [Fact]
        public void SmokeConfirmStateDefaultsToLatent()
    {
        // Arrange
        SmokeConfirm component = new();

        // Assert
        Assert.Equal(RefractionStates.Latent, component.State);
    }
}