---
title: "ADR-0006: Use Configurable Container Initialization for Blob Snapshot Storage"
description: Use a hosted initializer with configurable create-or-validate behavior so the Blob provider supports both Cosmos-like ergonomics and least-privilege deployments.
sidebar_position: 6
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Cloud architecture reviewer
  - Three Amigos synthesis
informed:
  - Tributary maintainers
  - Platform operators
---

# ADR-0006: Use Configurable Container Initialization for Blob Snapshot Storage

## Context and Problem Statement

The Blob provider wants a setup experience that feels close to the existing Cosmos provider, but production environments often pre-provision containers through infrastructure-as-code and deny applications permission to create them. The architecture therefore needs one startup contract that works for both developer ergonomics and least-privilege production hosting without pushing container checks into the first write path.

## Decision Drivers

- Registration should stay close to the existing Cosmos provider experience.
- Startup should fail fast on missing configuration or missing containers when the host expects infrastructure to be pre-provisioned.
- Production environments may forbid runtime container creation.
- Initialization behavior should be explicit instead of hidden behind first-write side effects.

## Considered Options

- Use a hosted initializer with configurable `CreateIfMissing` and `ValidateExists` modes.
- Always create the container if it is missing.
- Never create the container and require external provisioning in all environments.

## Decision Outcome

Chosen option: "Use a hosted initializer with configurable `CreateIfMissing` and `ValidateExists` modes", because it preserves a friendly default for local and simple deployments while still supporting least-privilege environments that want startup validation without runtime creation.

### Consequences

- Good, because development and simple deployments can adopt the provider with low friction.
- Good, because locked-down environments can fail fast when expected infrastructure is absent.
- Good, because container validation happens during startup instead of surfacing later on an application write path.
- Bad, because the provider exposes one more deployment-time option that teams need to choose deliberately.
- Bad, because startup behavior now depends on both configuration and Azure permissions.

### Confirmation

Compliance will be confirmed by tests and code review that prove `CreateIfMissing` creates missing containers when permissions allow it, `ValidateExists` fails startup when the container is absent, and invalid keyed-client or container configuration is rejected before runtime storage operations begin.

## Pros and Cons of the Options

### Use a hosted initializer with configurable `CreateIfMissing` and `ValidateExists` modes

Validate Blob configuration at startup and either create or verify the target container according to explicit configuration.

- Good, because it balances developer ergonomics with least-privilege production expectations.
- Good, because startup is the point where infrastructure mismatches are discovered.
- Neutral, because the provider still mirrors the Cosmos adoption model closely.
- Bad, because there is more configuration surface than a single hard-coded mode.

### Always create the container if it is missing

Treat container creation as the provider's unconditional startup behavior.

- Good, because the default experience is simple.
- Bad, because the approach does not work in environments that deny create permissions.
- Bad, because it assumes infrastructure ownership that some teams deliberately reserve for deployment tooling.

### Never create the container and require external provisioning in all environments

Make validation-only behavior the sole mode.

- Good, because permissions stay minimal and explicit.
- Good, because infrastructure ownership is unambiguous.
- Bad, because local development and simple adoption become less convenient than the existing Cosmos-like experience.

## More Information

- [Architecture Decision Records](index.md)
