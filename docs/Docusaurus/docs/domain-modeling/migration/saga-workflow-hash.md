---
id: domain-modeling-saga-workflow-hash-migration
title: Migrate Saga Workflow Hashes (Legacy Delimiter Format to Length-Prefixed Format)
sidebar_label: Saga Workflow Hash Migration
sidebar_position: 1
description: Finish sagas with legacy workflow hashes before deploying the guarded length-prefixed hash format.
---

# Migrate Saga Workflow Hashes (Legacy Delimiter Format to Length-Prefixed Format)

This guide covers the persisted `StepHash` change from the delimiter-only encoding present at main revision `f5bb7c0c2bd843deb27483e001a3350e106e83cd` to the guarded length-prefixed encoding. These are source and hash-format scopes, not package release versions.

## Who should read this

Read this before deploying the guarded saga runtime to a store containing sagas started by the legacy runtime.

## Compatibility summary

The new runtime compares each pending saga's stored hash with the current registered workflow before a forward or compensation step. An unchanged workflow started by the legacy runtime still has a different stored hash. Mixed old and new runtimes against active saga state are not a verified deployment mode.

The change does not rename saga events or the `StepHash` property. It changes the value written to that property. There is no automatic conversion of stored hashes or resume from the terminal mismatch event.

## Breaking changes

The legacy encoding joined metadata with delimiters. The guarded encoding uses length-prefixed fields, invariant numbers, strict UTF-8, Orleans type names, and stable defining assembly identities. Assembly version is excluded. Custom step names with delimiters no longer collide with other step sequences.

If a saga with a legacy hash reaches a guarded execution boundary, the runtime emits terminal `SagaFailed` with `SAGA_STEP_HASH_MISMATCH` before resolving a step.

## Required preparation

1. Inventory non-terminal sagas in each store used by the deployment. Include paused workflows and compensation in progress.
2. Keep the legacy runtime available until those sagas finish. Retain the existing workflow definitions during this drain.
3. Back up the event store and deployment configuration through the provider's normal procedure before the cutover. This guide does not define a hash rewrite or event migration.

## Upgrade sequence

1. Stop accepting new saga starts on the legacy runtime.
2. Let existing sagas reach a terminal state under that runtime, including any required domain-safe retries or operator handling outside this hash change.
3. Confirm there are no non-terminal legacy sagas before deploying the guarded runtime to all hosts that execute those sagas.
4. Resume new starts only after the guarded runtime is active.

## Code and configuration changes

The public `StartSagaCommandHandler<TSaga, TInput>` constructor changes its CLR signature:

- Before: `(ISagaStepInfoProvider<TSaga>, TimeProvider)`.
- After: `(ISagaStepInfoProvider<TSaga>, TimeProvider, ILogger<StartSagaCommandHandler<TSaga, TInput>>? logger = null)`.

Existing two-argument source calls still compile because the logger argument is optional. Rebuild assemblies compiled against the old two-parameter constructor; the old binary signature is no longer present. Reflection callers must look up the three-parameter constructor and supply a third argument (`null` is accepted). No configuration key changes in this slice.

Review registered step order, names, types, and compensation declarations before deployment; these fields now determine the persisted workflow identity. Method bodies and external configuration remain outside the hash check.

## Data, state, and serialization implications

`StepHash` remains a string in the saga-start event and saga state. The new string represents a different encoding of workflow metadata. Existing event history is not rewritten. A terminal mismatch cannot be automatically resumed by installing the old runtime again because the failure event is already durable.

## Validation

Check that the old runtime has no non-terminal saga records in the target store before cutover. After deploying the new runtime, start a new saga and verify its normal step progression. Test a changed workflow against a controlled non-production active saga and verify one terminal `SAGA_STEP_HASH_MISMATCH` without step resolution or invocation. Inspect saga history and the workflow-change log for the result. A plain mismatch has no hashing exception; an encoding failure logs its original exception.

## Rollback

Rollback before new guarded sagas start is a deployment rollback to the legacy runtime, provided no guarded hash or terminal mismatch has been stored. Once the guarded runtime has started sagas, the legacy runtime can continue them without checking their hashes, so rollback would remove drift protection from active work. Once the guarded runtime has written a terminal mismatch, restoring the old binary does not undo that event. Preserve the store backup and treat recovery of those cases as a separate data and domain decision, not an automatic rollback.

## Related release notes and reference

- [Sagas and Orchestration](../../concepts/sagas-and-orchestration.md) explains the workflow guard and its limits.
- [Domain Modeling Reference](../reference/reference.md) identifies the runtime and abstraction packages.
