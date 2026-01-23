---
sidebar_position: 2
title: Aggregates
description: High-level overview of command handling and event production for Mississippi event sourcing.
---

# Aggregates

Aggregates are the command-processing core of Mississippi event sourcing. They validate commands against current state and emit domain events that become the source of truth.

## Purpose

- Execute commands against an aggregate root.
- Produce domain events for persistence and downstream projections.
- Support optimistic concurrency by checking expected versions.

## Where it fits

Aggregates sit upstream of brooks. They translate commands into events that are appended to event streams and later reduced into projections.

## Source code reference

- [IGenericAggregateGrain](../../../../src/EventSourcing.Aggregates.Abstractions/IGenericAggregateGrain.cs)
- [IRootCommandHandler](../../../../src/EventSourcing.Aggregates.Abstractions/IRootCommandHandler.cs)
- [GenerateAggregateEndpointsAttribute](../../../../src/Sdk.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs) — marks an aggregate for endpoint code generation
- [GenerateCommandAttribute](../../../../src/Sdk.Generators.Abstractions/GenerateCommandAttribute.cs) — marks a command for endpoint code generation
