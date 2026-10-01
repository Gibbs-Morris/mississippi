using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Components.Molecules.Notifications;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules.Notifications;

/// <summary>Tests the status and independent action contract.</summary>
public sealed class NotificationPulseBehaviorTests : BunitContext
{
    /// <summary>Each callback controls only its own action.</summary>
    [Fact]
    public void ActionCallbacksRemainIndependent()
    {
        MouseEventArgs? expansion = null;
        int dismissals = 0;
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .Add(c => c.OnExpand, args => expansion = args)
            .Add(c => c.OnDismiss, () => dismissals++));
        MouseEventArgs expected = new()
        {
            ClientX = 42,
        };
        cut.Find(".rf-notification-pulse__expand").Click(expected);
        Assert.NotNull(expansion);
        Assert.Equal(expected.ClientX, expansion.ClientX);
        Assert.Equal(0, dismissals);
        cut.Find(".rf-notification-pulse__dismiss").Click();
        Assert.Equal(1, dismissals);
    }

    /// <summary>Labels are validated when the corresponding callback exists.</summary>
    /// <param name="expand">Whether to validate the expansion action.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionRequiresNonblankVisibleText(
        bool expand
    )
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            using IRenderedComponent<NotificationPulse> cut = expand
                ? Render<NotificationPulse>(p => p.Add(c => c.ExpandText, " ").Add(c => c.OnExpand, _ => { }))
                : Render<NotificationPulse>(p => p.Add(c => c.DismissText, " ").Add(c => c.OnDismiss, () => { }));
        });
        Assert.Contains(expand ? "ExpandText" : "DismissText", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Buttons follow changes in the supplied callbacks.</summary>
    [Fact]
    public void ActionsFollowCallbackAvailability()
    {
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>();
        Assert.Empty(cut.FindAll(".rf-notification-pulse__action"));
        cut.Render(p => p.Add(c => c.OnExpand, _ => { }));
        Assert.Single(cut.FindAll(".rf-notification-pulse__action"));
        Assert.Equal("View details", cut.Find(".rf-notification-pulse__expand").TextContent);
        cut.Render(p => p
            .Add(c => c.OnExpand, _ => { })
            .Add(c => c.OnDismiss, () => { }));
        Assert.Equal(2, cut.FindAll(".rf-notification-pulse__action").Count);
        Assert.Equal("Dismiss notification", cut.Find(".rf-notification-pulse__dismiss").TextContent);
        cut.Render(p => p
            .Add(c => c.OnExpand, default(EventCallback<MouseEventArgs>))
            .Add(c => c.OnDismiss, () => { }));
        Assert.Single(cut.FindAll(".rf-notification-pulse__action"));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__expand"));
        Assert.Equal("Dismiss notification", cut.Find(".rf-notification-pulse__dismiss").TextContent);
        cut.Render(p => p
            .Add(c => c.OnExpand, default(EventCallback<MouseEventArgs>))
            .Add(c => c.OnDismiss, default(EventCallback)));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__action"));
    }

    /// <summary>Actions are native buttons outside the stable status region.</summary>
    [Fact]
    public void ActionsStayOutsideStatusRegion()
    {
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .AddChildContent("Export ready")
            .Add(c => c.OnExpand, _ => { })
            .Add(c => c.OnDismiss, () => { }));
        IElement status = cut.Find(".rf-notification-pulse__status");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Contains("Export ready", status.TextContent, StringComparison.Ordinal);
        Assert.Empty(status.QuerySelectorAll("button"));
        Assert.Equal("true", cut.Find(".rf-notification-pulse__dot").GetAttribute("aria-hidden"));
        Assert.Equal("button", cut.Find(".rf-notification-pulse__expand").GetAttribute("type"));
        Assert.Equal("button", cut.Find(".rf-notification-pulse__dismiss").GetAttribute("type"));
        Assert.False(cut.Find(".rf-notification-pulse").HasAttribute("aria-expanded"));
    }

    /// <summary>Status content updates without changing the live-region contract.</summary>
    [Fact]
    public void StatusContentUpdates()
    {
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p.AddChildContent("Ready"));
        cut.Render(p => p.AddChildContent("Complete"));
        IElement status = cut.Find(".rf-notification-pulse__status");
        Assert.Contains("Complete", status.TextContent, StringComparison.Ordinal);
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Single(cut.FindAll(".rf-notification-pulse__status"));
    }

    /// <summary>Caller attributes cannot replace component-owned class and state.</summary>
    [Fact]
    public void WrapperComposesCallerAttributes()
    {
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["class"] = "host-class",
            ["DATA-STATE"] = "host-state",
            ["data-testid"] = "pulse-1",
        };
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p
            .Add(c => c.Class, "application-class")
            .Add(c => c.State, RefractionStates.Critical)
            .Add(c => c.AdditionalAttributes, attributes));
        IElement root = cut.Find(".rf-notification-pulse");
        Assert.Contains("application-class", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("host-class", root.ClassName, StringComparison.Ordinal);
        Assert.Equal(RefractionStates.Critical, root.GetAttribute("data-state"));
        Assert.Equal("pulse-1", root.GetAttribute("data-testid"));
    }
}