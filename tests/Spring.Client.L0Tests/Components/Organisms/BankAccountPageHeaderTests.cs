using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="BankAccountPageHeader" />.
/// </summary>
public sealed class BankAccountPageHeaderTests : BunitContext
{
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
            .Add(c => c.IsConnectionModalOpen, true)
            .Add(c => c.OnNavigateInvestigations, EventCallback.Factory.Create(this, () => navigated = true))
            .Add(c => c.OnToggleConnectionModal, EventCallback.Factory.Create(this, () => toggled = true)));
        cut.Find("header button").Click();
        cut.Find("nav button").Click();
        Assert.True(toggled);
        Assert.True(navigated);
        Assert.True(cut.Find("header button").HasAttribute("aria-expanded"));
    }
}