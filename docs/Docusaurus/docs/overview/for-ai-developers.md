---
id: for-ai-developers
title: Mississippi for AI-Assisted Development
sidebar_label: For AI Developers
sidebar_position: 7
description: Opinionated patterns and source generators that amplify AI productivity.
---

# Mississippi for AI-Assisted Development

Opinionated patterns. Predictable structure. Source generators that amplify AI productivity.

:::caution Early Alpha
Mississippi is in early alpha. APIs may change without notice.
:::

## Overview

AI-assisted development works best with consistent patterns. Mississippi prescribes clear roles for commands, events, reducers, and effects. Source generators handle boilerplate. AI models can learn the patterns and generate domain logic, tests, and UI automatically.

## Why Mississippi Amplifies AI Productivity

### Opinionated, Consistent Patterns

The Spring sample uses a consistent aggregate structure ([Spring.Domain Aggregates](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Domain/Aggregates)).

```text
Aggregates/
  BankAccount/
    BankAccountAggregate.cs       # State record
    Commands/
      DepositFunds.cs             # Command record
      WithdrawFunds.cs
    Events/
      FundsDeposited.cs           # Event record
      FundsWithdrawn.cs
    Handlers/
      DepositFundsHandler.cs      # Command handler
      WithdrawFundsHandler.cs
    Reducers/
      FundsDepositedReducer.cs    # State reducer
      FundsWithdrawnReducer.cs
    Effects/
      HighValueTransactionEffect.cs  # Side effect
```

AI models can recognise this structure and generate the same set of files in new domains.

### Source Generator Synergy

Defining a command:

```csharp
[GenerateCommand(Route = "deposit")]
[GenerateSerializer]
public sealed record DepositFunds
{
    [Id(0)] public decimal Amount { get; init; }
}
```

Automatically yields (per [`GenerateCommandAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs)):

| Generated Artifact | Purpose |
|-------------------|---------|
| `{Command}Action` | Client-side action class |
| `{Command}RequestDto` | Client request DTO |
| `{Command}ActionMapper` | Maps action to request DTO |
| `{Command}Effect` | HTTP dispatch effect |
| `{Command}Dto` | Server request DTO |
| `{Command}DtoMapper` | Maps DTO to domain command |
| Controller action | Server-side endpoint |

AI models can focus on domain logic. The framework generates the wiring.

### Pure, Testable Reducers

Reducers return new projection instances for each event (validated by [`RootReducer`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers/RootReducer.cs)):

```csharp
public BankAccountAggregate Reduce(
    FundsDeposited @event, 
    BankAccountAggregate state)
{
    return (state ?? new()) with
    {
      Balance = (state?.Balance ?? 0) + @event.Amount,
      DepositCount = (state?.DepositCount ?? 0) + 1,
    };
}
```

No side effects. Easy to verify. AI models can generate reducers and test cases reliably.

### Fluent Test Harness

[`AggregateTestHarness`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Testing/Aggregates/AggregateTestHarness.cs) enables Given/When/Then specifications:

```csharp
ForAggregate<BankAccountAggregate>()
    .WithHandler<DepositFundsHandler>()
    .WithReducer<FundsDepositedReducer>()
    .CreateScenario()
    .Given(new AccountOpened { HolderName = "Test", InitialDeposit = 100m })
    .When(new DepositFunds { Amount = 50m })
    .ThenEmits<FundsDeposited>(e => e.Amount.Should().Be(50m))
    .ThenState(s => s.Balance.Should().Be(150m));
```

AI models can generate test scenarios. The harness provides immediate feedback on correctness.

### Extensible Side Effects

Effects separate domain logic from infrastructure:

```csharp
internal sealed class HighValueTransactionEffect 
    : SimpleEventEffectBase<FundsDeposited, BankAccountAggregate>
{
    protected override async Task HandleSimpleAsync(
        FundsDeposited eventData,
        BankAccountAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken)
    {
        // External service call, AI scoring, etc.
    }
}
```

Plug in external services (email, payments) or AI-based logic (credit scoring, fraud detection) without modifying aggregates.

## AI Developer Benefits

| Benefit | How Mississippi Delivers |
|---------|--------------------------|
| **Predictable Structure** | Spring sample aggregates follow a consistent folder pattern ([Spring.Domain Aggregates](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Domain/Aggregates)) |
| **Reduced Boilerplate** | Source generators produce controllers, actions, and effects ([GenerateCommandAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs), [GenerateAggregateEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs)) |
| **Pure Functions** | Reducers are easy to generate and verify |
| **Testable Specifications** | Given/When/Then harness for immediate feedback |
| **Extensibility** | Effects pattern for external/AI service integration |

## Example: AI-Generated Domain

Given requirements:

> "Create a library book checkout system with check-out, return, and overdue fine commands"

AI generates:

```text
Aggregates/
  LibraryBook/
    LibraryBookAggregate.cs
    Commands/
      CheckOutBook.cs
      ReturnBook.cs
      ApplyOverdueFine.cs
    Events/
      BookCheckedOut.cs
      BookReturned.cs
      OverdueFineApplied.cs
    Handlers/
      CheckOutBookHandler.cs
      ReturnBookHandler.cs
      ApplyOverdueFineHandler.cs
    Reducers/
      BookCheckedOutReducer.cs
      BookReturnedReducer.cs
      OverdueFineAppliedReducer.cs
```

Source generators produce the API and client code. The AI focuses on business rules.

## Summary

Mississippi's opinionated patterns make AI-assisted development predictable. Source generators reduce human toil. Pure reducers are easy to verify. The test harness provides immediate feedback. Effects enable AI service integration without polluting domain logic.

## Next Steps

- [How It Works](./how-it-works.md) - Technical architecture
- [For Startups](./for-startups.md) - Rapid development patterns
- [Overview](./index.md) - Return to the main landing page
