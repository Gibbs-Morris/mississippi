using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Organisms.Confirmations;


namespace Mississippi.Refraction.Client.L0Tests.Components.Organisms.Confirmations;

/// <summary>Verify SmokeConfirm's native action and accessible dialog contracts.</summary>
public sealed class SmokeConfirmTests : BunitContext
{
    /// <summary>Ensure each enabled native action invokes only its parent callback.</summary>
    [Fact]
    public void ActionsAreFormSafeAndIndependent()
    {
        List<string> actions = [];
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.OnCancel, () => actions.Add("cancel"))
            .Add(c => c.OnConfirm, () => actions.Add("confirm")));
        Assert.Equal("button", cut.Find(".rf-smoke-confirm__cancel").GetAttribute("type"));
        Assert.Equal("button", cut.Find(".rf-smoke-confirm__confirm").GetAttribute("type"));
        cut.Find(".rf-smoke-confirm__cancel").Click();
        cut.Find(".rf-smoke-confirm__confirm").Click();
        Assert.Equal(["cancel", "confirm"], actions);
    }

    /// <summary>Ensure unavailable callbacks disable only their respective actions.</summary>
    [Fact]
    public void ActionsTrackCallbackAvailability()
    {
        int count = 0;
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.OnCancel, () => count++));
        Assert.False(cut.Find(".rf-smoke-confirm__cancel").HasAttribute("disabled"));
        Assert.True(cut.Find(".rf-smoke-confirm__confirm").HasAttribute("disabled"));
        cut.Render(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.OnCancel, default(EventCallback))
            .Add(c => c.OnConfirm, () => count++));
        Assert.True(cut.Find(".rf-smoke-confirm__cancel").HasAttribute("disabled"));
        Assert.False(cut.Find(".rf-smoke-confirm__confirm").HasAttribute("disabled"));
        cut.Find(".rf-smoke-confirm__confirm").Click();
        Assert.Equal(1, count);
    }

    /// <summary>Ensure blank accessible names and action labels fail predictably.</summary>
    /// <param name="label">The label to invalidate.</param>
    [Theory]
    [InlineData("title")]
    [InlineData("cancel")]
    [InlineData("confirm")]
    public void BlankLabelsAreRejected(
        string label
    )
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<SmokeConfirm> cut = label switch
            {
                "title" => Render<SmokeConfirm>(p => p.Add(c => c.Title, " ")),
                "cancel" => Render<SmokeConfirm>(p => p.Add(c => c.Title, "Delete item").Add(c => c.CancelText, " ")),
                var _ => Render<SmokeConfirm>(p => p.Add(c => c.Title, "Delete item").Add(c => c.ConfirmText, " ")),
            };
        });
        string expected = label switch
        {
            "title" => "Title",
            "cancel" => "CancelText",
            var _ => "ConfirmText",
        };
        Assert.Equal(expected, error.ParamName);
    }

    /// <summary>Ensure distinct instances retain stable IDs as optional text changes.</summary>
    [Fact]
    public void DescriptionRelationshipsTrackParameterChanges()
    {
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Consequence, "First consequence"));
        using IRenderedComponent<SmokeConfirm> other = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Archive item")
            .Add(c => c.Consequence, "Other consequence"));
        string? titleId = cut.Find(".rf-smoke-confirm__title").Id;
        string? descriptionId = cut.Find(".rf-smoke-confirm__consequence").Id;
        Assert.NotEqual(titleId, other.Find(".rf-smoke-confirm__title").Id);
        Assert.NotEqual(descriptionId, other.Find(".rf-smoke-confirm__consequence").Id);
        cut.Render(p => p.Add(c => c.Title, "Delete item").Add(c => c.Consequence, " "));
        Assert.Null(cut.Find(".rf-smoke-confirm").GetAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll(".rf-smoke-confirm__consequence"));
        cut.Render(p => p.Add(c => c.Title, "Delete item").Add(c => c.Consequence, "Later"));
        Assert.Equal(titleId, cut.Find(".rf-smoke-confirm__title").Id);
        Assert.Equal(descriptionId, cut.Find(".rf-smoke-confirm__consequence").Id);
    }

    /// <summary>Ensure case-distinct safe attributes can be forwarded without a rendering failure.</summary>
    [Fact]
    public void ForwardsCaseDistinctSafeAttributes()
    {
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["data-note"] = "lower",
            ["DATA-NOTE"] = "upper",
            ["data-testid"] = "confirmation",
        };
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.AdditionalAttributes, attributes));
        IElement root = cut.Find(".rf-smoke-confirm");
        Assert.True(root.HasAttribute("data-note"));
        Assert.Equal("confirmation", root.GetAttribute("data-testid"));
        Assert.Equal("dialog", root.GetAttribute("role"));
    }

    /// <summary>Ensure caller attributes cannot replace the component's accessible relationships.</summary>
    [Fact]
    public void OwnedAttributesStayProtectedWhileCallerMetadataComposes()
    {
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["class"] = "caller-class",
            ["role"] = "alert",
            ["data-state"] = "error",
            ["aria-labelledby"] = "wrong-title",
            ["aria-describedby"] = "help help",
            ["data-testid"] = "confirmation",
        };
        using IRenderedComponent<SmokeConfirm> cut = Render<SmokeConfirm>(p => p
            .Add(c => c.Title, "Delete item")
            .Add(c => c.Class, "extra-class")
            .Add(c => c.Consequence, "Cannot undo")
            .Add(c => c.State, RefractionStates.Active)
            .Add(c => c.AdditionalAttributes, attributes));
        IElement root = cut.Find(".rf-smoke-confirm");
        Assert.Contains("caller-class", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("extra-class", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("dialog", root.GetAttribute("role"));
        Assert.Equal("active", root.GetAttribute("data-state"));
        Assert.Equal("confirmation", root.GetAttribute("data-testid"));
        Assert.Equal(cut.Find(".rf-smoke-confirm__title").Id, root.GetAttribute("aria-labelledby"));
        Assert.Equal($"help {cut.Find(".rf-smoke-confirm__consequence").Id}", root.GetAttribute("aria-describedby"));
    }
}