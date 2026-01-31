# Task 08: Account Watch Page

## Objective

Create a Blazor page that displays real-time balance updates for multiple accounts simultaneously, enabling users to watch both source and destination accounts during a transfer.

## Rationale

This is the "wow factor" demo page. Users add accounts to a watch list and see balances update live as the saga executes. With the 10-second delay between steps, they can clearly see:
1. Source balance drops (Step 1 completes)
2. 10 seconds pass...
3. Destination balance rises (Step 2 completes)

## Wireframe

```
┌─────────────────────────────────────────────────────────────────┐
│  Account Watch                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Add Account to Watch                                           │
│  ┌──────────────────────────────────┐  ┌────────────────────┐  │
│  │ Enter account ID...              │  │  + Add to Watch    │  │
│  └──────────────────────────────────┘  └────────────────────┘  │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Watched Accounts (3)                                           │
│  ─────────────────────                                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Account: alice-checking                                │   │
│  │  Holder:  Alice Smith                                   │   │
│  │  Balance: £1,250.00  ▼ (just decreased)                 │   │
│  │  Status:  Open                             [× Remove]   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Account: bob-savings                                   │   │
│  │  Holder:  Bob Jones                                     │   │
│  │  Balance: £3,100.00  ▲ (just increased)                 │   │
│  │  Status:  Open                             [× Remove]   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Account: charlie-main                                  │   │
│  │  Holder:  —                                             │   │
│  │  Balance: —                                             │   │
│  │  Status:  Not found / Not open         [× Remove]       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  ⓘ Balances update in real-time via SignalR. Start a transfer  │
│     on the Transfer page and watch the balances change!        │
└─────────────────────────────────────────────────────────────────┘
```

## Deliverables

### 1. `AccountWatch.razor`

**Location:** `samples/Spring/Spring.Client/Pages/AccountWatch.razor`

```razor
@page "/account-watch"
@inherits InletComponent

<main>
    <nav>
        <a href="/">Bank Account</a>
        <a href="/transfer">Transfer Funds</a>
        <a href="/investigations">Investigations</a>
    </nav>

    <header>
        <h1>Account Watch</h1>
        <p>Monitor multiple account balances in real-time</p>
    </header>

    <section>
        <h2>Add Account</h2>
        <div class="add-account-form">
            <input type="text"
                   @bind="newAccountId"
                   @bind:event="oninput"
                   @onkeyup="HandleKeyUp"
                   placeholder="Enter account ID to watch"
                   aria-label="Account ID" />
            <button type="button" 
                    @onclick="AddAccount"
                    disabled="@(!CanAddAccount)">
                + Add to Watch
            </button>
        </div>
        
        @if (watchedAccountIds.Count >= MaxWatchedAccounts)
        {
            <p class="limit-warning">
                Maximum of @MaxWatchedAccounts accounts can be watched at once.
            </p>
        }
    </section>

    <section>
        <h2>Watched Accounts (@watchedAccountIds.Count)</h2>
        
        @if (watchedAccountIds.Count == 0)
        {
            <div class="empty-state">
                <p>No accounts being watched.</p>
                <p>Add an account ID above to start monitoring its balance.</p>
            </div>
        }
        else
        {
            <div class="account-grid">
                @foreach (string accountId in watchedAccountIds)
                {
                    <div class="account-card @GetBalanceChangeClass(accountId)">
                        <div class="account-header">
                            <span class="account-id">@accountId</span>
                            <button type="button"
                                    class="remove-button"
                                    @onclick="() => RemoveAccount(accountId)"
                                    aria-label="Remove @accountId from watch list">
                                ×
                            </button>
                        </div>
                        
                        @{
                            var projection = GetProjection<BankAccountBalanceProjectionDto>(accountId);
                            var isLoading = IsProjectionLoading<BankAccountBalanceProjectionDto>(accountId);
                        }
                        
                        @if (isLoading)
                        {
                            <div class="account-body loading">
                                <p>Loading...</p>
                            </div>
                        }
                        else if (projection is null)
                        {
                            <div class="account-body not-found">
                                <p>Account not found or not open</p>
                            </div>
                        }
                        else
                        {
                            <div class="account-body">
                                <dl>
                                    <dt>Holder</dt>
                                    <dd>@(string.IsNullOrEmpty(projection.HolderName) ? "—" : projection.HolderName)</dd>
                                    
                                    <dt>Balance</dt>
                                    <dd class="balance @GetBalanceChangeClass(accountId)">
                                        @projection.Balance.ToString("C")
                                        @if (GetBalanceChangeClass(accountId) == "increased")
                                        {
                                            <span class="change-indicator">▲</span>
                                        }
                                        else if (GetBalanceChangeClass(accountId) == "decreased")
                                        {
                                            <span class="change-indicator">▼</span>
                                        }
                                    </dd>
                                    
                                    <dt>Status</dt>
                                    <dd class="@(projection.IsOpen ? "open" : "closed")">
                                        @(projection.IsOpen ? "Open" : "Closed")
                                    </dd>
                                </dl>
                            </div>
                        }
                    </div>
                }
            </div>
        }
    </section>

    <aside>
        <p>
            <strong>Tip:</strong> Open the 
            <a href="/transfer" target="_blank">Transfer Funds</a> 
            page and add both source and destination accounts here to 
            watch balances change in real-time during the transfer!
        </p>
    </aside>
</main>
```

