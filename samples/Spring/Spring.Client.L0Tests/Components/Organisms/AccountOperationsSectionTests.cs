using System;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="AccountOperationsSection" />.
/// </summary>
public sealed class AccountOperationsSectionTests : BunitContext
{
    private static IElement FindButton(
        IRenderedComponent<AccountOperationsSection> cut,
        string text
    ) =>
        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));

    /// <summary>
    ///     Each panel gives every input a unique ID and an associated visible label.
    /// </summary>
    [Fact]
    public void AccountPanelInputsHaveUniqueIdsAndLabels()
    {
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.SelectedEntityId, "account-a-id")
            .Add(c => c.IsAccountOpen, false)
            .Add(c => c.IsExecutingOrLoading, false));
        string[] inputIds = cut.FindAll("input").Select(input => input.GetAttribute("id") ?? string.Empty).ToArray();
        Assert.Equal(6, inputIds.Length);
        Assert.Equal(inputIds.Length, inputIds.Distinct(StringComparer.Ordinal).Count());
        foreach (string inputId in inputIds)
        {
            Assert.Single(cut.FindAll($"label[for='{inputId}']"));
        }
    }

    /// <summary>
    ///     Editing one account's amount draft does not update the other account panel.
    /// </summary>
    [Fact]
    public void AccountPanelsKeepAmountCallbacksAndInvalidDraftsIndependent()
    {
        decimal? accountADeposit = null;
        decimal? accountBDeposit = null;
        using IRenderedComponent<AccountOperationsSection> accountA = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.SelectedEntityId, "account-a-id")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.DepositAmount, 0m)
            .Add(
                c => c.DepositAmountChanged,
                EventCallback.Factory.Create<decimal>(this, value => accountADeposit = value)));
        using IRenderedComponent<AccountOperationsSection> accountB = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account B")
            .Add(c => c.InputIdPrefix, "account-b")
            .Add(c => c.SelectedEntityId, "account-b-id")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.DepositAmount, 0m)
            .Add(
                c => c.DepositAmountChanged,
                EventCallback.Factory.Create<decimal>(this, value => accountBDeposit = value)));
        accountA.Find("#account-a-deposit-amount-input").Input("12.34");
        accountB.Find("#account-b-deposit-amount-input").Input("12.");
        Assert.Equal(12.34m, accountADeposit);
        Assert.Null(accountBDeposit);
        Assert.False(FindButton(accountA, "Deposit £").HasAttribute("disabled"));
        Assert.True(FindButton(accountB, "Deposit £").HasAttribute("disabled"));
        Assert.Equal("12.34", accountA.Find("#account-a-deposit-amount-input").GetAttribute("value"));
        Assert.Equal("12.", accountB.Find("#account-b-deposit-amount-input").GetAttribute("value"));
    }

    /// <summary>
    ///     Invalid initial deposits and withdrawals disable their direct actions.
    /// </summary>
    [Fact]
    public void InvalidOpeningAndWithdrawalAmountsDisableDirectActions()
    {
        using IRenderedComponent<AccountOperationsSection> closedAccount = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.IsAccountOpen, false)
            .Add(c => c.IsExecutingOrLoading, false));
        using IRenderedComponent<AccountOperationsSection> openAccount = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account B")
            .Add(c => c.InputIdPrefix, "account-b")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false));
        closedAccount.Find("#account-a-initial-deposit-input").Input("12.");
        openAccount.Find("#account-b-withdraw-amount-input").Input("invalid");
        Assert.True(FindButton(closedAccount, "Open Account").HasAttribute("disabled"));
        Assert.True(FindButton(openAccount, "Withdraw").HasAttribute("disabled"));
    }

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