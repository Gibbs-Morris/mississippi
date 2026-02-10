# RFC: Spring Sample Registrations

## Problem
Spring samples should expose a single registration entrypoint per SDK type so consumers do not need
to manually enumerate or understand Inlet-generated registrations.

## Goals
- Provide Spring sample registration entrypoints for client/server/silo SDK types.
- Hide registration details behind a single AddSpringDomain-style method per SDK type.
- Ensure registrations include everything required by Inlet source generation.
- Mark registration classes with the pending source gen attribute.
- Keep the pattern scalable across additional sample domains.

## Non-Goals
- Change runtime behavior beyond registration wiring.
- Introduce new public feature sets outside Spring samples.

## Current State
UNVERIFIED: Registration logic is split across sample project startup code and manual aggregate listing.
UNVERIFIED: No single AddSpringDomain-style registration entrypoints exist for all SDK types.

## Proposed Design
UNVERIFIED: Add three registration classes (client/server/silo) in Spring sample domain projects.
Each class exposes an AddSpringDomain extension method for its SDK type and is tagged with the
pending source gen attribute so downstream generators can discover and compose registrations.

## Alternatives
- Keep manual registration in sample startup code (rejected: not scalable).
- Create one monolithic registration type for all SDKs (rejected: unclear separation by SDK type).

## Security
No new authn/authz changes expected.

## Observability
No new logging/metrics required beyond existing registrations.

## Compatibility
Sample-only; no breaking changes to Mississippi framework APIs expected.

## Risks
- Missing a generated registration could lead to runtime failures.
- Source generator attribute usage may differ between SDKs.
