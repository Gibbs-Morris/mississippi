using System;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;

using MississippiSamples.LightSpeed.Client.Components.Organisms.Notifications;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Organisms.Notifications;

/// <summary>Verifies the controlled notification workflow and its UI-only focus requests.</summary>
public sealed class NotificationDemoTests : BunitContext
{
    /// <summary>Expand and dismiss emit typed parent intents without changing controlled parameters.</summary>
    [Fact]
    public void ActionsEmitParentOwnedCallbacks()
    {
        MouseEventArgs? receivedArgs = null;
        int dismissals = 0;
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, args => receivedArgs = args)
            .Add(c => c.DismissRequested, () => dismissals++));
        MouseEventArgs expectedArgs = new()
        {
            Button = 1,
            ClientX = 24,
        };
        cut.Find(".rf-notification-pulse__expand").Click(expectedArgs);
        cut.Find(".rf-notification-pulse__dismiss").Click();
        Assert.Same(expectedArgs, receivedArgs);
        Assert.Equal(1, dismissals);
        Assert.Empty(JSInterop.Invocations);
        Assert.True(cut.Instance.IsVisible);
        Assert.False(cut.Instance.IsExpanded);
    }

    /// <summary>Visible collapsed state keeps the controlled details target available to the action.</summary>
    [Fact]
    public void CollapsedStateKeepsControlledDetailsTargetMounted()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false));
        IElement details = cut.Find("[data-testid=notification-details]");
        IElement expand = cut.Find(".rf-notification-pulse__expand");
        Assert.Equal("notification-details", details.GetAttribute("id"));
        Assert.True(details.HasAttribute("hidden"));
        Assert.Equal("false", expand.GetAttribute("aria-expanded"));
        Assert.Equal("notification-details", expand.GetAttribute("aria-controls"));
    }

    /// <summary>The message stays in the status region while details render as a separate region.</summary>
    [Fact]
    public void DetailsRenderOutsideLiveStatusContent()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, true));
        IElement status = cut.Find("[data-testid=notification-pulse] .rf-notification-pulse__status");
        IElement details = cut.Find("[data-testid=notification-details]");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Contains("The sample export is ready to review", status.TextContent, StringComparison.Ordinal);
        Assert.Empty(status.QuerySelectorAll("[data-testid=notification-details]"));
        Assert.Equal("region", details.GetAttribute("role"));
        Assert.Equal("notification-details", details.GetAttribute("id"));
        Assert.False(details.HasAttribute("hidden"));
        Assert.Equal("-1", details.GetAttribute("tabindex"));
        Assert.Contains("Sample export details", details.TextContent, StringComparison.Ordinal);
    }

    /// <summary>Initial rendering does not request focus before a parent state transition occurs.</summary>
    [Fact]
    public void InitialRenderDoesNotStealFocus()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>();
        Assert.Empty(JSInterop.Invocations);
        Assert.Equal("-1", cut.Find("[data-testid=notification-demo-heading]").GetAttribute("tabindex"));
    }

    /// <summary>The restore action is present only for the parent-supplied hidden state.</summary>
    [Fact]
    public void RestoreActionFollowsVisibilityParameter()
    {
        int restores = 0;
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, false)
            .Add(c => c.IsExpanded, true)
            .Add(c => c.RestoreRequested, () => restores++));
        Assert.Empty(cut.FindAll("[data-testid=notification-pulse]"));
        Assert.Empty(cut.FindAll("[data-testid=notification-details]"));
        cut.Find("[data-testid=notification-restore]").Click();
        Assert.Equal(1, restores);
        Assert.False(cut.Instance.IsVisible);
        cut.Render(p => p.Add(c => c.IsVisible, true).Add(c => c.IsExpanded, false));
        Assert.Empty(cut.FindAll("[data-testid=notification-restore]"));
        Assert.Single(cut.FindAll("[data-testid=notification-pulse]"));
    }
}