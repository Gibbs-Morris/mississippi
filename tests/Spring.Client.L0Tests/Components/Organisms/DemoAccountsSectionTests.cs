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
}