---
title: Architecture Decision Records
sidebar_label: Overview
description: Index of all Architecture Decision Records (ADRs) for the Mississippi framework, following the MADR 4.0.0 template.
sidebar_position: 0
slug: /adr
id: adr-overview
---

# Architecture Decision Records

## Overview

This section contains the Architecture Decision Records (ADRs) for the Mississippi framework. Each ADR captures a significant architectural choice — the context that motivated it, the alternatives considered, and the consequences of the decision.

## What Is an ADR?

An ADR is a short, structured document that records a single architectural decision. We use the [MADR 4.0.0](https://adr.github.io/madr/) (Markdown Architectural Decision Records) template because it balances rigour with low friction.

## When to Write an ADR

Write an ADR when a decision:

- **Affects structure** — changes component boundaries, data flow, or deployment topology
- **Is hard to reverse** — carries significant migration cost if changed later
- **Crosses boundaries** — affects multiple components or teams
- **Involves trade-offs** — multiple viable alternatives exist with different consequences
- **Sets a precedent** — establishes a pattern that future decisions will follow

## ADR Lifecycle

| Status | Meaning |
|--------|---------|
| **Proposed** | Under discussion; the ADR is in a PR awaiting review |
| **Accepted** | Approved and in effect |
| **Deprecated** | No longer relevant (e.g., feature removed); kept for history |
| **Superseded** | Replaced by a newer ADR; includes a link to the replacement |

Accepted ADRs are **immutable** — if a decision changes, a new ADR supersedes the original.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [MADR 4.0.0 specification](https://adr.github.io/madr/) - Review the source template and section requirements
- [Architecture Decision Records key principles](https://github.com/Gibbs-Morris/mississippi/blob/main/docs/key-principles/architecture-decision-records.md) - See the framework's long-lived ADR rationale and usage guidance
