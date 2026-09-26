using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Components.Molecules.Notifications;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules.Notifications;

/// <summary>
///     Verifies status/action separation and controlled notification behavior for <see cref="NotificationPulse" />.
/// </summary>
public sealed class NotificationPulseBehaviorTests : BunitContext
{
    /// <summary>
    ///     NotificationPulse composes caller classes and forwards wrapper attributes without losing state.
    /// </summary>
    [Fact]
    public void NotificationPulseComposesClassesAndForwardsWrapperAttributes()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["class"] = "caller-class",
            ["data-testid"] = "pulse-1",
            ["DATA-STATE"] = RefractionStates.New,
            ["aria-live"] = "polite",
        };
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .Add(c => c.Class, "application-pulse")
            .Add(c => c.State, RefractionStates.Critical)
            .Add(c => c.AdditionalAttributes, attributes));

        // Assert
        IElement root = cut.Find(".rf-notification-pulse");
        Assert.Contains("application-pulse", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("caller-class", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("pulse-1", root.GetAttribute("data-testid"));
        Assert.Equal("polite", root.GetAttribute("aria-live"));
        Assert.Equal(RefractionStates.Critical, root.GetAttribute("data-state"));
        Assert.Equal("status", cut.Find(".rf-notification-pulse__status").GetAttribute("role"));
    }

    /// <summary>
    ///     NotificationPulse sends typed expansion and independent dismissal intents to its parent.
    /// </summary>
    [Fact]
    public void NotificationPulseInvokesIndependentActions()
    {
        // Arrange
        MouseEventArgs? receivedArgs = null;
        int dismissals = 0;
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .AddChildContent("Export ready")
            .Add(c => c.ExpandText, "Open details")
            .Add(c => c.DismissText, "Remove notification")
            .Add(c => c.OnExpand, args => receivedArgs = args)
            .Add(c => c.OnDismiss, () => dismissals++));
        MouseEventArgs expectedArgs = new()
        {
            Button = 1,
            ClientX = 42,
        };

        // Act
        cut.Find(".rf-notification-pulse__expand").Click(expectedArgs);
        cut.Find(".rf-notification-pulse__dismiss").Click();

        // Assert
        Assert.Same(expectedArgs, receivedArgs);
        Assert.Equal(1, dismissals);
        Assert.Equal("Open details", cut.Find(".rf-notification-pulse__expand").TextContent);
        Assert.Equal("Remove notification", cut.Find(".rf-notification-pulse__dismiss").TextContent);
    }

    /// <summary>
    ///     NotificationPulse omits disclosure attributes when controlled state is not supplied.
    /// </summary>
    [Fact]
    public void NotificationPulseOmitsControlledDetailsAttributesByDefault()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p.Add(
            c => c.OnExpand,
            _ => { }));

        // Assert
        IElement expand = cut.Find(".rf-notification-pulse__expand");
        Assert.False(expand.HasAttribute("aria-expanded"));
        Assert.False(expand.HasAttribute("aria-controls"));
    }

    /// <summary>
    ///     NotificationPulse renders and updates optional parent-controlled disclosure attributes.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersControlledDetailsStateAndUpdatesIt()
    {
        // Arrange
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .Add(c => c.IsExpanded, false)
            .Add(c => c.DetailsId, "notification-details")
            .Add(c => c.OnExpand, _ => { }));
        IElement expand = cut.Find(".rf-notification-pulse__expand");

        // Assert
        Assert.Equal("false", expand.GetAttribute("aria-expanded"));
        Assert.Equal("notification-details", expand.GetAttribute("aria-controls"));

        // Act
        cut.Render(p => p.Add(c => c.IsExpanded, true));

        // Assert
        IElement updatedExpand = cut.Find(".rf-notification-pulse__expand");
        Assert.Equal("true", updatedExpand.GetAttribute("aria-expanded"));
        Assert.Equal("notification-details", updatedExpand.GetAttribute("aria-controls"));

        // Act
        cut.Render(p => p.Add(c => c.IsExpanded, null).Add(c => c.DetailsId, null));

        // Assert
        IElement resetExpand = cut.Find(".rf-notification-pulse__expand");
        Assert.False(resetExpand.HasAttribute("aria-expanded"));
        Assert.False(resetExpand.HasAttribute("aria-controls"));
    }

    /// <summary>
    ///     NotificationPulse exposes independent native actions when their callbacks are supplied.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersSeparateStatusAndActions()
    {
        // Arrange
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .AddChildContent("<strong>Export ready</strong>")
            .Add(c => c.OnExpand, _ => { })
            .Add(c => c.OnDismiss, () => { }));

        // Assert
        IElement status = cut.Find(".rf-notification-pulse__status");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Contains("Export ready", status.TextContent, StringComparison.Ordinal);
        Assert.Empty(status.QuerySelectorAll("button"));
        Assert.Equal("View details", cut.Find(".rf-notification-pulse__expand").TextContent);
        Assert.Equal("Dismiss notification", cut.Find(".rf-notification-pulse__dismiss").TextContent);
        Assert.Equal("button", cut.Find(".rf-notification-pulse__expand").GetAttribute("type"));
        Assert.Equal("button", cut.Find(".rf-notification-pulse__dismiss").GetAttribute("type"));
        Assert.False(cut.Find(".rf-notification-pulse").HasAttribute("tabindex"));
    }

    /// <summary>
    ///     NotificationPulse rejects a blank details ID when expansion is available.
    /// </summary>
    [Fact]
    public void NotificationPulseRequiresNonBlankDetailsIdWhenExpansionCallbackSupplied()
    {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
                .Add(c => c.DetailsId, " ")
                .Add(c => c.OnExpand, _ => { }));
        });

        // Assert
        Assert.Contains("DetailsId", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     NotificationPulse rejects blank dismissal text when its dismissal callback is supplied.
    /// </summary>
    [Fact]
    public void NotificationPulseRequiresNonBlankDismissTextWhenCallbackSupplied()
    {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
                .Add(c => c.DismissText, " ")
                .Add(c => c.OnDismiss, () => { }));
        });

        // Assert
        Assert.Contains("DismissText", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     NotificationPulse rejects blank expansion text when its expansion callback is supplied.
    /// </summary>
    [Fact]
    public void NotificationPulseRequiresNonBlankExpandTextWhenCallbackSupplied()
    {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
                .Add(c => c.ExpandText, " ")
                .Add(c => c.OnExpand, _ => { }));
        });

        // Assert
        Assert.Contains("ExpandText", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     NotificationPulse keeps its owned class and state when caller class values are blank or null.
    /// </summary>
    [Fact]
    public void NotificationPulseRetainsOwnedClassAndStateForEmptyCallerClasses()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["class"] = null!,
            ["DATA-STATE"] = "caller-state",
        };
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .Add(c => c.Class, " ")
            .Add(c => c.State, RefractionStates.Critical)
            .Add(c => c.AdditionalAttributes, attributes));

        // Assert
        IElement root = cut.Find(".rf-notification-pulse");
        Assert.Equal("rf-notification-pulse", root.ClassName);
        Assert.Equal(RefractionStates.Critical, root.GetAttribute("data-state"));
    }

    /// <summary>
    ///     NotificationPulse updates rendered actions when callback availability changes.
    /// </summary>
    [Fact]
    public void NotificationPulseUpdatesActionsWithCallbackAvailability()
    {
        // Arrange
        using IRenderedComponent<NotificationPulse> cut =
            Render<NotificationPulse>(p => p.AddChildContent("Export ready"));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__action"));

        // Act
        cut.Render(p => p.Add(c => c.OnExpand, _ => { }));

        // Assert
        Assert.Single(cut.FindAll(".rf-notification-pulse__action"));
        Assert.NotEmpty(cut.FindAll(".rf-notification-pulse__expand"));

        // Act
        cut.Render(p => p.Add(c => c.OnDismiss, () => { }));

        // Assert
        Assert.Equal(2, cut.FindAll(".rf-notification-pulse__action").Count);

        // Act
        cut.Render(p => p.Add(c => c.OnExpand, default(EventCallback<MouseEventArgs>)));

        // Assert
        Assert.Single(cut.FindAll(".rf-notification-pulse__action"));
        Assert.NotEmpty(cut.FindAll(".rf-notification-pulse__dismiss"));
    }

    /// <summary>
    ///     NotificationPulse retains its status region while parent content changes.
    /// </summary>
    [Fact]
    public void NotificationPulseUpdatesStatusContentInPlace()
    {
        // Arrange
        using IRenderedComponent<NotificationPulse> cut =
            Render<NotificationPulse>(p => p.AddChildContent("Export ready"));
        Assert.Contains(
            "Export ready",
            cut.Find(".rf-notification-pulse__status").TextContent,
            StringComparison.Ordinal);

        // Act
        cut.Render(p => p.AddChildContent("Export complete"));

        // Assert
        IElement status = cut.Find(".rf-notification-pulse__status");
        Assert.Contains("Export complete", status.TextContent, StringComparison.Ordinal);
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Single(cut.FindAll(".rf-notification-pulse__status"));
    }
}