### 2. `AccountWatch.razor.cs`

**Location:** `samples/Spring/Spring.Client/Pages/AccountWatch.razor.cs`

```csharp
using Microsoft.AspNetCore.Components.Web;

using Spring.Client.Features.BankAccountBalance.Dtos;


namespace Spring.Client.Pages;

public sealed partial class AccountWatch
{
    private const int MaxWatchedAccounts = 10;

    private readonly Dictionary<string, decimal?> previousBalances = new();
    private readonly Dictionary<string, DateTime> balanceChangeTimestamps = new();
    private readonly List<string> watchedAccountIds = new();
    
    private string newAccountId = string.Empty;

    private bool CanAddAccount =>
        !string.IsNullOrWhiteSpace(newAccountId) &&
        !watchedAccountIds.Contains(newAccountId, StringComparer.OrdinalIgnoreCase) &&
        watchedAccountIds.Count < MaxWatchedAccounts;

    protected override void Dispose(bool disposing)
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

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        DetectBalanceChanges();
    }

    private void AddAccount()
    {
        if (!CanAddAccount) return;

        string accountId = newAccountId.Trim();
        watchedAccountIds.Add(accountId);
        SubscribeToProjection<BankAccountBalanceProjectionDto>(accountId);
        newAccountId = string.Empty;
    }

    private void DetectBalanceChanges()
    {
        DateTime now = DateTime.UtcNow;
        bool needsRerender = false;

        foreach (string accountId in watchedAccountIds)
        {
            BankAccountBalanceProjectionDto? projection = 
                GetProjection<BankAccountBalanceProjectionDto>(accountId);
            
            if (projection is null) continue;

            decimal currentBalance = projection.Balance;
            
            if (previousBalances.TryGetValue(accountId, out decimal? previous) && 
                previous.HasValue &&
                previous.Value != currentBalance)
            {
                // Balance changed - record timestamp for animation
                balanceChangeTimestamps[accountId] = now;
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
            needsRerender = true;
        }

        if (needsRerender)
        {
            StateHasChanged();
        }
    }

    private string GetBalanceChangeClass(string accountId)
    {
        if (!balanceChangeTimestamps.ContainsKey(accountId))
            return string.Empty;

        if (!previousBalances.TryGetValue(accountId, out decimal? previous) || !previous.HasValue)
            return string.Empty;

        BankAccountBalanceProjectionDto? projection = 
            GetProjection<BankAccountBalanceProjectionDto>(accountId);
        
        if (projection is null) return string.Empty;

        return projection.Balance > previous.Value ? "increased" : "decreased";
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && CanAddAccount)
        {
            AddAccount();
        }
    }

    private void RemoveAccount(string accountId)
    {
        UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(accountId);
        watchedAccountIds.Remove(accountId);
        previousBalances.Remove(accountId);
        balanceChangeTimestamps.Remove(accountId);
    }
}
```

