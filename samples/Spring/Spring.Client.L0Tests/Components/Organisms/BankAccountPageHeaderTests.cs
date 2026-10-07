using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="BankAccountPageHeader" />.
/// </summary>
public sealed class BankAccountPageHeaderTests : BunitContext
{
    /// <summary>Closed details do not expose a dangling ARIA reference.</summary>
    [Fact]
    public void ClosedConnectionDetailsHaveNoControlsReference()
    {
        using IRenderedComponent<BankAccountPageHeader> cut = Render<BankAccountPageHeader>(parameters =>
            parameters.Add(component => component.ConnectionDetailsId, "closed-details-target"));
        Assert.Equal("false", cut.Find("header button").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("header button").HasAttribute("aria-controls"));
    }

    /// <summary>
    ///     Closing details returns focus to its trigger once; opening and stable renders do not steal focus.
    /// </summary>
    /// <param name="initiallyOpen">Whether details are already open at the initial render.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingDetailsRestoresTriggerFocusWithoutStealingItOnOtherRenders(
        bool initiallyOpen
    )
    {
        using IRenderedComponent<BankAccountPageHeader> cut = Render<BankAccountPageHeader>(parameters =>
            parameters.Add(component => component.ConnectionDetailsId, "focus-details-target")
                .Add(component => component.IsConnectionDetailsOpen, initiallyOpen));
        Assert.Empty(JSInterop.Invocations);
        string? triggerReference = cut.Find("header button").GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(triggerReference), cut.Find("header button").OuterHtml);
        cut.Render(parameters => parameters.Add(component => component.IsConnectionDetailsOpen, true));
        Assert.Empty(JSInterop.Invocations);
        cut.Render(parameters => parameters.Add(component => component.IsConnectionDetailsOpen, false));
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(triggerReference, focused.Id);
        Assert.Equal("false", cut.Find("header button").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("header button").HasAttribute("aria-controls"));
        cut.Render(parameters => parameters.Add(component => component.IsConnectionDetailsOpen, false));
        cut.Render(parameters => parameters.Add(component => component.ConnectionStatusText, "Connected"));
        cut.Render(parameters => parameters.Add(component => component.IsConnectionDetailsOpen, true));
        Assert.Equal(
            triggerReference,
            Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]).Id);
    }

    /// <summary>
    ///     Header renders the connection status text and invokes callbacks.
    /// </summary>
    [Fact]
    public void HeaderRendersStatusAndInvokesCallbacks()
    {
        bool navigated = false;
        bool toggled = false;
        using IRenderedComponent<BankAccountPageHeader> cut = Render<BankAccountPageHeader>(p => p
            .Add(c => c.ConnectionStatusText, "Connected")
            .Add(c => c.ConnectionDetailsId, "header-details-target")
            .Add(c => c.IsConnectionDetailsOpen, true)
            .Add(c => c.OnNavigateInvestigations, EventCallback.Factory.Create(this, () => navigated = true))
            .Add(c => c.OnToggleConnectionDetails, EventCallback.Factory.Create(this, () => toggled = true)));
        cut.Find("header button").Click();
        cut.Find("nav button").Click();
        Assert.True(toggled);
        Assert.True(navigated);
        Assert.Equal("Connection status: Connected", cut.Find("header button").GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("header button").GetAttribute("aria-expanded"));
        Assert.Equal("header-details-target", cut.Find("header button").GetAttribute("aria-controls"));
        Assert.Equal("Account actions", cut.Find("nav").GetAttribute("aria-label"));
        cut.Render(p => p.Add(c => c.ConnectionStatusText, "Reconnecting"));
        Assert.Equal("Connection status: Reconnecting", cut.Find("header button").GetAttribute("aria-label"));
    }

    /// <summary>Verify that a header without a paired target exposes no dangling controls reference.</summary>
    /// <param name="open">Whether the header reports an expanded disclosure.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingPairedTargetHasNoControlsReference(
        bool open
    )
    {
        using IRenderedComponent<BankAccountPageHeader> cut = Render<BankAccountPageHeader>(parameters =>
            parameters.Add(component => component.IsConnectionDetailsOpen, open));
        Assert.False(cut.Find("header button").HasAttribute("aria-controls"));
        Assert.Equal(open ? "true" : "false", cut.Find("header button").GetAttribute("aria-expanded"));
    }
}