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

    /// <summary>
    ///     The initialized shortcut retains the supplied encoded pair and is unavailable before initialization.
    /// </summary>
    [Fact]
    public void InitializedShortcutPreservesBothEncodedAccountIds()
    {
        const string operationsUri = "/operations?a=account%20A%2F%26&b=account%20B%3F%23";
        using IRenderedComponent<DemoAccountsSection> cut = Render<DemoAccountsSection>(parameters => parameters
            .Add(component => component.IsInitialized, true)
            .Add(component => component.AccountAId, "account A/&")
            .Add(component => component.AccountBId, "account B?#")
            .Add(component => component.OperationsUri, new(operationsUri, UriKind.Relative)));
        Assert.Equal(operationsUri, cut.Find(".spring-demo-actions a").GetAttribute("href"));
        Assert.Contains("Go to Operations", cut.Find(".spring-demo-actions a").TextContent, StringComparison.Ordinal);
        cut.Render(parameters => parameters.Add(component => component.IsInitialized, false));
        Assert.Empty(cut.FindAll(".spring-demo-actions a"));
    }
}