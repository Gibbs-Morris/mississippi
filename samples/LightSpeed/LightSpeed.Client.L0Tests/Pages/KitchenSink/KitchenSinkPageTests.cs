using System;
using System.Collections.Generic;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Components.Organisms.Notifications;
using MississippiSamples.LightSpeed.Client.Features.Showcase;
using MississippiSamples.LightSpeed.Client.Pages.KitchenSink;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Pages.KitchenSink;

/// <summary>Verifies the page connects rendered callbacks to the real Reservoir store.</summary>
public sealed class KitchenSinkPageTests : BunitContext
{
    /// <summary>Emitter activation and disabled intent use the registered page store and survive reset.</summary>
    [Fact]
    public void EmitterActionsUpdateSelectedStateAndSurviveReset()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.Find("button.rf-emitter").Click();
        Assert.Equal("1", cut.Find("[data-testid=state-emitter-count]").TextContent);
        Assert.Equal(nameof(ActivateEmitterAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("input[name=emitter-disabled]").Change(true);
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("[data-testid=state-emitter-disabled]").TextContent);
        Assert.Equal(nameof(ChangeEmitterDisabledAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("form button[type=button]").Click();
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("disabled"));
        Assert.Equal("1", cut.Find("[data-testid=state-emitter-count]").TextContent);
        Assert.Equal(nameof(ResetProfileAction), cut.Find("[data-testid=last-action]").TextContent);
    }

    /// <summary>The rendered form validates and resets through selected store state.</summary>
    [Fact]
    public void FormActionsUpdateSelectedState()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.Find("input[name=work-email]").Input(string.Empty);
        Assert.Empty(cut.FindAll("[role=alert]"));
        cut.Find("form").Submit();
        Assert.Equal("ValidateProfileAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("true", cut.Find("input[name=work-email]").GetAttribute("aria-invalid"));
        cut.FindAll(".theme-options button").Single(button => button.TextContent.Trim() == "Light").Click();
        cut.Find("form button[type=button]").Click();
        Assert.Equal("alex@contoso.example", cut.Find("input[name=work-email]").GetAttribute("value"));
        Assert.Equal("4", cut.Find("[data-testid=action-count]").TextContent);
        Assert.Equal("ResetProfileAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    /// <summary>Notification actions update the selected flow and move focus to the next stable target.</summary>
    [Fact]
    public void NotificationActionsUpdateStateAndFocusStableTargets()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        IRenderedComponent<NotificationDemo> notification = cut.FindComponent<NotificationDemo>();
        IElement initialDetails = notification.Find("[data-testid=notification-details]");
        string detailsId = initialDetails.Id!;
        string? detailsReference = initialDetails.GetAttribute("blazor:elementReference");
        IElement initialHeading = notification.Find("[data-testid=notification-demo-heading]");
        string headingId = initialHeading.Id!;
        string? headingReference = initialHeading.GetAttribute("blazor:elementReference");

        // bUnit may omit retained element-reference markers after a rerender, so capture them when nodes are created.
        string? restoreReference = null;

        void CaptureRestoreReference(
            object? sender,
            EventArgs eventArgs
        )
        {
            IReadOnlyList<IElement> restoreTargets = notification.FindAll("[data-testid=notification-restore]");
            IElement? restoreTarget = restoreTargets.Count > 0 ? restoreTargets[0] : null;
            string? candidate = restoreTarget?.GetAttribute("blazor:elementReference");
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                restoreReference = candidate;
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(detailsReference), initialDetails.OuterHtml);
        Assert.False(string.IsNullOrWhiteSpace(headingReference), initialHeading.OuterHtml);
        Assert.Equal(detailsId, notification.Find(".rf-notification-pulse__expand").GetAttribute("aria-controls"));
        Assert.Equal(headingId, notification.Find("[data-testid=notification-demo]").GetAttribute("aria-labelledby"));
        notification.OnMarkupUpdated += CaptureRestoreReference;
        Assert.Equal("true", cut.Find("[data-testid=state-notification-visible]").TextContent);
        Assert.Equal("false", cut.Find("[data-testid=state-notification-expanded]").TextContent);
        Assert.Empty(JSInterop.Invocations);
        cut.Find("[data-testid=notification-pulse] .rf-notification-pulse__expand").Click();
        IElement details = notification.Find("[data-testid=notification-details]");
        Assert.Equal(detailsId, details.Id);
        ElementReference detailsFocus =
            Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(detailsReference, detailsFocus.Id);
        Assert.Equal("true", cut.Find("[data-testid=state-notification-expanded]").TextContent);
        Assert.Equal(nameof(ExpandNotificationAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("[data-testid=notification-pulse] .rf-notification-pulse__expand").Click();
        IElement repeatedDetails = notification.Find("[data-testid=notification-details]");
        Assert.Equal(detailsId, repeatedDetails.Id);
        ElementReference repeatedDetailsFocus =
            Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke(2)[1].Arguments[0]);
        Assert.Equal(detailsReference, repeatedDetailsFocus.Id);
        cut.Find("[data-testid=notification-pulse] .rf-notification-pulse__dismiss").Click();
        IElement restore = notification.Find("[data-testid=notification-restore]");
        ElementReference restoreFocus =
            Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke(3)[2].Arguments[0]);
        Assert.False(string.IsNullOrWhiteSpace(restoreReference), restore.OuterHtml);
        Assert.Equal(restoreReference, restoreFocus.Id);
        Assert.Equal("false", cut.Find("[data-testid=state-notification-visible]").TextContent);
        Assert.Equal("false", cut.Find("[data-testid=state-notification-expanded]").TextContent);
        Assert.Equal(nameof(DismissNotificationAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("[data-testid=notification-restore]").Click();
        IElement heading = notification.Find("[data-testid=notification-demo-heading]");
        Assert.Equal(headingId, heading.Id);
        ElementReference headingFocus =
            Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke(4)[3].Arguments[0]);
        Assert.Equal(headingReference, headingFocus.Id);
        Assert.Equal("true", cut.Find("[data-testid=state-notification-visible]").TextContent);
        Assert.Equal("false", cut.Find("[data-testid=state-notification-expanded]").TextContent);
        Assert.Equal(nameof(RestoreNotificationAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("[data-testid=notification-pulse] .rf-notification-pulse__expand").Click();
        cut.Find("form button[type=button]").Click();
        Assert.Equal("true", cut.Find("[data-testid=state-notification-visible]").TextContent);
        Assert.Equal("true", cut.Find("[data-testid=state-notification-expanded]").TextContent);
        notification.OnMarkupUpdated -= CaptureRestoreReference;
    }

    /// <summary>Progress choices dispatch through Reservoir and survive form resets.</summary>
    [Fact]
    public void ProgressChoicesUpdateSelectedState()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.FindAll(".progress-options button").Single(button => button.TextContent.Trim() == "75%").Click();
        Assert.Equal("75", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Equal("ChangeProgressAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("75%", cut.Find("[data-testid=state-progress]").TextContent);
        cut.Find("form button[type=button]").Click();
        Assert.Equal("75", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        cut.FindAll(".progress-options button")
            .Single(button => button.TextContent.Trim() == "Unknown duration")
            .Click();
        Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
        Assert.Equal("Unknown", cut.Find("[data-testid=state-progress]").TextContent);
        Assert.Equal("3", cut.Find("[data-testid=action-count]").TextContent);
    }
}