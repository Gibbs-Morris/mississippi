---
id: concepts-design-goals-and-trade-offs
title: Design Goals and Trade-Offs
sidebar_label: Design Goals and Trade-Offs
sidebar_position: 6
description: Explain what Mississippi optimizes for, how source generation and test harnesses reduce ceremony, and what constraints that introduces.
---

# Design Goals and Trade-Offs

## Overview

Mississippi is intentionally opinionated — and that is a feature, not a limitation.

The framework is designed to eliminate the ceremony that surrounds event-sourced, Orleans-based applications while keeping domain logic fully in the developer's hands. Conventions, source generators, generic runtime infrastructure, and dedicated test harnesses work together so teams spend their time on business problems instead of wiring infrastructure.

## The Problem This Solves

Teams building this style of system often repeat the same non-domain work:

- registering handlers, reducers, projections, and endpoints
- creating request and response DTOs that mirror domain types
- keeping gateway routes and client actions aligned with server behavior
- rebuilding Given/When/Then test setup for every aggregate and effect

Testing is a large part of that cost. In a typical Orleans application, validating domain behavior often means spinning up a `TestCluster`, mocking grain infrastructure, or mixing business-rule assertions with transport and hosting setup. Mississippi tries to separate those concerns so domain correctness can be exercised without bringing Orleans runtime concerns into every test.

The repository evidence shows Mississippi eliminating exactly that kind of repeated scaffolding.

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

   That matters because the framework does not force aggregate and effect tests to boot Orleans just to prove domain behavior. The harnesses let tests express prior events, execute a command or effect, and assert on emitted events, failures, and resulting state using the same domain concepts the runtime uses. Reducer and projection harnesses extend the same idea to read-side behavior, so the testing story stays aligned with the production model instead of requiring a separate testing architecture.

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

Mississippi is well positioned for AI-assisted development because it narrows how much infrastructure an engineer or coding agent must recreate by hand. The generators and test harnesses define a smaller, more regular surface area:

- domain records and handlers follow repeatable conventions
- gateway and client scaffolding are generated from attributes instead of manually mirrored
- tests can focus on business rules with lightweight harnesses instead of full-host setup

That does not make the framework "AI-native" in a magical sense. It means the framework chooses explicit patterns that are easier for both humans and tools to extend consistently.

## Related Tasks and Reference

- Use [Architectural Model](./architectural-model.md) for the full subsystem picture.
- Use [Write Model](./write-model.md) and [Read Models and Client Sync](./read-models-and-client-sync.md) for runtime behavior.
- Use [Samples](../samples/index.md) when you want to see the generated and handwritten pieces together in one application.

## Summary

Mississippi trades architectural freedom for development leverage: explicit patterns, generated infrastructure, and lightweight domain testing let teams ship faster without sacrificing correctness.

## Next Steps

- [Architectural Model](./architectural-model.md)
- [Write Model](./write-model.md)
- [Read Models and Client Sync](./read-models-and-client-sync.md)
- [Samples](../samples/index.md)
