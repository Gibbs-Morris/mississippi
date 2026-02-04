using System;
using System.Linq;

using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="AccountOperationsSection" />.
/// </summary>
public sealed class AccountOperationsSectionTests : BunitContext
{
    /// <summary>
    ///     Start transfer button invokes callback.
    /// </summary>
    [Fact]
    public void StartTransferInvokesCallback()
    {
        bool started = false;
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.SelectedEntityId, "account-1")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.OnStartTransfer, EventCallback.Factory.Create(this, () => started = true)));
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Start Transfer", StringComparison.Ordinal))
            .Click();
        Assert.True(started);
    }

    /// <summary>
    ///     Transfer status placeholder renders when projection is missing.
    /// </summary>
    [Fact]
    public void TransferStatusPlaceholderRendersWhenMissing()
    {
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.SelectedEntityId, "account-1")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false));
        Assert.Contains("Start a transfer to see saga status.", cut.Markup, StringComparison.Ordinal);
    }
}