### 3. `AccountWatch.razor.css`

**Location:** `samples/Spring/Spring.Client/Pages/AccountWatch.razor.css`

```css
.add-account-form {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.add-account-form input {
    flex: 1;
    max-width: 300px;
}

.limit-warning {
    color: var(--warning);
    font-size: 0.875rem;
}

.empty-state {
    text-align: center;
    padding: 2rem;
    color: var(--text-muted);
}

.account-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1rem;
}

.account-card {
    background: var(--surface);
    border-radius: 0.5rem;
    border: 2px solid transparent;
    transition: border-color 0.3s ease, box-shadow 0.3s ease;
}

.account-card.increased {
    border-color: #10b981;
    box-shadow: 0 0 12px rgba(16, 185, 129, 0.3);
}

.account-card.decreased {
    border-color: #ef4444;
    box-shadow: 0 0 12px rgba(239, 68, 68, 0.3);
}

.account-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--border);
}

.account-id {
    font-weight: 600;
    font-family: monospace;
}

.remove-button {
    background: transparent;
    border: none;
    font-size: 1.25rem;
    cursor: pointer;
    color: var(--text-muted);
    padding: 0 0.25rem;
}

.remove-button:hover {
    color: var(--danger);
}

.account-body {
    padding: 1rem;
}

.account-body.loading,
.account-body.not-found {
    color: var(--text-muted);
    font-style: italic;
}

.account-body dl {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.5rem 1rem;
    margin: 0;
}

.account-body dt {
    color: var(--text-muted);
    font-size: 0.875rem;
}

.account-body dd {
    margin: 0;
    font-weight: 500;
}

.balance {
    font-size: 1.25rem;
    font-family: monospace;
}

.balance.increased {
    color: #10b981;
}

.balance.decreased {
    color: #ef4444;
}

.change-indicator {
    margin-left: 0.25rem;
    font-size: 0.875rem;
}

dd.open {
    color: #10b981;
}

dd.closed {
    color: #ef4444;
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.6; }
}

.account-card.increased,
.account-card.decreased {
    animation: pulse 0.5s ease-in-out;
}
```

### 4. Navigation Update

Ensure link exists in `MainLayout.razor` (may already be done in Task 07).

## Acceptance Criteria

- [ ] Page accessible at `/account-watch`
- [ ] Can add accounts by ID with Enter key or button
- [ ] Limit of 10 watched accounts enforced
- [ ] Each account card shows: ID, Holder Name, Balance, Status
- [ ] Balance updates in real-time via SignalR
- [ ] Visual feedback when balance increases (green glow + ▲)
- [ ] Visual feedback when balance decreases (red glow + ▼)
- [ ] Change indicator disappears after 2 seconds
- [ ] Can remove accounts from watch list
- [ ] Handles non-existent/closed accounts gracefully
- [ ] Responsive grid layout
- [ ] Navigation links present

## Demo Flow

1. User opens `/account-watch` in one browser tab
2. Adds `alice-checking` and `bob-savings` to watch list
3. Opens `/transfer` in another tab
4. Initiates transfer from `alice-checking` to `bob-savings` for £100
5. Watches `/account-watch` tab:
   - `alice-checking` balance drops by £100 (red flash)
   - 10 seconds pass...
   - `bob-savings` balance rises by £100 (green flash)

## Dependencies

- Existing `BankAccountBalanceProjectionDto` and SignalR infrastructure
- Uses same projection subscription pattern as Index page

## Blocked By

- Nothing (uses existing infrastructure)

## Blocks

- [10-integration-testing](10-integration-testing.md)
