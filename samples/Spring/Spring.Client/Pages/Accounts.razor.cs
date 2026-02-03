using System;
using System.Globalization;

using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Selectors;
using Spring.Client.Features.BankAccountAggregate.State;
using Spring.Client.Features.DemoAccounts;
using Spring.Client.Features.EntitySelection;
using Spring.Client.Features.EntitySelection.Selectors;


namespace Spring.Client.Pages;

/// <summary>
///     Demo account setup and selection page.
/// </summary>
public sealed partial class Accounts
{
    private string accountIdInput = string.Empty;

    private bool isConnectionModalOpen;

    /// <summary>
    ///     Gets the current connection identifier.
    /// </summary>
    private string? ConnectionId => Select<SignalRConnectionState, string?>(SignalRConnectionSelectors.GetConnectionId);

    /// <summary>
    ///     Gets the connection identifier display value.
    /// </summary>
    private string ConnectionIdDisplay => ConnectionId ?? "—";

    /// <summary>
    ///     Gets the current connection status.
    /// </summary>
    private SignalRConnectionStatus ConnectionStatus =>
        Select<SignalRConnectionState, SignalRConnectionStatus>(SignalRConnectionSelectors.GetStatus);

    /// <summary>
    ///     Gets the demo account A identifier.
    /// </summary>
    private string? DemoAccountAId => Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountAId);

    /// <summary>
    ///     Gets the demo account A display name.
    /// </summary>
    private string? DemoAccountAName => Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountAName);

    /// <summary>
    ///     Gets the demo account B identifier.
    /// </summary>
    private string? DemoAccountBId => Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountBId);

    /// <summary>
    ///     Gets the demo account B display name.
    /// </summary>
    private string? DemoAccountBName => Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountBName);

    /// <summary>
    ///     Gets a value indicating whether demo accounts have been initialized.
    /// </summary>
    private bool IsDemoAccountsInitialized => Select<DemoAccountsState, bool>(DemoAccountsSelectors.IsInitialized);

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is not established.
    /// </summary>
    private bool IsDisconnected => Select<SignalRConnectionState, bool>(SignalRConnectionSelectors.IsDisconnected);

    /// <summary>
    ///     Gets a value indicating whether any operation is in progress.
    /// </summary>
    private bool IsExecutingOrLoading =>
        Select<BankAccountAggregateState, ProjectionsFeatureState, bool>(
            BankAccountCompositeSelectors.IsOperationInProgress(SelectedEntityId));

    private bool IsSelectedAccountA =>
        !string.IsNullOrEmpty(DemoAccountAId) &&
        string.Equals(SelectedEntityId, DemoAccountAId, StringComparison.Ordinal);

    private bool IsSelectedAccountB =>
        !string.IsNullOrEmpty(DemoAccountBId) &&
        string.Equals(SelectedEntityId, DemoAccountBId, StringComparison.Ordinal);

    /// <summary>
    ///     Gets the timestamp when the connection was last successfully established.
    /// </summary>
    private DateTimeOffset? LastConnectedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastConnectedAt);

    /// <summary>
    ///     Gets the last connected timestamp display value.
    /// </summary>
    private string LastConnectedAtDisplay => FormatTimestamp(LastConnectedAt);

    /// <summary>
    ///     Gets the timestamp when the connection was last disconnected.
    /// </summary>
    private DateTimeOffset? LastDisconnectedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastDisconnectedAt);

    /// <summary>
    ///     Gets the last disconnected timestamp display value.
    /// </summary>
    private string LastDisconnectedAtDisplay => FormatTimestamp(LastDisconnectedAt);

    /// <summary>
    ///     Gets the last error message from the connection.
    /// </summary>
    private string? LastError => Select<SignalRConnectionState, string?>(SignalRConnectionSelectors.GetLastError);

    /// <summary>
    ///     Gets the timestamp when the last message was received.
    /// </summary>
    private DateTimeOffset? LastMessageReceivedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastMessageReceivedAt);

    /// <summary>
    ///     Gets the last message timestamp display value.
    /// </summary>
    private string LastMessageReceivedAtDisplay => FormatTimestamp(LastMessageReceivedAt);

    /// <summary>
    ///     Gets the current reconnection attempt count.
    /// </summary>
    private int ReconnectAttemptCount =>
        Select<SignalRConnectionState, int>(SignalRConnectionSelectors.GetReconnectAttemptCount);

    /// <summary>
    ///     Gets the currently selected entity ID from entity selection state.
    /// </summary>
    private string? SelectedEntityId => Select<EntitySelectionState, string?>(EntitySelectionSelectors.GetEntityId);

    private static string FormatTimestamp(
        DateTimeOffset? timestamp
    ) =>
        timestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";

    private void ClearEntity() => Dispatch(new SetEntityIdAction(string.Empty));

    private void CloseConnectionModal() => isConnectionModalOpen = false;

    private void InitializeDemoAccounts()
    {
        string accountAId = Guid.NewGuid().ToString();
        string accountBId = Guid.NewGuid().ToString();
        const string accountAName = "Ada Lovelace";
        const string accountBName = "Grace Hopper";
        Dispatch(new SetDemoAccountsAction(accountAId, accountAName, accountBId, accountBName));
        Dispatch(new OpenAccountAction(accountAId, accountAName, 500m));
        Dispatch(new OpenAccountAction(accountBId, accountBName, 500m));
        Dispatch(new SetEntityIdAction(accountAId));
    }

    private void NavigateToInvestigations() => Dispatch(new NavigateAction("/investigations"));

    private void RequestReconnect() => Dispatch(new RequestSignalRConnectionAction());

    private void SetEntityId() => Dispatch(new SetEntityIdAction(accountIdInput));

    private void SwitchToDemoAccountA()
    {
        if (!string.IsNullOrWhiteSpace(DemoAccountAId))
        {
            Dispatch(new SetEntityIdAction(DemoAccountAId));
        }
    }

    private void SwitchToDemoAccountB()
    {
        if (!string.IsNullOrWhiteSpace(DemoAccountBId))
        {
            Dispatch(new SetEntityIdAction(DemoAccountBId));
        }
    }

    private void ToggleConnectionModal() => isConnectionModalOpen = !isConnectionModalOpen;
}