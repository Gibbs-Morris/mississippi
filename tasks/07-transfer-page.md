# Task 07: Transfer Funds Page

## Objective

Create a Blazor page where users can initiate a money transfer between two accounts and watch the saga progress in real-time.

## Rationale

This is the primary demo UI for the saga feature. It should be simple, clear, and show the saga pattern in action.

## Wireframe

```
┌─────────────────────────────────────────────────────────────────┐
│  Transfer Funds                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  From Account: [________________]                               │
│                                                                 │
│  To Account:   [________________]                               │
│                                                                 │
│  Amount (£):   [________________]                               │
│                                                                 │
│  [  Transfer Funds  ]                                           │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Transfer Status                                                │
│  ─────────────────                                              │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  ● Step 1: Debit Source Account    [=====>    ] Running │   │
│  │  ○ Step 2: Credit Destination      [          ] Pending │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Saga ID: 3f7a2b1c-...                                         │
│  Started: 14:32:05                                              │
│  Status: Running                                                │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  ⓘ Tip: Open the Account Watch page in another tab to see      │
│         balances update in real-time during the transfer!       │
└─────────────────────────────────────────────────────────────────┘
```

## Deliverables

### 1. `Transfer.razor`

**Location:** `samples/Spring/Spring.Client/Pages/Transfer.razor`

```razor
@page "/transfer"
@inherits InletComponent

<main>
    <nav>
        <a href="/">Bank Account</a>
        <a href="/account-watch">Account Watch</a>
        <a href="/investigations">Investigations</a>
    </nav>

    <header>
        <h1>Transfer Funds</h1>
        <p>Transfer money between two accounts using a saga</p>
    </header>

    <section>
        <h2>New Transfer</h2>
        <fieldset disabled="@IsTransferInProgress">
            <legend>Transfer Details</legend>
            
            <div>
                <label for="source-account">From Account</label>
                <input id="source-account"
                       type="text"
                       @bind="sourceAccountId"
                       placeholder="Source account ID" />
            </div>
            
            <div>
                <label for="destination-account">To Account</label>
                <input id="destination-account"
                       type="text"
                       @bind="destinationAccountId"
                       placeholder="Destination account ID" />
            </div>
            
            <div>
                <label for="amount">Amount (£)</label>
                <input id="amount"
                       type="number"
                       @bind="amount"
                       placeholder="0.00"
                       step="0.01"
                       min="0.01" />
            </div>
            
            <button type="button" 
                    @onclick="StartTransfer"
                    disabled="@(!CanStartTransfer)">
                Transfer Funds
            </button>
        </fieldset>
    </section>

    @if (CurrentSagaId.HasValue)
    {
        <section>
            <h2>Transfer Status</h2>
            @if (SagaStatus is not null)
            {
                <div class="saga-progress">
                    <div class="step @GetStepClass(1)">
                        <span class="step-icon">@GetStepIcon(1)</span>
                        <span class="step-name">Step 1: Debit Source Account</span>
                        <span class="step-status">@GetStepStatus(1)</span>
                    </div>
                    <div class="step @GetStepClass(2)">
                        <span class="step-icon">@GetStepIcon(2)</span>
                        <span class="step-name">Step 2: Credit Destination</span>
                        <span class="step-status">@GetStepStatus(2)</span>
                    </div>
                </div>
                
                <dl>
                    <dt>Saga ID</dt>
                    <dd>@CurrentSagaId</dd>
                    
                    <dt>Started</dt>
                    <dd>@SagaStatus.StartedAt?.ToString("HH:mm:ss")</dd>
                    
                    <dt>Status</dt>
                    <dd class="@SagaStatus.Phase.ToLowerInvariant()">
                        @SagaStatus.Phase
                    </dd>
                    
                    @if (!string.IsNullOrEmpty(SagaStatus.FailureReason))
                    {
                        <dt>Error</dt>
                        <dd class="error">@SagaStatus.FailureReason</dd>
                    }
                    
                    @if (SagaStatus.IsCompensating)
                    {
                        <dt>Compensation</dt>
                        <dd class="warning">Rolling back changes...</dd>
                    }
                    
                    @if (SagaStatus.CompletedAt.HasValue)
                    {
                        <dt>Completed</dt>
                        <dd>@SagaStatus.CompletedAt?.ToString("HH:mm:ss")</dd>
                    }
                </dl>
            }
            else
            {
                <p>Loading saga status...</p>
            }
        </section>
    }

    <aside>
        <p>
            <strong>Tip:</strong> Open the 
            <a href="/account-watch" target="_blank">Account Watch</a> 
            page in another tab to see balances update in real-time 
            during the transfer!
        </p>
    </aside>
</main>
```

### 2. `Transfer.razor.cs`

**Location:** `samples/Spring/Spring.Client/Pages/Transfer.razor.cs`

