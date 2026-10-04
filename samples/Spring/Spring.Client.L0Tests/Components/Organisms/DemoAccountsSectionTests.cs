using System;

using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

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
    ///     The setup action is disabled while account initialization is in progress.
    /// </summary>
    [Fact]
    public void InitializeButtonIsDisabledWhileLoading()
    {
        using IRenderedComponent<DemoAccountsSection> cut = Render<DemoAccountsSection>(p => p
            .Add(c => c.IsExecutingOrLoading, true)
            .Add(c => c.IsInitialized, false));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    /// <summary>
    ///     Initialized account IDs have separate, labeled output targets.
    /// </summary>
    [Fact]
    public void InitializedAccountIdsRenderInDistinctTargets()
    {
        using IRenderedComponent<DemoAccountsSection> cut = Render<DemoAccountsSection>(p => p
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.IsInitialized, true)
            .Add(c => c.AccountAId, "account-a-id")
            .Add(c => c.AccountAName, "Ada Lovelace")
            .Add(c => c.AccountBId, "account-b-id")
            .Add(c => c.AccountBName, "Grace Hopper"));
        Assert.Equal("account-a-id", cut.Find("#demo-account-a-id").TextContent);
        Assert.Equal("account-b-id", cut.Find("#demo-account-b-id").TextContent);
        Assert.Contains(
            "Ada Lovelace",
            cut.Find("#demo-account-a-id").ParentElement?.TextContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "Grace Hopper",
            cut.Find("#demo-account-b-id").ParentElement?.TextContent,
            StringComparison.Ordinal);
    }
}