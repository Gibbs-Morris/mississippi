using System;
using System.Globalization;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client;

using MississippiSamples.Spring.Client.Components.Atoms.AmountInputAdapter;
using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.BankAccountLedger.Dtos;
using MississippiSamples.Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace MississippiSamples.Spring.Client.Components.Organisms;

/// <summary>
///     Bank account operations section.
/// </summary>
public sealed partial class AccountOperationsSection
{
    private bool isDepositAmountValid = true;

    private bool isInitialDepositValid = true;

    private bool isTransferAmountValid = true;

    private bool isWithdrawAmountValid = true;

    /// <summary>Gets or sets the balance projection.</summary>
    [Parameter]
    public BankAccountBalanceProjectionDto BalanceProjection { get; set; } = default!;

    /// <summary>Gets or sets the observed balance projection version.</summary>
    [Parameter]
    public long BalanceVersion { get; set; } = -1;

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

    /// <summary>Gets or sets the stable prefix used for this panel's native input IDs.</summary>
    [Parameter]
    public string InputIdPrefix { get; set; } = "account";

    /// <summary>Gets or sets a value indicating whether the account is open.</summary>
    [Parameter]
    public bool IsAccountOpen { get; set; }

    /// <summary>Gets or sets a value indicating whether the balance is loading.</summary>
    [Parameter]
    public bool IsBalanceLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether execution is in progress.</summary>
    [Parameter]
    public bool IsExecutingOrLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether the ledger is loading.</summary>
    [Parameter]
    public bool IsLedgerLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether the shared SignalR connection is connected.</summary>
    [Parameter]
    public bool IsLiveConnectionConnected { get; set; }

    /// <summary>Gets or sets a value indicating whether the transfer destination is read-only.</summary>
    [Parameter]
    public bool IsTransferDestinationReadOnly { get; set; }

    /// <summary>Gets or sets the ledger read error.</summary>
    [Parameter]
    public string? LedgerError { get; set; }

    /// <summary>Gets or sets the ledger projection.</summary>
    [Parameter]
    public BankAccountLedgerProjectionDto LedgerProjection { get; set; } = default!;

    /// <summary>Gets or sets the observed ledger projection version.</summary>
    [Parameter]
    public long LedgerVersion { get; set; } = -1;

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

    /// <summary>Gets or sets the transfer-status read error.</summary>
    [Parameter]
    public string? TransferReadError { get; set; }

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

    private string DepositAmountInputId => GetInputId("deposit-amount-input");

    private string HolderNameInputId => GetInputId("holder-name-input");

    private string InitialDepositInputId => GetInputId("initial-deposit-input");

    private string LastCompletedStepText =>
        TransferStatusProjection is not null && (TransferStatusProjection.LastCompletedStepIndex >= 0)
            ? TransferStatusProjection.LastCompletedStepIndex.ToString(CultureInfo.CurrentCulture)
            : "None";

    private string PanelHeadingId => GetInputId("panel-heading");

    private string TransferAmountInputId => GetInputId("transfer-amount-input");

    private string TransferDestinationInputId => GetInputId("transfer-destination-input");

    private string TransferOutcomeText =>
        TransferStatusProjection?.Phase switch
        {
            SagaPhaseDto.Completed =>
                "Transfer completed. Verify the debit and credit in both live balances and ledgers.",
            SagaPhaseDto.Compensated when TransferStatusProjection.LastCompletedStepIndex < 0 =>
                "No step completed. No compensating account action was required. Read the error and verify both balances.",
            SagaPhaseDto.Compensated =>
                "The saga reports compensation. Check the source balance and ledger for the reversing deposit.",
            SagaPhaseDto.Compensating => "A step failed. The saga is attempting its defined compensation.",
            SagaPhaseDto.Failed =>
                "Transfer failed. Read the error and verify both accounts before starting another transfer.",
            var _ => "The transfer is pending. Its final outcome has not arrived.",
        };

    private string TransferPanelId => GetInputId("transfer-panel");

    private string TransferStatusId => GetInputId("transfer-status");

    private string TransferStatusState =>
        TransferStatusProjection?.Phase switch
        {
            SagaPhaseDto.Completed => RefractionStates.Complete,
            SagaPhaseDto.Compensated => RefractionStates.Alert,
            SagaPhaseDto.Compensating or SagaPhaseDto.Running => RefractionStates.Busy,
            SagaPhaseDto.Failed => RefractionStates.Error,
            var _ => RefractionStates.Quiet,
        };

    private string WithdrawAmountInputId => GetInputId("withdraw-amount-input");

    private static string FormatAmount(
        decimal amount
    ) =>
        "£" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private static string FormatTransferTimestamp(
        DateTimeOffset? timestamp
    ) =>
        timestamp?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;

    private string GetInputId(
        string inputName
    ) =>
        $"{InputIdPrefix}-{inputName}";

    private RenderFragment RenderAmountInput(
        string inputId,
        string label,
        decimal value,
        EventCallback<decimal> valueChanged,
        Action<bool> validityChanged,
        bool isDisabled
    ) =>
        builder =>
        {
            builder.OpenComponent<SpringAmountInput>(0);
            builder.AddAttribute(1, nameof(SpringAmountInput.InputId), inputId);
            builder.AddAttribute(2, nameof(SpringAmountInput.Label), label);
            builder.AddAttribute(3, nameof(SpringAmountInput.Value), value);
            builder.AddAttribute(4, nameof(SpringAmountInput.ValueChanged), valueChanged);
            builder.AddAttribute(
                5,
                nameof(SpringAmountInput.IsValidChanged),
                EventCallback.Factory.Create(this, validityChanged));
            builder.AddAttribute(6, nameof(SpringAmountInput.IsDisabled), isDisabled);
            builder.CloseComponent();
        };

    private void SetDepositAmountValidity(
        bool isValid
    ) =>
        isDepositAmountValid = isValid;

    private void SetInitialDepositValidity(
        bool isValid
    ) =>
        isInitialDepositValid = isValid;

    private void SetTransferAmountValidity(
        bool isValid
    ) =>
        isTransferAmountValid = isValid;

    private void SetWithdrawAmountValidity(
        bool isValid
    ) =>
        isWithdrawAmountValid = isValid;
}