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
        using IRenderedComponent<BankAccountPageHeader> cut = Render<BankAccountPageHeader>();
        Assert.Equal("false", cut.Find("header button").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("header button").HasAttribute("aria-controls"));
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
            .Add(c => c.IsConnectionDetailsOpen, true)
            .Add(c => c.OnNavigateInvestigations, EventCallback.Factory.Create(this, () => navigated = true))
            .Add(c => c.OnToggleConnectionDetails, EventCallback.Factory.Create(this, () => toggled = true)));
        cut.Find("header button").Click();
        cut.Find("nav button").Click();
        Assert.True(toggled);
        Assert.True(navigated);
        Assert.Equal("true", cut.Find("header button").GetAttribute("aria-expanded"));
        Assert.Equal("spring-connection-details", cut.Find("header button").GetAttribute("aria-controls"));
        Assert.Equal("Account actions", cut.Find("nav").GetAttribute("aria-label"));
    }
}