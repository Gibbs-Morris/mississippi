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

This section contains the Architecture Decision Records (ADRs) for the Mississippi framework. Each ADR captures a significant architectural choice, including the context that motivated it, the alternatives considered, and the consequences of the decision.

The published corpus currently contains a mixed set of legacy sequential ADRs and new-model ADRs. New-model ADRs are merge-safe under parallel pull requests: they use canonical frontmatter identity, human-readable timestamped filenames, deterministic ordering metadata, and final-at-merge lifecycle values.

## What Is an ADR?

An ADR is a short, structured document that records a single architectural decision. We use the [MADR 4.0.0](https://adr.github.io/madr/) (Markdown Architectural Decision Records) template because it balances rigour with low friction.

## Mississippi Publication Contract

This page describes the target publication contract for new Mississippi ADRs.
Repository-wide validation, full mixed-corpus exemplar coverage, and other
enforcement surfaces are follow-on rollout work, so authors and reviewers must
currently apply these rules deliberately rather than assuming CI already proves
every invariant.

- Canonical identity lives in frontmatter `id`, not in the filename.
- New ADR filenames follow `YYYY-MM-DD-title-slug--HHmmssSSS[-NN].md`.
- New ADRs carry immutable `created_at_utc` metadata, and `sidebar_position` is derived from that timestamp plus the disambiguator.
- New ADRs merged to `main` use final `status` values only: `accepted`, `rejected`, or `deprecated`.
- Supersession is represented with reciprocal `supersedes` and `superseded_by` metadata, not by changing an older ADR body.
- Legacy ADRs use `legacy_refs` when governance backfill needs to preserve historical `ADR-NNNN` aliases.
- Human prose references should use linked `ADR YYYY-MM-DD: Title` text.

## Contributor Happy Path

1. Create a new ADR using the timestamped filename pattern.
2. Choose an immutable `created_at_utc`, then derive `id` and `sidebar_position` from it.
3. Set the final merged `status` before review closes.
4. Use relative links plus linked `ADR YYYY-MM-DD: Title` prose references.
5. If the ADR supersedes a legacy record, backfill only the minimum governance metadata in the same change.

## Worked Example

Use [ADR 2026-03-25: Redesign ADR Governance Publication Model](./2026-03-25-redesign-adr-governance-publication-model--215831956.md) as the concrete new-model example.

Given this input:

- `created_at_utc: 2026-03-25T21:58:31.956Z`
- disambiguator: `00`

Derive these outputs:

- filename: `2026-03-25-redesign-adr-governance-publication-model--215831956.md`
- `id`: `adr-20260325T215831956Z-00`
- `sidebar_position`: `177447591195600`

If you are authoring manually instead of using a future scaffold, copy that
pattern exactly: choose the immutable UTC timestamp first, then derive the
filename, `id`, and ordering metadata from it.

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
| **Accepted** | Approved and in effect |
| **Rejected** | Intentionally declined and preserved for future context |
| **Deprecated** | Historical but no longer the recommended active decision |

New-model ADRs do not publish `proposed` status to `main`; the decision outcome is final at merge time. If a decision changes later, a new ADR supersedes the earlier one, and the older ADR keeps its original final status while gaining reciprocal supersession metadata.

Use these distinctions when choosing between `deprecated` and supersession:

- Keep the older ADR `accepted` and add `superseded_by` when a newer ADR directly replaces that accepted decision.
- Use `deprecated` when the ADR itself remains published historical guidance that is no longer the recommended active choice and that change is not being expressed only through a direct superseding replacement.

## Referencing and Supersession

Use relative Markdown links when referencing other ADRs. In prose, prefer linked `ADR YYYY-MM-DD: Title` text instead of sequential-number shorthand.

When an ADR supersedes another one:

- The new ADR adds a `supersedes` entry with the target ADR `id` and relative `path`.
- The older ADR gains the matching `superseded_by` entry as the only routine post-merge metadata change.
- Historical `ADR-NNNN` references may remain inside untouched legacy ADRs.

## Governance Reference

- [ADR 2026-03-25: Redesign ADR Governance Publication Model](./2026-03-25-redesign-adr-governance-publication-model--215831956.md)

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [MADR 4.0.0 specification](https://adr.github.io/madr/) - Review the source template and section requirements
- [Architecture Decision Records key principles](https://github.com/Gibbs-Morris/mississippi/blob/main/docs/key-principles/architecture-decision-records.md) - See the framework's long-lived ADR rationale and usage guidance
