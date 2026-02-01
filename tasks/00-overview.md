# Transfer Funds Saga Feature - Task Overview

## Goal

Build a complete money transfer saga feature in Spring sample that demonstrates:

1. **Saga Pattern** - Atomic transfer between two accounts with compensation on failure
2. **Real-time UX** - Watch account balances update live during transfer (10s delay between steps)
3. **Full Source Generation** - Same developer experience as aggregates (dispatch action → everything flows)
4. **Saga Observability** - Track saga status, see progress, handle failures visually

## Architecture

```text
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Blazor Client  │────▶│   ASP.NET API   │────▶│   Orleans Silo  │
│                 │     │                 │     │                 │
│ Dispatch Action │     │ SagaController  │     │ SagaOrchestrator│
│ Watch Balances  │◀────│ (generated)     │◀────│ Steps/Comps     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                                               │
        │         SignalR Projection Updates            │
        └───────────────────────────────────────────────┘
```

## Task Breakdown

| Task | Description | Est. Hours |
|------|-------------|------------|
| [01-saga-abstractions](01-saga-abstractions.md) | Client abstractions for saga actions | 2h |
| [02-server-generators](02-server-generators.md) | SagaControllerGenerator + SagaServerDtoGenerator | 4h |
| [03-client-generators](03-client-generators.md) | Full client-side saga generator suite | 6h |
| [04-generator-tests](04-generator-tests.md) | L0 tests for all new generators | 4h |
| [05-domain-saga](05-domain-saga.md) | TransferFunds saga in Spring.Domain | 3h |
| [06-saga-status-projection](06-saga-status-projection.md) | Client subscription to saga status | 2h |
| [07-transfer-page](07-transfer-page.md) | Transfer funds UI page | 2h |
| [08-account-watch-page](08-account-watch-page.md) | Multi-account balance watch dashboard | 2h |
| [09-delay-effect](09-delay-effect.md) | 10s delay between saga steps for demo | 1h |
| [10-integration-testing](10-integration-testing.md) | L2 tests for end-to-end saga flow | 3h |

### Total Estimated: ~29 hours

## Success Criteria

1. ✅ User can enter source account, destination account, and amount
2. ✅ Clicking "Transfer" dispatches a generated action (no manual HTTP)
3. ✅ Account Watch page shows both account balances updating in real-time
4. ✅ 10-second visible delay between debit and credit for demo effect
5. ✅ If destination account is closed/invalid, source is refunded (compensation)
6. ✅ Saga status visible during execution (Running, Step 1/2, Completed/Failed)
7. ✅ All infrastructure is source-generated (same DX as aggregates)

## Dependencies

- Existing BankAccountAggregate with Deposit/Withdraw commands
- Existing BankAccountBalanceProjection with SignalR updates
- Existing saga infrastructure (SagaOrchestrator, Steps, Compensations)
- Existing Inlet client/server infrastructure

## Out of Scope

- Saga cancellation mid-flight (future enhancement)
- Saga retry UI (future enhancement)
- Saga audit log/history view (future enhancement)
