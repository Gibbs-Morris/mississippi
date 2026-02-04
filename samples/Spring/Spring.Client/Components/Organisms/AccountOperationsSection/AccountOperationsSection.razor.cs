using Microsoft.AspNetCore.Components;

using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.BankAccountLedger.Dtos;
using Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace Spring.Client.Components.Organisms;

/// <summary>
///     Bank account operations section.
/// </summary>
public sealed partial class AccountOperationsSection
{
    /// <summary>Gets or sets the balance projection.</summary>
    [Parameter]
    public BankAccountBalanceProjectionDto BalanceProjection { get; set; } = default!;

    /// <summary>Gets or sets the deposit amount.</summary>
    [Parameter]
    public decimal DepositAmount { get; set; }

    /// <summary>Gets or sets the callback when the deposit amount changes.</summary>
    [Parameter]
    public EventCallback<decimal> DepositAmountChanged { get; set; }

    /// <summary>Gets or sets the error message.</summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the holder name.</summary>
    [Parameter]
    public string HolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback when the holder name changes.</summary>
    [Parameter]
    public EventCallback<string> HolderNameChanged { get; set; }

    /// <summary>Gets or sets the initial deposit.</summary>
    [Parameter]
    public decimal InitialDeposit { get; set; }

    /// <summary>Gets or sets the callback when the initial deposit changes.</summary>
    [Parameter]
    public EventCallback<decimal> InitialDepositChanged { get; set; }

    /// <summary>Gets or sets a value indicating whether the account is open.</summary>
    [Parameter]
    public bool IsAccountOpen { get; set; }

    /// <summary>Gets or sets a value indicating whether execution is in progress.</summary>
    [Parameter]
    public bool IsExecutingOrLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether the transfer destination is read-only.</summary>
    [Parameter]
    public bool IsTransferDestinationReadOnly { get; set; }

    /// <summary>Gets or sets the last command success state.</summary>
    [Parameter]
    public bool? LastCommandSucceeded { get; set; }

    /// <summary>Gets or sets the ledger projection.</summary>
    [Parameter]
    public BankAccountLedgerProjectionDto LedgerProjection { get; set; } = default!;

    /// <summary>Gets or sets the callback for deposit action.</summary>
    [Parameter]
    public EventCallback OnDeposit { get; set; }

    /// <summary>Gets or sets the callback for the 20x £5 deposit burst.</summary>
    [Parameter]
    public EventCallback OnDepositBurst20 { get; set; }

    /// <summary>Gets or sets the callback for the 200x £10 deposit burst.</summary>
    [Parameter]
    public EventCallback OnDepositBurst200 { get; set; }

    /// <summary>Gets or sets the callback for the single £100 deposit.</summary>
    [Parameter]
    public EventCallback OnDepositSingle100 { get; set; }

    /// <summary>Gets or sets the callback for opening the account.</summary>
    [Parameter]
    public EventCallback OnOpenAccount { get; set; }

    /// <summary>Gets or sets the callback to start a transfer.</summary>
    [Parameter]
    public EventCallback OnStartTransfer { get; set; }

    /// <summary>Gets or sets the callback to switch accounts.</summary>
    [Parameter]
    public EventCallback OnSwitchAccount { get; set; }

    /// <summary>Gets or sets the callback for withdraw action.</summary>
    [Parameter]
    public EventCallback OnWithdraw { get; set; }

    /// <summary>Gets or sets the callback for the 20x £5 withdrawal burst.</summary>
    [Parameter]
    public EventCallback OnWithdrawBurst20 { get; set; }

    /// <summary>Gets or sets the callback for the 200x £10 withdrawal burst.</summary>
    [Parameter]
    public EventCallback OnWithdrawBurst200 { get; set; }

    /// <summary>Gets or sets the callback for the single £100 withdrawal.</summary>
    [Parameter]
    public EventCallback OnWithdrawSingle100 { get; set; }

    /// <summary>Gets or sets the panel label shown in the header.</summary>
    [Parameter]
    public string PanelLabel { get; set; } = "Account";

    /// <summary>Gets or sets the selected entity identifier.</summary>
    [Parameter]
    public string SelectedEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer amount.</summary>
    [Parameter]
    public decimal TransferAmount { get; set; }

    /// <summary>Gets or sets the callback when the transfer amount changes.</summary>
    [Parameter]
    public EventCallback<decimal> TransferAmountChanged { get; set; }

    /// <summary>Gets or sets the transfer destination account id.</summary>
    [Parameter]
    public string TransferDestinationAccountId { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback when transfer destination changes.</summary>
    [Parameter]
    public EventCallback<string> TransferDestinationAccountIdChanged { get; set; }

    /// <summary>Gets or sets the transfer saga id.</summary>
    [Parameter]
    public string? TransferSagaId { get; set; }

    /// <summary>Gets or sets the transfer status projection.</summary>
    [Parameter]
    public MoneyTransferStatusProjectionDto TransferStatusProjection { get; set; } = default!;

    /// <summary>Gets or sets the withdraw amount.</summary>
    [Parameter]
    public decimal WithdrawAmount { get; set; }

    /// <summary>Gets or sets the callback when the withdraw amount changes.</summary>
    [Parameter]
    public EventCallback<decimal> WithdrawAmountChanged { get; set; }
}