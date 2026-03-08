---
id: concepts-design-goals-and-trade-offs
title: Design Goals and Trade-Offs
sidebar_label: Design Goals and Trade-Offs
sidebar_position: 6
description: Explain what Mississippi optimizes for, how source generation and test harnesses reduce ceremony, and what constraints that introduces.
---

# Design Goals and Trade-Offs

## Overview

Mississippi is intentionally opinionated.

It is designed to reduce the ceremony around event-sourced, Orleans-based applications without removing the need to write real C# domain logic. The framework does that through conventions, generators, generic runtime infrastructure, and test harnesses.

## The Problem This Solves

Teams building this style of system often repeat the same non-domain work:

- registering handlers, reducers, projections, and endpoints
- creating request and response DTOs that mirror domain types
- keeping gateway routes and client actions aligned with server behavior
- rebuilding Given/When/Then test setup for every aggregate and effect

The repository evidence shows Mississippi trying to reduce exactly that kind of repeated scaffolding.

## Core Idea

Mississippi is low-ceremony, not no-code.

Developers still own the important logic:

- command validation
- event definitions
- reducer logic
- effect logic
- saga steps and compensation rules

The framework owns more of the repeatable infrastructure around that logic.

## How It Works

The verified ergonomics story is built from three mechanisms.

1. Attribute-driven generation.
   `GenerateAggregateEndpointsAttribute`, `GenerateProjectionEndpointsAttribute`, `GenerateSagaEndpointsAttribute`, and `GenerateCommandAttribute` each drive concrete runtime, gateway, or client output.
2. Generic runtime building blocks.
   `GenericAggregateGrain<TAggregate>`, `UxProjectionGrain<TProjection>`, and the root dispatcher types mean many applications do not need one handwritten grain or controller per domain concept.
3. Focused test harnesses.
   `AggregateTestHarness<TAggregate>`, `AggregateScenario<TAggregate>`, and `EffectTestHarness<...>` support Given/When/Then style testing around domain logic without full infrastructure setup.

## Guarantees

- Mississippi provides real source generators for aggregate, projection, saga, gateway, and client scaffolding. This is not just a design aspiration.
- The repository includes dedicated test harnesses for aggregate scenarios, reducers, projections, and both synchronous and fire-and-forget effects.
- The current client model is explicitly Redux-style. `IStore`, Reservoir reducers, and Inlet projection actions all use that vocabulary directly.
- The repository is explicitly pre-1.0.

## Non-Guarantees

- Mississippi does not remove the need to understand distributed systems. Teams still need to reason about eventual consistency, side effects, retries, and compensation.
- Mississippi does not promise maximum architectural freedom. It expects developers to work with its conventions rather than around them.
- Mississippi does not guarantee that every concept page in the public docs is already as complete as the codebase. Some public pages are still being rebuilt.

## Trade-Offs

- Strong conventions make generator output more reliable, but they also make naming, namespace layout, and attribute placement matter more.
- Generated surfaces reduce repetitive code, but they can hide infrastructure from developers who have not yet learned the model.
- Domain tests become simpler when handlers, reducers, and effects stay narrow. That same narrowness means business rules are split across several small types instead of one large service.

## Why This Model Fits AI-Assisted Engineering

This section is a reasoned interpretation of the repository shape, not a runtime guarantee.

Mississippi appears well suited to AI-assisted development because it narrows how much infrastructure an engineer or coding agent must recreate by hand. The generators and test harnesses define a smaller, more regular surface area:

- domain records and handlers follow repeatable conventions
- gateway and client scaffolding are generated from attributes instead of manually mirrored
- tests can focus on business rules with lightweight harnesses instead of full-host setup

That does not make the framework "AI-native" in a magical sense. It means the framework chooses explicit patterns that are easier for both humans and tools to extend consistently.

## Related Tasks and Reference

- Use [Architectural Model](./architectural-model.md) for the full subsystem picture.
- Use [Write Model](./write-model.md) and [Read Models and Client Sync](./read-models-and-client-sync.md) for runtime behavior.
- Use [Samples](../samples/index.md) when you want to see the generated and handwritten pieces together in one application.

## Summary

Mississippi optimizes for leverage through explicit patterns, reusable runtime infrastructure, generation, and lightweight domain testing.

## Next Steps

- [Architectural Model](./architectural-model.md)
- [Write Model](./write-model.md)
- [Read Models and Client Sync](./read-models-and-client-sync.md)
- [Samples](../samples/index.md)
