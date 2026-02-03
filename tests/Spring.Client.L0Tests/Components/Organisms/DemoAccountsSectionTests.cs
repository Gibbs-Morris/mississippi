using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="DemoAccountsSection" />.
/// </summary>
public sealed class DemoAccountsSectionTests : BunitContext
{
    /// <summary>
    ///     Initialize button triggers the callback.
    /// </summary>
    [Fact]
    public void InitializeButtonInvokesCallback()
    {
        bool initialized = false;
        using IRenderedComponent<DemoAccountsSection> cut = Render<DemoAccountsSection>(p => p
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.IsInitialized, false)
            .Add(c => c.OnInitialize, EventCallback.Factory.Create(this, () => initialized = true)));
        cut.Find("button").Click();
        Assert.True(initialized);
    }

    /// <summary>
    ///     Switch buttons respect selection state.
    /// </summary>
    [Fact]
    public void SwitchButtonsReflectSelectionState()
    {
        using IRenderedComponent<DemoAccountsSection> cut = Render<DemoAccountsSection>(p => p
            .Add(c => c.IsInitialized, true)
            .Add(c => c.IsSelectedAccountA, true)
            .Add(c => c.IsSelectedAccountB, false));
        IElement[] buttons = cut.FindAll("button").ToArray();
        Assert.True(buttons[1].HasAttribute("disabled"));
        Assert.False(buttons[2].HasAttribute("disabled"));
    }
}