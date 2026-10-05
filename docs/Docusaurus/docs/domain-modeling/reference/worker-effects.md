---
id: worker-effects
title: Worker Event Effects
description: Reference background effect envelopes, worker routing, supplied state, and failure observation.
sidebar_position: 5
sidebar_label: Worker Effects
---

# Worker Event Effects

## Overview

Background event effects receive an envelope through an Orleans worker grain. The aggregate dispatches the handoff without waiting for the effect's business operation to finish.

## Applies To

- `Mississippi.DomainModeling.Abstractions.FireAndForgetEffectEnvelope<TEvent, TAggregate>`
- `Mississippi.DomainModeling.Abstractions.IFireAndForgetEffectWorkerGrain<TEvent, TAggregate>`
- The built-in aggregate registration and worker implementation

## Envelope

The [sealed record](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/FireAndForgetEffectEnvelope.cs) has five init-only properties:

| Property | Meaning and default |
|----------|---------------------|
| `EventData` | The triggering event; defaults to null |
| `AggregateState` | The supplied aggregate state; defaults to null |
| `BrookKey` | Full brook identity; defaults to an empty string |
| `EventPosition` | The triggering event's position; defaults to `0` |
| `EffectTypeName` | CLR effect identity for worker resolution; defaults to an empty string |

The record itself does not validate those values. The public worker grain contract accepts envelopes from Orleans callers. Its built-in handler does not verify that the event was persisted, that the state belongs to the supplied entity, or that the caller is authorized. This is a trusted runtime handoff; applications exposing command entry points need their gateway authorization and appropriate access controls at the Orleans client boundary. The [registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/FireAndForgetEffectRegistration.cs) supplies event, state, key, and position from its arguments without making a local deep copy. It derives `EffectTypeName` from `typeof(TEffect).FullName`, falling back to the simple name; callers do not supply that string to `Dispatch`.

The envelope crosses an Orleans grain boundary. Its `[GenerateSerializer]` attribute does not generate codecs for arbitrary event or aggregate payload types; each concrete payload and its object graph need compatible [Orleans serialization support](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization). Missing support can fail copying or serialization before worker execution.

## Aggregate Handoff

The [aggregate runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/GenericAggregateGrain.cs) dispatches registrations whose `EventType` equals the original event's exact runtime type. It supplies each event's brook position from the original persisted batch. Follow-up events yielded and persisted by awaited effects are processed by that awaited-effect loop, but are not added to worker dispatch; a worker registration for their event type is not invoked by this path. Each [`AddFireAndForgetEventEffect<TEvent, TAggregate, TEffect>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/AggregateRegistrations.cs) call adds registrations without deduplication. Repeating the same registration creates another matching dispatch for every original event, so register each intended effect once to avoid duplicate handoffs.

It loads the aggregate snapshot at the last known position after the awaited-effect phase returns, then uses that state for all original events in the batch. The aggregate catches and logs noncritical dispatch failures from that phase before proceeding, so the state can include only the follow-up events persisted before the failure. The iteration limit can also end that phase with further follow-up effects undispatched; worker handoff does not certify a fully successful cascade. The envelope therefore does not necessarily contain the historical state immediately after its individual event. Treat supplied state as read-only observation. The worker does not persist direct mutations of that object; durable aggregate changes go through the command API. The Orleans boundary also does not replace the concrete payload's copying/serialization contract described above.

The registration routes to a worker keyed by the aggregate type's full CLR name, falling back to its simple name. That routing key is not the entity ID. It discards the task returned by `ExecuteAsync` and supplies the default cancellation token. That token cannot be canceled, and the handoff supplies no operation deadline. An external operation that never completes can keep that worker execution occupied indefinitely; effect implementations need their own timeout or cancellation policy for such work. The [worker contract](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/IFireAndForgetEffectWorkerGrain.cs) marks this call `[OneWay]`.

Orleans [one-way delivery](https://learn.microsoft.com/en-us/dotnet/orleans/grains/oneway) provides no receipt, failure, or completion signal and can lose a message before the worker receives it. [Stateless workers](https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains) can have several activations, so the routing key does not guarantee execution or completion order for one entity.

A committed append can fail cursor publication before this handoff, and loading the post-event snapshot can fail or be canceled after commitment. Those paths can leave committed events without worker dispatch; the built-in handoff does not automatically replay them. See [Brook Append Outcomes](../../reference/brook-append-outcomes.md).

The [registration tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/FireAndForgetEffectRegistrationTests.cs) verify the routing key, envelope arguments, default token, and omission of unmatched event types.

## Worker Resolution And Failures

The [stateless worker](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/FireAndForgetEffectWorkerGrain.cs) resolves registered `IFireAndForgetEventEffect<TEvent, TAggregate>` implementations and selects the first whose CLR `FullName` exactly matches `EffectTypeName`. With the default Microsoft DI container, resolving this enumerable constructs the registered implementations before that selection. A constructor or dependency failure in another registered effect can prevent an otherwise healthy requested effect from running; the worker catches ordinary resolution failures and logs/records them under the requested effect name.

Renaming an effect or moving it to another namespace changes this identity. During a rolling upgrade, an envelope with an old name can reach a worker that logs a missing effect and returns. Orleans serialization aliases do not translate this application-level string. `FullName` omits the effect type's assembly identity. Effect implementations used for this event/aggregate pair need distinct CLR full names across loaded assemblies; two implementations with the same full name are indistinguishable to this lookup, which selects the first match.

A missing implementation, null event, or null aggregate state is logged and recorded as a failure metric, then execution returns. Ordinary exceptions from resolution or handling, including cancellation exceptions, are logged and swallowed by the worker. `OutOfMemoryException`, `StackOverflowException`, and `ThreadInterruptedException` propagate. A null envelope is rejected before that protected handling block.

The [existing worker tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/FireAndForgetEffectWorkerGrainTests.cs) cover valid execution, a missing event, and an ordinary effect exception.

A noncritical synchronous exception from registration dispatch, including worker-grain reference lookup or call setup, is caught by the aggregate and logged. It records neither a successful dispatch metric nor a worker failure metric, and does not by itself fail the command or trigger retry/replay. Worker logs and metrics apply after the worker receives the call. Worker instruments use the exact meter name `Mississippi.DomainModeling.Runtime.FireAndForgetEffects`. Subscribe to that name through OpenTelemetry `AddMeter`, or use a matching wildcard. The [Spring runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Runtime/Program.cs) subscribes to `Mississippi.DomainModeling.Runtime` only; [meter matching](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/customizing-the-sdk/README.md) does not treat that exact parent name as a wildcard for these worker instruments.

The built-in handoff does not provide a durable retry queue or application-level idempotency mechanism. Aggregate command completion does not establish that the worker's external side effect completed.

## Summary

Worker effects receive an event, supplied state, and routing metadata after persistence. Their execution is outside the aggregate's awaited completion path. Noncritical exceptions inside registration dispatch are log-only; post-append cursor publication or snapshot-load failures can still propagate before that dispatch catch. Received worker failures use the worker's logs and metrics.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for effect ownership.
- Read [Spring Key Concepts](../../samples/spring-sample/concepts/key-concepts.md) for the sample's background effect role.
