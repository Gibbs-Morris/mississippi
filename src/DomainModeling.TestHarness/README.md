# DomainModeling test harness examples

These examples use the Spring sample types. The [aggregate fixture](../../samples/Spring/Spring.Domain.L0Tests/Fixtures/BankAccountFixture.cs) and [projection fixture](../../samples/Spring/Spring.Domain.L0Tests/Fixtures/BankAccountBalanceFixture.cs) show executable wiring and imports.

Use xUnit assertions in callbacks. Failed prerequisites throw immediately, before callbacks run. The [contract tests](../../tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) cover failure behavior and structural comparisons.

## Aggregate harness

```csharp
CommandHandlerTestExtensions.ForAggregate<BankAccountAggregate>()
    .WithHandler<OpenAccountHandler>()
    .WithHandler<DepositFundsHandler>()
    .WithReducer<AccountOpenedReducer>()
    .WithReducer<FundsDepositedReducer>()
    .CreateScenario()
    .Given(new AccountOpened { HolderName = "Test", InitialDeposit = 100m })
    .When(new DepositFunds { Amount = 50m })
    .ThenEmits<FundsDeposited>(e => Assert.Equal(50m, e.Amount))
    .ThenState(s => Assert.Equal(150m, s.Balance));
```

## Aggregate scenario

Given a configured harness named `harness`:

```csharp
harness.CreateScenario()
    .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
    .When(new DepositFunds { Amount = 50m })
    .ThenEmits<FundsDeposited>(e => Assert.Equal(50m, e.Amount))
    .ThenState(s => Assert.Equal(150m, s.Balance));
```

## Projection scenario

Given a configured harness named `harness`:

```csharp
harness.CreateScenario()
    .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
    .When(new FundsDeposited { Amount = 50m })
    .ThenAssert(p => Assert.Equal(150m, p.Balance));
```

## Single reducer

Given a reducer, its input state and event, and the expected output:

```csharp
// Quick apply and assert
var result = reducer.Apply(initialState, eventData);
Assert.Equal(expected, result.Balance);
// Or use ShouldProduce for expected output assertions
reducer.ShouldProduce(initialState, eventData, expectedProjection);
```

## Projection harness

```csharp
// Create a harness with all reducers for a projection
var harness = ReducerTestExtensions.ForProjection<BankAccountBalanceProjection>()
    .WithReducer<AccountOpenedBalanceReducer>()
    .WithReducer<FundsDepositedBalanceReducer>()
    .WithReducer<FundsWithdrawnBalanceReducer>();
// Run a scenario with Given/When/Then
harness.CreateScenario()
    .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
    .When(new FundsDeposited { Amount = 50m })
    .ThenAssert(p => Assert.Equal(150m, p.Balance));
```
