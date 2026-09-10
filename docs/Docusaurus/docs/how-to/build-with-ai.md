---
title: Build a Feature with an AI Assistant
description: Give an AI assistant explicit Mississippi business rules, source references, and verification criteria for one reviewable feature.
sidebar_position: 1
sidebar_label: Build with AI
---

# Build a Feature with an AI Assistant

## Overview

Build one feature by specifying its business rules, representing accepted changes as events, and verifying its state transitions before connecting the UI. Mississippi gives both you and an AI assistant named places for each part of that work: commands, handlers, events, reducers, projections, actions, and effects.

This workflow uses Spring's bank-account withdrawal as a concrete reference. The business benefit is a reviewable path from a requirement to observable behavior: a reviewer can inspect the rule, the accepted event, and the resulting state separately.

## When to use this

Use this procedure when you want an AI assistant to implement one business operation with explicit source references and acceptance checks.

## Before you begin

- Choose the capability and packages from the [capability map](../reference/capability-map.md).
- Have an application following the [Spring project boundaries](../samples/spring-sample/concepts/host-applications.md), or use Spring to learn the workflow.
- Give the assistant access to the source and tests for the framework version your application uses.
- Read the [write model](../concepts/write-model.md) so commands, events, and reducers have distinct responsibilities.

## Steps

### 1. Specify One Business Decision

Write the rule and examples before requesting code. For a withdrawal, identify the account, the requested amount, the conditions for acceptance, and the state the user should eventually see.

Spring's [WithdrawFundsHandler](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Handlers/WithdrawFundsHandler.cs) accepts a positive amount when the account is open and the balance is sufficient. The following cases translate those rules into observable outcomes; amounts are illustrative.

| Given | When | Expected outcome |
| --- | --- | --- |
| Open account with balance 100 | Withdraw 25 | One `FundsWithdrawn` event for 25; reduced balance 75 |
| Open account with balance 100 | Withdraw 100 | One `FundsWithdrawn` event for 100; reduced balance 0 |
| Open account with balance 100 | Withdraw 101 | Rejection with `InvalidCommand`; no withdrawal event |
| Open account with balance 100 | Withdraw 0 | Rejection with `InvalidCommand`; no withdrawal event |
| Closed account | Withdraw 25 | Rejection with `InvalidState`; no withdrawal event |

Use explicit outcomes such as these instead of a request like "make withdrawals work." They give the assistant a target that tests can check.

### 2. Assign Each Concern to Its Artifact

Keep the same vocabulary in the requirement, code, and tests.

| Concern | Artifact | Spring reference |
| --- | --- | --- |
| Requested business operation | Command | [WithdrawFunds](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Commands/WithdrawFunds.cs) |
| State needed to decide | Aggregate state | [BankAccountAggregate](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs) |
| Whether the request is valid | Command handler | [WithdrawFundsHandler](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Handlers/WithdrawFundsHandler.cs) |
| Accepted business fact | Event | [FundsWithdrawn](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Events/FundsWithdrawn.cs) |
| How the fact changes state | Event reducer | [FundsWithdrawnReducer](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Reducers/FundsWithdrawnReducer.cs) |
| Data the screen reads | UX projection | [BankAccountBalance](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance) |

The handler returns events or a failed operation result. The reducer applies a recorded fact to state. The runtime handles event persistence through the aggregate execution path. This separation lets you change how a screen displays a withdrawal while keeping the business decision in one place.

### 3. Provide a Bounded Implementation Brief

Give the assistant a brief that names the artifacts, references, and acceptance tests. The following is a prompt template; replace the bracketed values with your application's details.

```text
Implement [one business operation] using Mississippi [installed version].

Business rule:
[State required, accepted inputs, rejected inputs, and business outcome.]

Acceptance cases:
[Given state -> command -> event or rejection -> resulting state.]

Interface access:
[Authentication mechanism, permitted callers, and operation/entity permissions.]
[The application boundary that checks the requested entity ID.]

Use these references:
[Paths to the existing aggregate, handler, event, reducer, and tests.]
[Relevant Mississippi documentation and source for the installed version.]

Deliver:
- A named command expressing business intent.
- A handler that validates the request against aggregate state.
- Events representing accepted facts and pure reducers applying them.
- Tests for acceptance, rejection, boundaries, and state transitions.
- Generated integration through the supported Inlet attributes.
- Authentication and authorization for exposed APIs and subscriptions.
- Interface tests for anonymous callers, insufficient permissions, and denied entities.
- The projection and client changes needed to observe the result.

Before editing, identify the files and verification steps.
For each API or behavior, check the supplied source and existing tests.
Report executed checks and the behavior each check verifies.
```

Keep the task to one operation at a time. Review the resulting domain diff before asking for another feature. The template guides implementation; the acceptance tests provide evidence for whether the result is correct.

### 4. Keep Deterministic Transitions Explicit

A pure reducer computes the next state from the prior state and its event or action. Given the same inputs and reducer implementation, it computes the same result. Put externally obtained facts into the event or action before reduction, so replay uses the recorded inputs.

For example, Spring's withdrawal reducer subtracts the event amount and increments the withdrawal count. The reducer has everything needed to explain that transition. That makes it useful for debugging, testing, and asking an assistant to explain an unexpected balance.

Use these choices when reviewing generated suggestions:

