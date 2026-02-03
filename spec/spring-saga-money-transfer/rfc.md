# RFC: Spring Saga Money Transfer

## Problem

The Spring sample does not yet demonstrate a saga-driven money transfer between accounts, which is necessary to surface orchestration challenges and validate the saga design in realistic usage.

## Goals

- Add a Spring sample saga that transfers money from one bank account to another using the saga runtime.
- Expose the saga in the Spring sample UI and server to illustrate end-to-end flow.
- Achieve 100% unit test coverage for all code under src on this branch, including new saga framework code and any touched existing code.

## Non-Goals

- Introduce production-grade payment processing or external integrations.
- Modify core data storage providers beyond what the sample already uses.
- Add new non-saga features unrelated to transfer orchestration.

## Current State

- Spring.Domain has BankAccount aggregate commands for deposit/withdraw/open with generated endpoints. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/BankAccount/)
- There is no Spring sample saga demonstrating multi-account transfers. (VERIFIED: grep GenerateSagaEndpoints in samples/)
- StartSagaCommand includes Input, but StartSagaCommandHandler only emits SagaStartedEvent and does not persist input. (VERIFIED: src/EventSourcing.Sagas.Abstractions/StartSagaCommand.cs, src/EventSourcing.Sagas/StartSagaCommandHandler.cs)
- Unit test coverage for all src projects is not verified at 100%. (UNVERIFIED)

## Proposed Design

- Define a saga input command type (e.g., StartMoneyTransferCommand) in Spring.Domain and use it as the saga InputType so schema-first generation remains consistent with aggregates/projections.
- Persist saga input by emitting a dedicated saga input event from StartSagaCommandHandler and applying it via a saga reducer (so steps can read input from state).
- Add a saga state type in Spring.Domain that orchestrates a transfer: debit source account, credit destination account, and compensate on failure.
- Use saga step attributes and saga orchestration effect to drive the transfer steps.
- Add server/silo registrations for saga endpoints in Spring sample and register the generated saga feature in the client.
- Add Spring.Client UI flow to trigger the saga and show status (using generated saga client actions/state).
- Extend L0 tests for Spring.Domain and saga framework code to target 100% coverage for new src changes.

## Alternatives Considered

- Implement transfer as a single aggregate command without saga orchestration (rejected: does not validate saga design).
- Implement a custom orchestration grain outside the saga framework (rejected: bypasses the new saga runtime).

## Security

- Ensure commands validate account ownership and amounts; rely on existing aggregate validation rules. (UNVERIFIED)

## Observability

- Use saga orchestration logging via LoggerExtensions for transfer progress and failure. (UNVERIFIED)

## Compatibility / Migrations

- No breaking changes expected; new events and saga types are additive. (UNVERIFIED)

## Risks

- Saga input persistence introduces a public API/contract change to saga events/handlers.
- Saga sample may require UI/UX updates and coordination across Client/Server/Silo.
