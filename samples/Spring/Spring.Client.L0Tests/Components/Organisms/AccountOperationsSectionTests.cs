using System;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;
using MississippiSamples.Spring.Client.Features.MoneyTransferStatus.Dtos;


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
    ///     A transfer destination displays its supplied account ID and remains read-only.
    /// </summary>
    [Fact]
    public void TransferDestinationShowsSuppliedAccountIdAndRemainsReadOnly()
    {
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.SelectedEntityId, "account-a-id")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.TransferDestinationAccountId, "account-b-id")
            .Add(c => c.IsTransferDestinationReadOnly, true));
        IElement destination = cut.Find("#account-a-transfer-destination-input");
        Assert.Equal("account-b-id", destination.GetAttribute("value"));
        Assert.True(destination.HasAttribute("readonly"));
        Assert.True(destination.HasAttribute("disabled"));
    }

    /// <summary>
    ///     Every mapped saga phase is presented with its supported Refraction state.
    /// </summary>
    /// <param name="phase">The projected saga phase.</param>
    /// <param name="expectedState">The expected Refraction telemetry state.</param>
    [Theory]
    [InlineData(SagaPhaseDto.NotStarted, "quiet")]
    [InlineData(SagaPhaseDto.Completed, "complete")]
    [InlineData(SagaPhaseDto.Compensated, "alert")]
    [InlineData(SagaPhaseDto.Compensating, "busy")]
    [InlineData(SagaPhaseDto.Running, "busy")]
    public void TransferStatusMapsSagaPhaseToRefractionState(
        SagaPhaseDto phase,
        string expectedState
    )
    {
        MoneyTransferStatusProjectionDto projection = new(
            null,
            null,
            null,
            0,
            phase,
            new(2026, 9, 27, 10, 15, 0, TimeSpan.Zero));
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.TransferStatusProjection, projection));
        Assert.Equal(expectedState, cut.Find("#account-a-transfer-status").GetAttribute("data-state"));
    }

    /// <summary>
    ///     Transfer status placeholder renders when projection is missing.
    /// </summary>
    [Fact]
    public void TransferStatusPlaceholderRendersWhenMissing()
    {
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.SelectedEntityId, "account-1")
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false));
        IElement status = cut.Find("#account-a-transfer-status");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("quiet", status.GetAttribute("data-state"));
        Assert.Contains("Start a transfer to see saga status.", status.TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The failure projection is exposed through the accessible Refraction status strip.
    /// </summary>
    [Fact]
    public void TransferStatusRendersProjectionDetailsAndFailureState()
    {
        MoneyTransferStatusProjectionDto projection = new(
            new(2026, 9, 27, 10, 16, 0, TimeSpan.Zero),
            "insufficient-funds",
            "The source account does not have enough funds.",
            1,
            SagaPhaseDto.Failed,
            new(2026, 9, 27, 10, 15, 0, TimeSpan.Zero));
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.TransferSagaId, "transfer-saga-123")
            .Add(c => c.TransferStatusProjection, projection));
        IElement transferPanel = cut.Find("#account-a-transfer-panel");
        IElement status = cut.Find("#account-a-transfer-status");
        Assert.Equal("Transfer", transferPanel.QuerySelector("h2")?.TextContent.Trim());
        Assert.Contains("rf-pane", transferPanel.GetAttribute("class"), StringComparison.Ordinal);
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("Account A transfer status", status.GetAttribute("aria-label"));
        Assert.Contains("rf-telemetry-strip", status.GetAttribute("class"), StringComparison.Ordinal);
        Assert.Equal("true", status.GetAttribute("data-spring-transfer-status"));
        Assert.Equal("error", status.GetAttribute("data-state"));
        Assert.Contains("Transfer saga ID: transfer-saga-123", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Phase: Failed", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Last completed step: 1", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Started:", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Finished:", status.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Completed:", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Error code: insufficient-funds", status.TextContent, StringComparison.Ordinal);
        Assert.Contains(
            "Error: The source account does not have enough funds.",
            status.TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     A newly running saga clearly reports that no step has completed yet.
    /// </summary>
    [Fact]
    public void TransferStatusShowsNoneWhenRunningSagaHasNoCompletedStep()
    {
        MoneyTransferStatusProjectionDto projection = new(
            null,
            null,
            null,
            -1,
            SagaPhaseDto.Running,
            new(2026, 9, 27, 10, 15, 0, TimeSpan.Zero));
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.PanelLabel, "Account A")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.TransferSagaId, "transfer-saga-started")
            .Add(c => c.TransferStatusProjection, projection));
        IElement status = cut.Find("#account-a-transfer-status");
        Assert.Equal("busy", status.GetAttribute("data-state"));
        Assert.Contains("Phase: Running", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Last completed step: None", status.TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The assigned saga remains visible while its projection is still loading.
    /// </summary>
    [Fact]
    public void TransferStatusShowsWaitingWhenSagaIdExistsWithoutProjection()
    {
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(p => p
            .Add(c => c.InputIdPrefix, "account-a")
            .Add(c => c.IsAccountOpen, true)
            .Add(c => c.IsExecutingOrLoading, false)
            .Add(c => c.TransferSagaId, "transfer-saga-pending"));
        IElement status = cut.Find("#account-a-transfer-status");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("quiet", status.GetAttribute("data-state"));
        Assert.Contains("Transfer saga ID: transfer-saga-pending", status.TextContent, StringComparison.Ordinal);
        Assert.Contains("Waiting for transfer status.", status.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Start a transfer to see saga status.", status.TextContent, StringComparison.Ordinal);
    }
}