| Prefer | Rework this suggestion | Reason |
| --- | --- | --- |
| `WithdrawFunds` as a named command | A generic "set account state" request | The handler can enforce the withdrawal's business rule |
| `FundsWithdrawn` as the accepted fact | Passing the command directly to an event reducer | The persisted history should represent accepted outcomes |
| A new state value computed from state and event | Reading the clock or calling an API in the reducer | Recorded inputs make state reconstruction repeatable |
| A focused projection for a screen | Reimplementing server balance rules in a component | The screen can consume the read model built from accepted events |
| An effect for external work | Sending notifications from a reducer | Effects give external work an explicit execution boundary |

The server reducer contract is [EventReducerBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/EventReducerBase.cs). Reservoir uses its own action-reduction contracts for local feature state; see [Reservoir reference](../reservoir/reference/reference.md).

### 5. Let Generators Connect the Domain

After verifying the business behavior and defining interface access, follow the existing application's attributed domain pattern. Inlet generates the supported transport and client artifacts from those inputs. For example, Spring's `WithdrawFunds` declares a command route and `BankAccountAggregate` opts into aggregate endpoints.

For a network-accessible gateway, configure authentication and authorization before exposing the generated transport. Select named application policies and put `[GenerateAuthorization]` metadata on each protected generated command, projection, and saga. Verify every generated route's effective authorization. Force mode adds a default controller filter only when the controller and all its actions lack explicit authorization metadata; a controller mixing protected and unannotated actions needs explicit coverage for the remaining actions. The default `GeneratedApiAuthorization.Mode` is `Disabled`; review the [authorization options](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/GeneratedApiAuthorizationOptions.cs) and [generation metadata](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAuthorizationAttribute.cs) explicitly.

Protect an HTTP MCP endpoint and its tools through a separate application authorization boundary, or restrict them to a trusted local development environment. Generated HTTP-controller and Inlet subscription policies apply to those interfaces; generated MCP tools invoke domain grains directly. Include MCP access checks when that transport is part of the application.

Treat permission to act on a particular entity as an application decision. Name the boundary that receives the entity ID and verifies access; subscription identity policies receive a null resource. Include that decision and its tests in the implementation brief.

Use the generated artifacts as part of the application's build. Keep the human-authored rule in the handler and the state transition in the reducer. When reviewing assistant changes, check the domain attributes and the resulting API/client behavior together.

Continue through [building an aggregate](../samples/spring-sample/tutorials/building-an-aggregate.md), [building projections](../samples/spring-sample/tutorials/building-projections.md), and [client composition](../inlet/how-to/how-to.md) for the concrete integration paths.

## Verify the result

Use more than successful compilation to judge completion.

1. Run handler tests covering the acceptance matrix, including rejection codes and emitted event data.
2. Run reducer tests checking the next state and preservation of the input state.
3. Verify generated registrations and compile the runtime, gateway, and client projects that consume the domain.
4. Exercise the command through the application and observe the projection and subscribed client state.
5. Check command progress and projection progress as separate observations. The [client synchronization model](../concepts/read-models-and-client-sync.md) delivers projection changes asynchronously.

6. For exposed interfaces, verify anonymous requests are rejected (`401`/`403` as appropriate), authenticated callers without permission are denied, and the application rejects access to unauthorized entities. Check allowed and denied projection subscriptions too. [Spring auth-proof mode](../samples/spring-sample/how-to/auth-proof-mode.md) supplies executable HTTP authorization cases with development identities. Add application-specific SignalR integration checks for allowed and denied subscriptions.
For the Spring example, run the domain tests from the repository root with PowerShell 7 and the .NET SDK selected by `global.json`. The canonical quality script builds the test project and its dependencies, executes the tests, and writes TRX and coverage evidence.

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject samples/Spring/Spring.Domain.L0Tests/Spring.Domain.L0Tests.csproj -SourceProject samples/Spring/Spring.Domain/Spring.Domain.csproj -SkipMutation
```

Require exit code 0, `RESULT: PASS`, a nonzero `TEST_TOTAL`, and matching `TEST_PASSED` and `TEST_TOTAL`. Inspect the emitted `test_results.trx` path for the individual handler and reducer results. `-SkipMutation` selects the ordinary test-and-coverage check.

Build the consuming sample projects with the canonical sample build entry point:

```powershell
pwsh ./build.ps1 -SkipMississippi -Configuration Release
```

This builds `samples.slnx`, including Spring's runtime, gateway, client, and their referenced projects. Require exit code 0 and `ALL REQUESTED BUILDS COMPLETED SUCCESSFULLY`, with zero build warnings and errors. The build checks generated integration; the tests above check business behavior.

For another Mississippi repository sample, substitute its actual test and source project paths in the quality command. In your own application, use your solution's build/test entry points and apply the same acceptance cases and nonempty test-result checks; these PowerShell scripts belong to the Mississippi repository.

Spring provides [withdrawal handler tests](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain.L0Tests/Aggregates/BankAccount/Handlers/WithdrawFundsHandlerTests.cs) and [withdrawal reducer tests](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain.L0Tests/Aggregates/BankAccount/Reducers/FundsWithdrawnAggregateReducerTests.cs) as concrete examples. Its [repository validation guide](https://github.com/Gibbs-Morris/mississippi/blob/main/README.md#validate-spring-after-a-change) explains the executable API and browser checks.

## Summary

Give an AI assistant explicit business intent, concrete source references, and observable acceptance cases. Use Mississippi's handlers and reducers to keep decisions and transitions reviewable, generators to connect the supported interfaces, and tests to verify the delivered behavior.

## Next Steps

- [Build an aggregate](../samples/spring-sample/tutorials/building-an-aggregate.md) to follow the domain implementation path.
- [Read models and client sync](../concepts/read-models-and-client-sync.md) to understand when the UI observes a change.
- [Capability and package map](../reference/capability-map.md) to choose the next feature area.
