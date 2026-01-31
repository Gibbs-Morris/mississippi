using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components.Web;

using Spring.Client.Features.BankAccountBalance.Dtos;


namespace Spring.Client.Pages;

/// <summary>
///     Account Watch page for monitoring multiple account balances in real-time.
/// </summary>
/// <remarks>
///     <para>
///         This page enables users to watch multiple bank account balances simultaneously.
///         It subscribes to SignalR projections for each watched account and displays
///         real-time balance updates with visual indicators for changes.
///     </para>
/// </remarks>
public sealed partial class AccountWatch
{
    private const int MaxWatchedAccounts = 10;

    private readonly Dictionary<string, decimal?> previousBalances = new();

    private readonly Dictionary<string, DateTime> balanceChangeTimestamps = new();

    private readonly Dictionary<string, bool> balanceIncreased = new();

    private readonly List<string> watchedAccountIds = new();

    private string newAccountId = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether a new account can be added.
    /// </summary>
    private bool CanAddAccount =>
        !string.IsNullOrWhiteSpace(newAccountId) &&
        !watchedAccountIds.Contains(newAccountId.Trim(), StringComparer.OrdinalIgnoreCase) &&
        watchedAccountIds.Count < MaxWatchedAccounts;

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing)
        {
            foreach (string accountId in watchedAccountIds)
            {
                UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(accountId);
            }
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        base.OnAfterRender(firstRender);
        DetectBalanceChanges();
    }

    /// <summary>
    ///     Adds a new account to the watch list.
    /// </summary>
    private void AddAccount()
    {
        if (!CanAddAccount)
        {
            return;
        }

        string accountId = newAccountId.Trim();
        watchedAccountIds.Add(accountId);
        SubscribeToProjection<BankAccountBalanceProjectionDto>(accountId);
        newAccountId = string.Empty;
    }

    /// <summary>
    ///     Detects balance changes and updates visual indicators.
    /// </summary>
    private void DetectBalanceChanges()
    {
        DateTime now = DateTime.UtcNow;
        bool needsRerender = false;

        foreach (string accountId in watchedAccountIds)
        {
            BankAccountBalanceProjectionDto? projection =
                GetProjection<BankAccountBalanceProjectionDto>(accountId);

            if (projection is null)
            {
                continue;
            }

            decimal currentBalance = projection.Balance;

            if (previousBalances.TryGetValue(accountId, out decimal? previous) &&
                previous.HasValue &&
                previous.Value != currentBalance)
            {
                // Balance changed - record timestamp and direction for animation
                balanceChangeTimestamps[accountId] = now;
                balanceIncreased[accountId] = currentBalance > previous.Value;
                needsRerender = true;
            }

            previousBalances[accountId] = currentBalance;
        }

        // Clear old change indicators (after 2 seconds)
        List<string> expiredChanges = balanceChangeTimestamps
            .Where(kvp => (now - kvp.Value).TotalSeconds > 2)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string accountId in expiredChanges)
        {
            balanceChangeTimestamps.Remove(accountId);
            balanceIncreased.Remove(accountId);
            needsRerender = true;
        }

        if (needsRerender)
        {
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Gets the CSS class for balance change animation.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <returns>The CSS class to apply.</returns>
    private string GetBalanceChangeClass(
        string accountId
    )
    {
        if (!balanceChangeTimestamps.ContainsKey(accountId))
        {
            return string.Empty;
        }

        if (!balanceIncreased.TryGetValue(accountId, out bool increased))
        {
            return string.Empty;
        }

        return increased ? "increased" : "decreased";
    }

    /// <summary>
    ///     Handles keyboard events for the account input.
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    private void HandleKeyUp(
        KeyboardEventArgs e
    )
    {
        if (e.Key == "Enter" && CanAddAccount)
        {
            AddAccount();
        }
    }

    /// <summary>
    ///     Removes an account from the watch list.
    /// </summary>
    /// <param name="accountId">The account ID to remove.</param>
    private void RemoveAccount(
        string accountId
    )
    {
        if (watchedAccountIds.Remove(accountId))
        {
            UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(accountId);
            previousBalances.Remove(accountId);
            balanceChangeTimestamps.Remove(accountId);
            balanceIncreased.Remove(accountId);
        }
    }
}
