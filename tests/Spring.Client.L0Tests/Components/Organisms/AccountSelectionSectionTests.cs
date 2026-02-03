using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="AccountSelectionSection" />.
/// </summary>
public sealed class AccountSelectionSectionTests : BunitContext
{
    /// <summary>
    ///     Continue button invokes callback.
    /// </summary>
    [Fact]
    public void ContinueButtonInvokesCallback()
    {
        bool continued = false;
        using IRenderedComponent<AccountSelectionSection> cut = Render<AccountSelectionSection>(p => p.Add(
            c => c.OnContinue,
            EventCallback.Factory.Create(this, () => continued = true)));
        cut.Find("button").Click();
        Assert.True(continued);
    }

    /// <summary>
    ///     Input updates propagate through the callback.
    /// </summary>
    [Fact]
    public void InputUpdatesInvokeCallback()
    {
        string? latest = null;
        using IRenderedComponent<AccountSelectionSection> cut = Render<AccountSelectionSection>(p => p
            .Add(c => c.AccountIdInput, "abc")
            .Add(c => c.AccountIdInputChanged, EventCallback.Factory.Create<string>(this, value => latest = value)));
        cut.Find("input").Change("xyz");
        Assert.Equal("xyz", latest);
    }
}