```csharp
namespace Spring.Client.Pages;

public sealed partial class Transfer
{
    private decimal amount = 100m;
    private Guid? currentSagaId;
    private string destinationAccountId = string.Empty;
    private string sourceAccountId = string.Empty;

    private bool CanStartTransfer =>
        !IsTransferInProgress &&
        !string.IsNullOrWhiteSpace(sourceAccountId) &&
        !string.IsNullOrWhiteSpace(destinationAccountId) &&
        sourceAccountId != destinationAccountId &&
        amount > 0;

    private Guid? CurrentSagaId => currentSagaId;

    private bool IsTransferInProgress =>
        GetState<TransferFundsSagaState>().IsExecuting;

    private SagaStatusDto? SagaStatus =>
        currentSagaId.HasValue
            ? GetProjection<SagaStatusDto>(currentSagaId.Value.ToString())
            : null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && currentSagaId.HasValue)
        {
            UnsubscribeFromProjection<SagaStatusDto>(currentSagaId.Value.ToString());
        }
        base.Dispose(disposing);
    }

    private string GetStepClass(int stepOrder)
    {
        if (SagaStatus is null) return "pending";
        
        int? currentOrder = SagaStatus.CurrentStepOrder;
        
        if (SagaStatus.Phase == "Completed") return "completed";
        if (SagaStatus.Phase == "Failed" && currentOrder == stepOrder) return "failed";
        if (SagaStatus.IsCompensating) return currentOrder >= stepOrder ? "compensating" : "completed";
        if (currentOrder == stepOrder) return "running";
        if (currentOrder > stepOrder) return "completed";
        return "pending";
    }

    private string GetStepIcon(int stepOrder)
    {
        string stepClass = GetStepClass(stepOrder);
        return stepClass switch
        {
            "completed" => "✓",
            "running" => "●",
            "failed" => "✗",
            "compensating" => "↺",
            _ => "○"
        };
    }

    private string GetStepStatus(int stepOrder)
    {
        string stepClass = GetStepClass(stepOrder);
        return stepClass switch
        {
            "completed" => "Done",
            "running" => "Running...",
            "failed" => "Failed",
            "compensating" => "Rolling back",
            _ => "Pending"
        };
    }

    private void StartTransfer()
    {
        Guid sagaId = Guid.NewGuid();
        currentSagaId = sagaId;

        // Subscribe to saga status updates
        SubscribeToProjection<SagaStatusDto>(sagaId.ToString());

        // Dispatch the generated saga action
        Dispatch(new StartTransferFundsSagaAction(
            sagaId,
            sourceAccountId,
            destinationAccountId,
            amount));
    }
}
```

### 3. `Transfer.razor.css`

**Location:** `samples/Spring/Spring.Client/Pages/Transfer.razor.css`

```css
.saga-progress {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 1rem;
    background: var(--surface);
    border-radius: 0.5rem;
    margin-bottom: 1rem;
}

.step {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem;
    border-radius: 0.25rem;
}

.step.running {
    background: rgba(59, 130, 246, 0.1);
}

.step.completed {
    opacity: 0.7;
}

.step.failed {
    background: rgba(239, 68, 68, 0.1);
}

.step.compensating {
    background: rgba(245, 158, 11, 0.1);
}

.step-icon {
    font-size: 1.25rem;
    width: 1.5rem;
    text-align: center;
}

.step.completed .step-icon { color: #10b981; }
.step.running .step-icon { color: #3b82f6; }
.step.failed .step-icon { color: #ef4444; }
.step.compensating .step-icon { color: #f59e0b; }

.step-name {
    flex: 1;
}

.step-status {
    font-size: 0.875rem;
    color: var(--text-muted);
}

dd.error {
    color: #ef4444;
}

dd.warning {
    color: #f59e0b;
}

dd.completed {
    color: #10b981;
}

dd.running {
    color: #3b82f6;
}
```

### 4. Navigation Update

Add link to `MainLayout.razor`:

```razor
<nav>
    <a href="/">Bank Account</a>
    <a href="/transfer">Transfer Funds</a>
    <a href="/account-watch">Account Watch</a>
    <a href="/investigations">Investigations</a>
</nav>
```

## Acceptance Criteria

- [ ] Page accessible at `/transfer`
- [ ] Form validates inputs (both accounts required, different, amount > 0)
- [ ] Clicking "Transfer Funds" dispatches generated `StartTransferFundsSagaAction`
- [ ] Saga status updates in real-time via SignalR
- [ ] Progress shows Step 1/2 with visual indicators
- [ ] Success state shows completed checkmarks
- [ ] Failure state shows error message and compensation status
- [ ] Form disabled during transfer
- [ ] Navigation links present
- [ ] Responsive design
- [ ] Accessible (labels, ARIA where needed)

## Dependencies

- [03-client-generators](03-client-generators.md) - `StartTransferFundsSagaAction`
- [05-domain-saga](05-domain-saga.md) - Saga definition
- [06-saga-status-projection](06-saga-status-projection.md) - Status updates

## Blocked By

- [03-client-generators](03-client-generators.md)
- [06-saga-status-projection](06-saga-status-projection.md)

## Blocks

- [10-integration-testing](10-integration-testing.md)
