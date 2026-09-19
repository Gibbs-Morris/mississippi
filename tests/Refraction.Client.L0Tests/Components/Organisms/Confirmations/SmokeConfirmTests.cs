using System;
using System.Collections.Generic;
using System.Reflection;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Organisms.Confirmations;


namespace Mississippi.Refraction.Client.L0Tests.Components.Organisms.Confirmations;

/// <summary>
///     Tests for <see cref="SmokeConfirm" /> component.
/// </summary>
public sealed class SmokeConfirmTests : BunitContext
{
    /// <summary>
    ///     SmokeConfirm composes caller classes and descriptions while retaining owned semantics.
    /// </summary>
    [Fact]
    public void SmokeConfirmComposesCallerAttributesWithoutOverridingOwnedSemantics()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["class"] = "caller-class",
            ["aria-describedby"] = "help-text help-text",
            ["aria-labelledby"] = "caller-title",
            ["role"] = "alert",
            ["data-state"] = RefractionStates.Error,
            ["data-testid"] = "confirm-1",
        };

        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Class, "application-confirm")
            .Add(c => c.Consequence, "This action is irreversible")
            .Add(c => c.State, RefractionStates.Active)
            .Add(c => c.AdditionalAttributes, attributes));

        // Assert
        IElement root = cut.Find(".rf-smoke-confirm");
        string? titleId = cut.Find(".rf-smoke-confirm__title").Id;
        string? consequenceId = cut.Find(".rf-smoke-confirm__consequence").Id;
        Assert.Contains("rf-smoke-confirm", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("application-confirm", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("caller-class", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("dialog", root.GetAttribute("role"));
        Assert.Equal(RefractionStates.Active, root.GetAttribute("data-state"));
        Assert.Equal(titleId, root.GetAttribute("aria-labelledby"));
        Assert.Equal($"help-text {consequenceId}", root.GetAttribute("aria-describedby"));
        Assert.Equal("confirm-1", root.GetAttribute("data-testid"));
    }

    /// <summary>
    ///     SmokeConfirm disables each action independently and updates native state as callbacks change.
    /// </summary>
    [Fact]
    public void SmokeConfirmDisablesActionsWithoutCallbacksAndUpdatesAvailability()
    {
        // Arrange
        int cancelCount = 0;
        int confirmCount = 0;
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.OnCancel, () => cancelCount++));

        // Assert
        IElement cancel = cut.Find(".rf-smoke-confirm__cancel");
        IElement confirm = cut.Find(".rf-smoke-confirm__confirm");
        Assert.False(cancel.HasAttribute("disabled"));
        Assert.True(confirm.HasAttribute("disabled"));

        // Act
        cancel.Click();
        confirm.Click();

        // Assert
        Assert.Equal(1, cancelCount);
        Assert.Equal(0, confirmCount);

        // Act
        cut.Render(p => p.Add(c => c.OnCancel, default(EventCallback)).Add(c => c.OnConfirm, () => confirmCount++));

        // Assert
        Assert.True(cut.Find(".rf-smoke-confirm__cancel").HasAttribute("disabled"));
        Assert.False(cut.Find(".rf-smoke-confirm__confirm").HasAttribute("disabled"));
        cut.Find(".rf-smoke-confirm__cancel").Click();
        cut.Find(".rf-smoke-confirm__confirm").Click();
        Assert.Equal(1, cancelCount);
        Assert.Equal(1, confirmCount);
    }

    /// <summary>
    ///     SmokeConfirm does not render consequence element when empty.
    /// </summary>
    [Fact]
    public void SmokeConfirmDoesNotRenderConsequenceWhenEmpty()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Consequence, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-smoke-confirm__consequence"));
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr.CaptureUnmatchedValues);
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop.PropertyType);
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
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
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
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p =>
            p.Add(c => c.Title, "Delete item").Add(c => c.OnCancel, () => { wasCancelled = true; }));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p =>
            p.Add(c => c.Title, "Delete item").Add(c => c.OnConfirm, () => { wasConfirmed = true; }));

        // Act
        cut.Find(".rf-smoke-confirm__confirm").Click();

        // Assert
        Assert.True(wasConfirmed);
    }

    /// <summary>
    ///     SmokeConfirm keeps its generated relationships stable while consequence content changes.
    /// </summary>
    [Fact]
    public void SmokeConfirmKeepsStableHeadingAndConsequenceRelationships()
    {
        // Arrange
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Consequence, "The first consequence"));
        IElement root = cut.Find(".rf-smoke-confirm");
        string? titleId = root.GetAttribute("aria-labelledby");
        string? consequenceId = cut.Find(".rf-smoke-confirm__consequence").Id;
        using IRenderedComponent<SmokeConfirm> second = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Archive item")
            .Add(c => c.Consequence, "The second consequence"));
        Assert.NotEqual(titleId, second.Find(".rf-smoke-confirm__title").Id);
        Assert.NotEqual(consequenceId, second.Find(".rf-smoke-confirm__consequence").Id);

        // Act
        cut.Render(p => p.Add(c => c.Consequence, string.Empty));

        // Assert
        IElement updatedRoot = cut.Find(".rf-smoke-confirm");
        Assert.Equal(titleId, updatedRoot.GetAttribute("aria-labelledby"));
        Assert.Null(updatedRoot.GetAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll(".rf-smoke-confirm__consequence"));

        // Act
        cut.Render(p => p.Add(c => c.Consequence, "The second consequence"));

        // Assert
        Assert.Equal(titleId, cut.Find(".rf-smoke-confirm").GetAttribute("aria-labelledby"));
        Assert.Equal(consequenceId, cut.Find(".rf-smoke-confirm__consequence").Id);
        Assert.Equal(consequenceId, cut.Find(".rf-smoke-confirm").GetAttribute("aria-describedby"));
    }

    /// <summary>
    ///     SmokeConfirm rejects blank cancel and confirm labels.
    /// </summary>
    [Fact]
    public void SmokeConfirmRejectsBlankActionLabels()
    {
        // Act
        ArgumentException cancelError = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
                .Add(c => c.Title, "Delete item")
                .Add(c => c.CancelText, " "));
        });
        ArgumentException confirmError = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
                .Add(c => c.Title, "Delete item")
                .Add(c => c.ConfirmText, "\t"));
        });

        // Assert
        Assert.Equal("CancelText", cancelError.ParamName);
        Assert.Equal("ConfirmText", confirmError.ParamName);
    }

    /// <summary>
    ///     SmokeConfirm rejects a blank title because it is the dialog's accessible name.
    /// </summary>
    [Fact]
    public void SmokeConfirmRejectsBlankTitle()
    {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, " "));
        });

        // Assert
        Assert.Equal("Title", error.ParamName);
    }

    /// <summary>
    ///     SmokeConfirm renders action buttons.
    /// </summary>
    [Fact]
    public void SmokeConfirmRendersActionButtons()
    {
        // Act
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, "Delete item"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .AddUnmatched("data-testid", "confirm-1"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Consequence, "This action is irreversible"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.CancelText, "Abort"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.ConfirmText, "Proceed"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.State, RefractionStates.Active));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, "Delete item"));

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
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p.Add(c => c.Title, "Delete item"));

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

    /// <summary>
    ///     SmokeConfirm renders native button semantics and invokes only the selected callback.
    /// </summary>
    [Fact]
    public void SmokeConfirmUsesNativeButtonsAndIndependentCallbacks()
    {
        // Arrange
        List<string> actions = [];
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.OnCancel, () => actions.Add("cancel"))
            .Add(c => c.OnConfirm, () => actions.Add("confirm")));

        // Act
        cut.Find(".rf-smoke-confirm__cancel").Click();
        cut.Find(".rf-smoke-confirm__confirm").Click();

        // Assert
        Assert.Equal(["cancel", "confirm"], actions);
        Assert.Equal("button", cut.Find(".rf-smoke-confirm__cancel").GetAttribute("type"));
        Assert.Equal("button", cut.Find(".rf-smoke-confirm__confirm").GetAttribute("type"));
    }
}