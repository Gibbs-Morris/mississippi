using System;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Components.Molecules.Notifications;

using MississippiSamples.LightSpeed.Client.Components.Organisms.Notifications;

using TestContext = Xunit.TestContext;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Organisms.Notifications;

/// <summary>Verifies the controlled notification workflow and its UI-only focus requests.</summary>
public sealed class NotificationDemoTests : BunitContext
{
    private static Task<InvalidOperationException> StartExpectedFailureAsync(
        IRenderedComponent<NotificationDemo> cut,
        IRenderedComponent<NotificationPulse> pulse
    ) =>
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.InvokeAsync(() => pulse.Instance.OnExpand.InvokeAsync(new())));

    /// <summary>Pulse actions follow the individually supplied callbacks as they change.</summary>
    [Fact]
    public void ActionAvailabilityUpdatesWhenCallbacksChange()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.ExpandRequested, _ => { }));
        Assert.Single(cut.FindAll(".rf-notification-pulse__expand"));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__dismiss"));
        cut.Render(p => p.Add(c => c.DismissRequested, () => { }));
        Assert.Single(cut.FindAll(".rf-notification-pulse__expand"));
        Assert.Single(cut.FindAll(".rf-notification-pulse__dismiss"));
        cut.Render(p => p.Add(c => c.ExpandRequested, default(EventCallback<MouseEventArgs>)));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__expand"));
        Assert.Single(cut.FindAll(".rf-notification-pulse__dismiss"));
        cut.Render(p => p.Add(c => c.DismissRequested, default(EventCallback)));
        Assert.Empty(cut.FindAll(".rf-notification-pulse__action"));
    }

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

    /// <summary>Dismissal keeps its request while hidden state is still old, then focuses the restore action.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AsyncDismissalKeepsPendingFocusAcrossOldStateAndFocusesRestore()
    {
        using SemaphoreSlim callbackStarted = new(0, 1);
        using SemaphoreSlim callbackCompleted = new(0, 1);
        Func<Task> callback = async () =>
        {
            callbackStarted.Release();
            await callbackCompleted.WaitAsync(TestContext.Current.CancellationToken);
        };
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.DismissRequested, callback)
            .Add(c => c.RestoreRequested, () => { }));
        Task click = cut.Find(".rf-notification-pulse__dismiss").ClickAsync();
        await callbackStarted.WaitAsync(TestContext.Current.CancellationToken);
        cut.Render(p => p.Add(c => c.IsVisible, true));
        Assert.Empty(JSInterop.Invocations);
        cut.Render(p => p.Add(c => c.IsVisible, false));
        IElement restore = cut.Find("[data-testid=notification-restore]");
        string? restoreReference = restore.GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(restoreReference), restore.OuterHtml);
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(restoreReference, focused.Id);
        callbackCompleted.Release();
        await click;
        Assert.Single(JSInterop.Invocations);
    }

    /// <summary>Expansion keeps its focus request through an old-state render and focuses accepted state.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AsyncExpansionKeepsPendingFocusAcrossOldStateAndFocusesAcceptedState()
    {
        using SemaphoreSlim callbackStarted = new(0, 1);
        using SemaphoreSlim callbackCompleted = new(0, 1);
        Func<MouseEventArgs, Task> callback = async _ =>
        {
            callbackStarted.Release();
            await callbackCompleted.WaitAsync(TestContext.Current.CancellationToken);
        };
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, callback));
        string? detailsReference = cut.Find("[data-testid=notification-details]")
            .GetAttribute("blazor:elementReference");
        Assert.False(
            string.IsNullOrWhiteSpace(detailsReference),
            cut.Find("[data-testid=notification-details]").OuterHtml);
        Task click = cut.Find(".rf-notification-pulse__expand").ClickAsync();
        await callbackStarted.WaitAsync(TestContext.Current.CancellationToken);
        cut.Render(p => p.Add(c => c.IsExpanded, false));
        Assert.Empty(JSInterop.Invocations);
        cut.Render(p => p.Add(c => c.IsExpanded, true));
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(detailsReference, focused.Id);
        callbackCompleted.Release();
        await click;
        Assert.Single(JSInterop.Invocations);
    }

    /// <summary>Restoration keeps its request through an old-state render, then focuses the stable heading.</summary>
    /// <param name="isExpanded">Whether the accepted visible render keeps details expanded.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncRestorationKeepsPendingFocusAcrossOldStateAndFocusesHeading(
        bool isExpanded
    )
    {
        using SemaphoreSlim callbackStarted = new(0, 1);
        using SemaphoreSlim callbackCompleted = new(0, 1);
        IRenderedComponent<NotificationDemo> cut = null!;
        Func<Task> callback = async () =>
        {
            callbackStarted.Release();
            await callbackCompleted.WaitAsync(TestContext.Current.CancellationToken);
            cut.Render(parameters => parameters.Add(c => c.IsVisible, true).Add(c => c.IsExpanded, isExpanded));
        };
        using (cut = Render<NotificationDemo>(p => p
                   .Add(c => c.IsVisible, false)
                   .Add(c => c.RestoreRequested, callback)))
        {
            string? headingReference = cut.Find("[data-testid=notification-demo-heading]")
                .GetAttribute("blazor:elementReference");
            Assert.False(
                string.IsNullOrWhiteSpace(headingReference),
                cut.Find("[data-testid=notification-demo-heading]").OuterHtml);
            Task click = cut.Find("[data-testid=notification-restore]").ClickAsync();
            await callbackStarted.WaitAsync(TestContext.Current.CancellationToken);
            cut.Render(p => p.Add(c => c.IsVisible, false));
            Assert.Empty(JSInterop.Invocations);
            callbackCompleted.Release();
            await click;
            ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
            Assert.Equal(headingReference, focused.Id);
            Assert.Single(JSInterop.Invocations);
        }
    }

    /// <summary>Visible collapsed state keeps the controlled details target available to the action.</summary>
    [Fact]
    public void CollapsedStateKeepsControlledDetailsTargetMounted()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, _ => { }));
        IElement details = cut.Find("[data-testid=notification-details]");
        IElement expand = cut.Find(".rf-notification-pulse__expand");
        string? detailsId = details.GetAttribute("id");
        Assert.False(string.IsNullOrWhiteSpace(detailsId), details.OuterHtml);
        Assert.True(details.HasAttribute("hidden"));
        Assert.Equal("false", expand.GetAttribute("aria-expanded"));
        Assert.Equal(detailsId, expand.GetAttribute("aria-controls"));
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
        Assert.False(details.HasAttribute("hidden"));
        Assert.Equal("-1", details.GetAttribute("tabindex"));
        Assert.Equal(
            cut.Find("[data-testid=notification-demo-heading]").Id,
            cut.Find("[data-testid=notification-demo]").GetAttribute("aria-labelledby"));
        Assert.Equal(cut.Find("h3").Id, details.GetAttribute("aria-labelledby"));
        Assert.Contains("Sample export details", details.TextContent, StringComparison.Ordinal);
    }

    /// <summary>A rejected expansion cannot replace dismissal focus for either restore target.</summary>
    /// <param name="hasRestoreCallback">Whether the hidden state supplies a restore action.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DismissalFocusSurvivesRejectedExpansion(
        bool hasRestoreCallback
    )
    {
        using SemaphoreSlim dismissStarted = new(0, 1);
        using SemaphoreSlim dismissCompleted = new(0, 1);
        using SemaphoreSlim expandStarted = new(0, 1);
        using SemaphoreSlim expandCompleted = new(0, 1);
        IRenderedComponent<NotificationDemo> cut = null!;
        Func<Task> dismissCallback = async () =>
        {
            dismissStarted.Release();
            await dismissCompleted.WaitAsync(TestContext.Current.CancellationToken);
            cut.Render(parameters => parameters.Add(c => c.IsVisible, false));
        };
        Func<MouseEventArgs, Task> expandCallback = async _ =>
        {
            expandStarted.Release();
            await expandCompleted.WaitAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("Expansion was rejected.");
        };
        using (cut = hasRestoreCallback
                   ? Render<NotificationDemo>(p => p
                       .Add(c => c.IsVisible, true)
                       .Add(c => c.IsExpanded, false)
                       .Add(c => c.DismissRequested, dismissCallback)
                       .Add(c => c.ExpandRequested, expandCallback)
                       .Add(c => c.RestoreRequested, () => { }))
                   : Render<NotificationDemo>(p => p
                       .Add(c => c.IsVisible, true)
                       .Add(c => c.IsExpanded, false)
                       .Add(c => c.DismissRequested, dismissCallback)
                       .Add(c => c.ExpandRequested, expandCallback)))
        {
            string? headingReference = cut.Find("[data-testid=notification-demo-heading]")
                .GetAttribute("blazor:elementReference");
            Assert.False(
                string.IsNullOrWhiteSpace(headingReference),
                cut.Find("[data-testid=notification-demo-heading]").OuterHtml);
            IRenderedComponent<NotificationPulse> pulse = cut.FindComponent<NotificationPulse>();
            Task dismissClick = cut.Find(".rf-notification-pulse__dismiss").ClickAsync();
            await dismissStarted.WaitAsync(TestContext.Current.CancellationToken);
            Task<InvalidOperationException> expandClick = StartExpectedFailureAsync(cut, pulse);
            await expandStarted.WaitAsync(TestContext.Current.CancellationToken);
            dismissCompleted.Release();
            await dismissClick;
            Assert.Empty(cut.FindAll(".rf-notification-pulse__expand"));
            string expectedFocusReference;
            if (hasRestoreCallback)
            {
                IElement restore = cut.Find("[data-testid=notification-restore]");
                expectedFocusReference = restore.GetAttribute("blazor:elementReference")!;
                Assert.False(string.IsNullOrWhiteSpace(expectedFocusReference), restore.OuterHtml);
            }
            else
            {
                expectedFocusReference = headingReference;
            }

            ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
            Assert.Equal(expectedFocusReference, focused.Id);
            expandCompleted.Release();
            await expandClick;
            Assert.Single(JSInterop.Invocations);
        }
    }

    /// <summary>Dismissal without a restore callback falls back to the stable heading target.</summary>
    [Fact]
    public void DismissalWithoutRestoreCallbackFocusesHeadingAfterParentHidesNotification()
    {
        int dismissals = 0;
        IRenderedComponent<NotificationDemo> cut = null!;
        using (cut = Render<NotificationDemo>(p => p.Add(c => c.IsVisible, true)
                   .Add(
                       c => c.DismissRequested,
                       () =>
                       {
                           dismissals++;
                           cut.Render(parameters => parameters.Add(c => c.IsVisible, false));
                       })))
        {
            string? headingReference = cut.Find("[data-testid=notification-demo-heading]")
                .GetAttribute("blazor:elementReference");
            Assert.False(
                string.IsNullOrWhiteSpace(headingReference),
                cut.Find("[data-testid=notification-demo-heading]").OuterHtml);
            cut.Find(".rf-notification-pulse__dismiss").Click();
            Assert.Equal(1, dismissals);
            Assert.Empty(cut.FindAll("[data-testid=notification-restore]"));
            Assert.Contains(
                "The parent can show it again when ready.",
                cut.Find("[data-testid=notification-hidden]").TextContent,
                StringComparison.Ordinal);
            ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
            Assert.Equal(headingReference, focused.Id);
        }
    }

    /// <summary>A failed async callback clears its request before a later unrelated render.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FailedAsyncCallbackClearsPendingFocus()
    {
        using SemaphoreSlim callbackStarted = new(0, 1);
        using SemaphoreSlim callbackFailed = new(0, 1);
        Func<MouseEventArgs, Task> callback = async _ =>
        {
            callbackStarted.Release();
            await callbackFailed.WaitAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("Expansion failed.");
        };
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, callback));
        IRenderedComponent<NotificationPulse> pulse = cut.FindComponent<NotificationPulse>();
        Task<InvalidOperationException> invocation = StartExpectedFailureAsync(cut, pulse);
        await callbackStarted.WaitAsync(TestContext.Current.CancellationToken);
        cut.Render(p => p.Add(c => c.IsExpanded, false));
        callbackFailed.Release();
        await invocation;
        Assert.Empty(JSInterop.Invocations);
        cut.Render(p => p.Add(c => c.IsExpanded, true));
        Assert.Empty(JSInterop.Invocations);
    }

    /// <summary>An ignored async callback clears its request before a later accepted render.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task IgnoredAsyncCallbackClearsPendingFocus()
    {
        using SemaphoreSlim callbackStarted = new(0, 1);
        using SemaphoreSlim callbackCompleted = new(0, 1);
        Func<MouseEventArgs, Task> callback = async _ =>
        {
            callbackStarted.Release();
            await callbackCompleted.WaitAsync(TestContext.Current.CancellationToken);
        };
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, callback));
        Task click = cut.Find(".rf-notification-pulse__expand").ClickAsync();
        await callbackStarted.WaitAsync(TestContext.Current.CancellationToken);
        cut.Render(p => p.Add(c => c.IsExpanded, false));
        callbackCompleted.Release();
        await click;
        Assert.Empty(JSInterop.Invocations);
        cut.Render(p => p.Add(c => c.IsExpanded, true));
        Assert.Empty(JSInterop.Invocations);
    }

    /// <summary>Initial rendering does not request focus before a parent state transition occurs.</summary>
    [Fact]
    public void InitialRenderDoesNotStealFocus()
    {
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>();
        Assert.Empty(JSInterop.Invocations);
        Assert.Equal("-1", cut.Find("[data-testid=notification-demo-heading]").GetAttribute("tabindex"));
        Assert.Empty(cut.FindAll("[data-testid=notification-pulse]"));
        Assert.Empty(cut.FindAll("[data-testid=notification-restore]"));
        Assert.Contains(
            "The parent can show it again when ready.",
            cut.Find("[data-testid=notification-hidden]").TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>Each demo instance owns distinct stable IDs for its relationships.</summary>
    [Fact]
    public void InstancesUseDistinctStableLabelRelationships()
    {
        using IRenderedComponent<NotificationDemo> first = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, _ => { }));
        using IRenderedComponent<NotificationDemo> second = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, _ => { }));
        IElement firstDemo = first.Find("[data-testid=notification-demo]");
        IElement firstHeading = first.Find("[data-testid=notification-demo-heading]");
        IElement firstDetails = first.Find("[data-testid=notification-details]");
        IElement firstDetailsHeading = first.Find("h3");
        IElement firstExpand = first.Find(".rf-notification-pulse__expand");
        IElement secondDemo = second.Find("[data-testid=notification-demo]");
        IElement secondHeading = second.Find("[data-testid=notification-demo-heading]");
        IElement secondDetails = second.Find("[data-testid=notification-details]");
        IElement secondDetailsHeading = second.Find("h3");
        IElement secondExpand = second.Find(".rf-notification-pulse__expand");
        string firstHeadingId = firstHeading.Id!;
        string firstDetailsId = firstDetails.Id!;
        string firstDetailsHeadingId = firstDetailsHeading.Id!;
        string secondHeadingId = secondHeading.Id!;
        string secondDetailsId = secondDetails.Id!;
        string secondDetailsHeadingId = secondDetailsHeading.Id!;
        Assert.NotEqual(firstHeadingId, secondHeadingId);
        Assert.NotEqual(firstDetailsId, secondDetailsId);
        Assert.NotEqual(firstDetailsHeadingId, secondDetailsHeadingId);
        Assert.Equal(firstHeadingId, firstDemo.GetAttribute("aria-labelledby"));
        Assert.Equal(firstDetailsId, firstExpand.GetAttribute("aria-controls"));
        Assert.Equal(firstDetailsHeadingId, firstDetails.GetAttribute("aria-labelledby"));
        Assert.Equal(secondHeadingId, secondDemo.GetAttribute("aria-labelledby"));
        Assert.Equal(secondDetailsId, secondExpand.GetAttribute("aria-controls"));
        Assert.Equal(secondDetailsHeadingId, secondDetails.GetAttribute("aria-labelledby"));
        first.Render(p => p.Add(c => c.IsExpanded, true));
        second.Render(p => p.Add(c => c.IsExpanded, true));
        Assert.Equal(firstHeadingId, first.Find("[data-testid=notification-demo-heading]").Id);
        Assert.Equal(firstDetailsId, first.Find("[data-testid=notification-details]").Id);
        Assert.Equal(firstDetailsHeadingId, first.Find("h3").Id);
        Assert.Equal(secondHeadingId, second.Find("[data-testid=notification-demo-heading]").Id);
        Assert.Equal(secondDetailsId, second.Find("[data-testid=notification-details]").Id);
        Assert.Equal(secondDetailsHeadingId, second.Find("h3").Id);
    }

    /// <summary>An older async completion cannot clear a newer focus request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task OlderAsyncCompletionDoesNotClearNewerFocusRequest()
    {
        using SemaphoreSlim firstStarted = new(0, 1);
        using SemaphoreSlim firstCompleted = new(0, 1);
        using SemaphoreSlim secondStarted = new(0, 1);
        using SemaphoreSlim secondCompleted = new(0, 1);
        Func<MouseEventArgs, Task> firstCallback = async _ =>
        {
            firstStarted.Release();
            await firstCompleted.WaitAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("First expansion failed.");
        };
        Func<Task> secondCallback = async () =>
        {
            secondStarted.Release();
            await secondCompleted.WaitAsync(TestContext.Current.CancellationToken);
        };
        using IRenderedComponent<NotificationDemo> cut = Render<NotificationDemo>(p => p
            .Add(c => c.IsVisible, true)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ExpandRequested, firstCallback));
        string? headingReference = cut.Find("[data-testid=notification-demo-heading]")
            .GetAttribute("blazor:elementReference");
        Assert.False(
            string.IsNullOrWhiteSpace(headingReference),
            cut.Find("[data-testid=notification-demo-heading]").OuterHtml);
        IRenderedComponent<NotificationPulse> pulse = cut.FindComponent<NotificationPulse>();
        Task<InvalidOperationException> firstClick = StartExpectedFailureAsync(cut, pulse);
        await firstStarted.WaitAsync(TestContext.Current.CancellationToken);
        cut.Render(p => p.Add(c => c.IsVisible, false).Add(c => c.RestoreRequested, secondCallback));
        Task secondClick = cut.Find("[data-testid=notification-restore]").ClickAsync();
        await secondStarted.WaitAsync(TestContext.Current.CancellationToken);
        firstCompleted.Release();
        await firstClick;
        Assert.Empty(JSInterop.Invocations);
        cut.Render(p => p.Add(c => c.IsVisible, true).Add(c => c.IsExpanded, false));
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(headingReference, focused.Id);
        secondCompleted.Release();
        await secondClick;
        Assert.Single(JSInterop.Invocations);
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