---
id: command-history
title: Client Command History
description: Reference command lifecycle state, history retention, in-flight IDs, and outcome fields.
sidebar_position: 9
sidebar_label: Client Command History
---

# Client Command History

## Overview

`AggregateCommandStateBase` records command lifecycle actions in Reservoir. Its in-flight set, history list, and latest outcome fields answer different questions and can contain different information.

## Applies To

- `Mississippi.Inlet.Client.Abstractions.State.AggregateCommandStateBase`
- `IAggregateCommandState` and `AggregateCommandStateReducers`
- `CommandHistoryEntry` and `CommandStatus`
- Generated aggregate-command and saga-start state/reducers, which share this base and these lifecycle helpers

## State And Entry Defaults

The [base state](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/AggregateCommandStateBase.cs) starts with an empty immutable history list and in-flight ID set. `ErrorCode`, `ErrorMessage`, and `LastCommandSucceeded` are null. `IsExecuting` is true whenever the in-flight set is nonempty.

Each [history entry](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/Commands/CommandHistoryEntry.cs) holds the command ID/type, status, start/completion timestamps, and error code/message. `CreateExecuting` sets status to `Executing` and leaves completion/error fields null.

`ToFailed` copies an entry with status `Failed`, a completion timestamp, and supplied errors. `ToSucceeded` copies it with status `Succeeded` and a completion timestamp; it preserves other fields, including any earlier entry errors. These methods do not enforce transition order. Timestamps come from their arguments.

## Lifecycle Reducers

The [reducer helpers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs) reject null state or action. Their `ComputeCommand...` methods return updated in-flight/history collections; their `ReduceCommand...` methods copy a derived base state and also update its outcome fields:

- Executing adds the ID to the set and appends a new executing entry. The state reducer clears top-level errors and sets `LastCommandSucceeded` to null.
- Failed removes the ID and changes the first history entry with that ID to failed. The state reducer assigns the action's errors and sets `LastCommandSucceeded` to false.
- Succeeded removes the ID and changes the first matching history entry to succeeded. The state reducer clears top-level errors and sets `LastCommandSucceeded` to true.

These transitions describe individual reductions. The built-in store does not serialize concurrent `Dispatch` calls: overlapping effect completions can reduce the same prior state, and the later assignment can lose another completion's history update or ID removal. Reliable lifecycle tracking requires serialization of all dispatches to the shared store, including effect-result dispatches.

Completion does not create a missing history entry. Starting the same ID more than once adds multiple history entries but only one set member; completion updates only the first matching entry.

The latest outcome flag follows the last reduced lifecycle action. It is independent of `IsExecuting`, so another command can remain in flight after a completion sets the flag to true or false.

Mapping, JSON processing, or another ordinary effect failure after an executing action can end enumeration without a terminal action. The [root action effect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/RootActionEffect.cs) swallows non-critical iterator failures; an entry and its in-flight ID can remain executing until an explicit terminal action or state reset.

## History Retention

Starting a command enforces a FIFO history cap, defaulting to [`IAggregateCommandState.DefaultMaxHistoryEntries`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/IAggregateCommandState.cs), which is 200. Excess entries are removed from the beginning of the list, in insertion order. Completion does not reorder the list or enforce the cap again.

Only direct callers of `ComputeCommandExecuting` or `ReduceCommandExecuting` can supply a different `maxHistoryEntries`. The [generated command reducers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientReducersGenerator.cs) and [saga reducers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/SagaClientReducersGenerator.cs) always use 200. A constructed or restored oversized list remains oversized until another executing reduction trims it.

Retention does not protect executing entries or remove their IDs from the in-flight set. A later completion can therefore remove an ID without finding a retained entry to update. A zero cap retains no history; a negative cap is not validated and causes the underlying range removal to fail.

This is in-memory feature state in Reservoir's scoped store. A client reload or new scope starts from registered initial state, and resetting to the normal initial state clears its history. Persistence and rehydration belong to the application; the retained list is not a durable audit log.

The [state tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/AggregateCommandStateBaseTests.cs), [history-entry tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/CommandHistoryEntryTests.cs), and [reducer tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/AggregateCommandStateReducersTests.cs) cover defaults, entry factories, lifecycle updates, error/outcome fields, and the configured history cap.

## Summary

Use the in-flight set for recorded unfinished commands, history for retained observations, and top-level fields for the latest lifecycle outcome. Executing reductions trim history independently of active IDs; neither retention nor an in-flight ID establishes durable or ongoing server work.

## Next Steps

- Read [Inlet Reference](./reference.md) for generated client feature registrations.
- Read [Reservoir State Flow](../../reservoir/concepts/state-flow.md) for action-driven state updates.
