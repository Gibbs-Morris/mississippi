# Test Fixtures

This folder contains pre-wired test fixtures that eliminate boilerplate when testing Spring domain logic.

## Available Fixtures

| Fixture | Purpose | Common Methods |
|---------|---------|----------------|
| `BankAccountFixture` | BankAccount aggregate testing | `OpenAccount()`, `ClosedAccount()`, `AccountWithBalance()` |
| `BankAccountBalanceFixture` | BankAccountBalance projection testing | `OpenAccount()`, `Empty()`, `Replay()` |
| `BankAccountLedgerFixture` | BankAccountLedger projection testing | `Empty()`, `WithEntries()`, `Replay()` |
| `FlaggedTransactionsFixture` | FlaggedTransactions projection testing | `Empty()`, `WithEntries()`, `Replay()` |
| `TransactionInvestigationQueueFixture` | Investigation queue aggregate testing | `EmptyQueue()`, `WithFlaggedCount()` |
| `HighValueTransactionEffectFixture` | High-value effect testing | `ProcessDepositAsync()`, `CreateHarness()` |

## Before/After Comparison

### Aggregate Testing

**Before (verbose):**

```csharp
AggregateTestHarness<BankAccountAggregate> harness = CommandHandlerTestExtensions
    .ForAggregate<BankAccountAggregate>()
    .WithHandler<OpenAccountHandler>()
    .WithHandler<DepositFundsHandler>()
    .WithHandler<WithdrawFundsHandler>()
    .WithReducer<AccountOpenedReducer>()
    .WithReducer<FundsDepositedReducer>()
    .WithReducer<FundsWithdrawnReducer>();

harness.CreateScenario()
    .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
    .When(new DepositFunds { Amount = 50m })
    .ThenEmits<FundsDeposited>(evt => evt.Amount.Should().Be(50m));
```

**After (concise):**

```csharp
BankAccountFixture.OpenAccount("John", 100m)
    .When(new DepositFunds { Amount = 50m })
    .ThenEmits<FundsDeposited>(evt => evt.Amount.Should().Be(50m));
```

### Projection Testing

**Before (verbose):**

```csharp
ReducerTestHarness<BankAccountBalanceProjection> harness = ReducerTestExtensions
    .ForProjection<BankAccountBalanceProjection>()
    .WithReducer<AccountOpenedBalanceReducer>()
    .WithReducer<FundsDepositedBalanceReducer>()
    .WithReducer<FundsWithdrawnBalanceReducer>();

var result = harness.ApplyEvents(
    new AccountOpened { HolderName = "John", InitialDeposit = 100m },
    new FundsDeposited { Amount = 50m });
```

**After (concise):**

```csharp
var result = BankAccountBalanceFixture.Replay(
    new AccountOpened { HolderName = "John", InitialDeposit = 100m },
    new FundsDeposited { Amount = 50m });
```

### Effect Testing

**Before (verbose):**

```csharp
var harness = EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>
    .Create()
    .WithGrainKey("acc-123")
    .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());

var effect = harness.Build((f, c, l) => new HighValueTransactionEffect(f, c, l));
await harness.InvokeAsync(effect, evt, state);
harness.DispatchedCommands.ShouldHaveDispatched<FlagTransaction>();
```

**After (concise):**

```csharp
var result = await HighValueTransactionEffectFixture.ProcessDepositAsync("acc-123", 15_000m);
result.ShouldHaveDispatchedFlagTransaction()
    .WithAccountId("acc-123")
    .WithAmount(15_000m);
```

## Summary

| Aspect | Before | After |
|--------|--------|-------|
| Aggregate Setup | 6+ lines of `.WithHandler`/`.WithReducer` | One-liner fixture method |
| Projection Setup | 4+ lines of `.WithReducer` | `Replay()` one-liner |
| Effect Setup | 10+ lines with 3 generic type params | `ProcessDepositAsync()` |
| Assertions | Generic `ShouldEmit`/`ShouldHaveDispatched` | Domain-specific `ShouldHaveFlagged` |
| Discoverability | Must know all handler/reducer types | Fixture exposes common scenarios |
