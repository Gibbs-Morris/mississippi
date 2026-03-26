---
applyTo: 'docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md,docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-*--*.md'
---

# Architecture Decision Records (MADR)

Governing thought: ADRs use the MADR 4.0.0 template, live in `docs/Docusaurus/docs/adr/` for Docusaurus publishing, and merge as final records whose canonical identity comes from immutable frontmatter while published routes stay human-readable through explicit slugs.

> Drift check: Review the MADR 4.0.0 specification at <https://adr.github.io/madr/> before modifying the template; check `docs/key-principles/architecture-decision-records.md` for foundational thinking.

## Rules (RFC 2119)

- ADRs **MUST** live in `docs/Docusaurus/docs/adr/` and follow the MADR 4.0.0 template defined in this file. Why: Single published location ensures discoverability via the Docusaurus site.
- New ADR filenames **MUST** follow the pattern `YYYY-MM-DD-title-slug--HHmmssSSS[-NN].md`, using UTC date and time values that match `created_at_utc`; untouched legacy ADRs **MAY** retain their existing sequential filenames until a deliberate backfill touches them. Why: Human-readable timestamped filenames stay merge-safe under parallel PRs without making the filename the canonical identity.
- New ADR frontmatter **MUST** include `id`, `title`, `description`, `slug`, `sidebar_position`, `status`, `date`, `created_at_utc`, `supersedes`, and `superseded_by`; `decision_makers`, `consulted`, and `informed` fields **SHOULD** be included when applicable; backfilled legacy ADRs **SHOULD** add `legacy_refs` when preserving historical aliases. Why: The new governance model separates canonical identity, published route, ordering, lifecycle, and supersession into explicit metadata.
- New ADR `id` values **MUST** use the format `adr-YYYYMMDDTHHmmssSSSZ-NN`, remain immutable after merge, and act as the only canonical machine-readable identity; filenames and titles **MUST NOT** be treated as canonical identifiers. Why: Canonical frontmatter identity survives rename pressure and branch concurrency.
- New ADR `slug` values **MUST** use the pattern `/adr/YYYY-MM-DD-title-slug--HHmmssSSS[-NN]`, derived directly from the filename stem and treated as immutable after merge unless an intentional route migration is being performed. Why: Docusaurus otherwise falls back to opaque `id`-based routes for docs that define an explicit frontmatter `id`.
- New ADR `sidebar_position` values **MUST** be derived from `created_at_utc` with the repository ordering formula `unixEpochMilliseconds(created_at_utc) * 100 + disambiguator`; handwritten drift **MUST NOT** be treated as authoritative. Why: Docusaurus still needs a numeric sort key, but deterministic ordering must derive from immutable UTC metadata rather than sequence allocation.
- Authors creating new ADRs **MUST** choose the final merged `status` before review closes and **MUST NOT** plan a follow-up status-only edit after merge. Why: The publication model requires ADRs to be final at merge time rather than mutating later just to express outcome.
- The required MADR sections **MUST** be: `Context and Problem Statement`, `Considered Options`, and `Decision Outcome` (with `Chosen option:` sentence). Why: These three sections are the MADR 4.0.0 mandatory minimum.
- Optional MADR sections (`Decision Drivers`, `Consequences`, `Confirmation`, `Pros and Cons of the Options`, `More Information`) **SHOULD** be included when they add value and **MAY** be omitted for simple decisions. Why: Keeps lightweight decisions lightweight while allowing rigour when needed.
- New ADRs, substantively revised mutable ADRs, and new superseding ADRs **SHOULD** include Mermaid when they explain a multi-step flow or a multi-component structural relationship that prose alone would make materially harder to understand; trivial, metadata-only, and link-only edits **SHOULD NOT** trigger new diagram work. Why: Diagram effort should track comprehension benefit, not create churn for small edits.
- When Mermaid is included in an ADR, the prose **MUST** remain authoritative, the diagram **MUST** stay aligned with that prose, and the diagram **SHOULD** appear directly under the section it clarifies. Why: Diagrams improve comprehension only when they support rather than compete with the written decision record.
- When an ADR meets the normal Mermaid trigger and omits a diagram, the author **SHOULD** include a short omission rationale near the relevant discussion or in a clearly labeled note. Why: This keeps omissions intentional and reviewable without forcing low-value diagrams.
- Authors **SHOULD** prefer `sequenceDiagram` for interactions over time, `flowchart` for process or decision flow, and simple architecture or C4-style Mermaid for structural relationships. Why: Default mappings reduce diagram-choice churn and improve reviewer consistency.
- Once an ADR is merged to `main` with any final `status` (for example `accepted`, `rejected`, or `deprecated`), its Context and Decision Outcome sections **MUST NOT** be modified; if a decision changes, a new ADR **MUST** be created, and the older ADR **MUST** keep its original final `status` while receiving reciprocal supersession metadata only. Why: Immutability preserves historical reasoning without overloading lifecycle text.
- New-model ADR `status` values **MUST** be one of `accepted`, `rejected`, or `deprecated`; `proposed` **MUST NOT** appear on `main`, and `superseded` **MUST NOT** be represented as a status value. Why: Merged ADRs are final at publication time, while supersession is an explicit relationship rather than a lifecycle state.
- Supersession relationships **MUST** use reciprocal `supersedes` and `superseded_by` metadata entries containing both `id` and relative `path`; authors **MUST NOT** encode supersession solely in free-form status text. Why: Explicit graph metadata is machine-checkable and keeps older ADR bodies immutable.
- Non-empty `supersedes` and `superseded_by` lists **MUST** encode each entry as an object with `id` and relative `path` properties, not as plain strings. For example:

  ```yaml
  supersedes:
    - id: adr-20250101T120000000Z-01
      path: 2025-01-01-old-decision--120000000-01.md
  superseded_by:
    - id: adr-20250202T090000000Z-01
      path: 2025-02-02-new-decision--090000000-01.md
  ```
- When a new ADR supersedes an untouched legacy ADR, authors **MUST** perform the minimum governance metadata backfill in the same change and **MUST NOT** edit the legacy ADR body. Why: Mixed-corpus supersession must remain reviewer-auditable and bounded to metadata-only cutover work.
- Legacy governance backfill **MUST** stay within this allow-list: canonical governance frontmatter, `legacy_refs`, reciprocal `supersedes` and `superseded_by` metadata, and derived ordering metadata explicitly required by the contract. Why: Day-one migration is intentionally narrow so governance rollout does not become historical ADR rewriting.
- ADR cross-references **MUST** use relative Markdown links. Why: Keeps links valid across environments.
- New prose references to ADRs **SHOULD** use linked `ADR YYYY-MM-DD: Title` text; historical `ADR-NNNN` references **MAY** remain inside untouched legacy artifacts. Why: Human references should remain readable while the canonical machine identity lives in metadata.
- ADRs **SHOULD** be written during or before the decision, not after. Why: Writing forces clarity; retrospective ADRs lose context.
- Only decisions meeting the ADR threshold **SHOULD** be recorded: affects structure, hard to reverse, crosses component boundaries, involves significant trade-offs, or sets a precedent. Why: Prevents trivial decisions from cluttering the log.

## Scope and Audience

All contributors creating or modifying ADRs; the cs ADR Keeper agent is the primary author within the Clean Squad workflow.

## At-a-Glance Quick-Start

- Create: copy the template below to `docs/Docusaurus/docs/adr/YYYY-MM-DD-title-slug--HHmmssSSS.md`
- Set immutable `id` and `created_at_utc` values once; do not allocate or renumber a repository sequence number
- Derive `slug` from the filename stem so the published route stays human-readable and does not default to the canonical `id`
- Derive `sidebar_position` from `created_at_utc` and the disambiguator instead of treating it as a hand-maintained counter
- Fill in required sections: Context and Problem Statement, Considered Options, Decision Outcome
- Add Mermaid when the ADR documents both a multi-step flow or multi-component structural relationship and a relationship that would be materially harder to understand from prose alone; otherwise Mermaid remains optional
- If a qualifying ADR intentionally omits Mermaid, include a short omission rationale
- Set the final merged status before review closes: `accepted`, `rejected`, or `deprecated`
- If the ADR supersedes an older ADR, add reciprocal `supersedes` and `superseded_by` metadata using relative paths
- If the target ADR is an untouched legacy record, backfill only the minimum governance metadata in the same change

## Contributor Happy Path

1. Create a new ADR from the template below using the timestamped filename pattern.
2. Choose an immutable `created_at_utc`, then derive `id`, `slug`, and `sidebar_position` from that value.
3. Set the final merged `status` before review closes: `accepted`, `rejected`, or `deprecated`.
4. Use linked `ADR YYYY-MM-DD: Title` prose references and keep canonical `id` usage in metadata and relationships.
5. If the ADR supersedes another decision, add reciprocal `supersedes` and `superseded_by` entries with relative paths.
6. If the superseded ADR is an untouched legacy record, add only the minimum governance metadata backfill in the same change.

## Decision Aid

- Use `accepted` when the repository adopts the decision.
- Use `rejected` when the repository intentionally records a declined option.
- Use `deprecated` when the ADR remains historical but is no longer the recommended active decision and is not being represented only through direct supersession.

## Legacy Backfill Boundary

For governance cutover work on legacy ADRs, only these metadata changes are in scope:

- canonical governance frontmatter required by the new model
- `legacy_refs` preserving historical `ADR-NNNN` aliases
- reciprocal `supersedes` and `superseded_by` entries
- derived ordering metadata explicitly required by the new contract

## Qualifying Test

A qualifying ADR normally has both of these properties:

1. It documents a multi-step flow or multi-component structural relationship.
2. That relationship would be materially harder to understand from prose alone.

If either property is absent, Mermaid remains optional.

## MADR 4.0.0 Template (Mississippi)

````markdown
---
id: adr-20260325T153045123Z-00
title: "ADR 2026-03-25: Title of Decision"
description: One-sentence summary of the decision
slug: /adr/2026-03-25-title-of-decision--153045123
sidebar_position: 174291664512300
status: accepted
date: 2026-03-25
created_at_utc: 2026-03-25T15:30:45.123Z
supersedes: []
superseded_by: []
decision_makers:
  - Name or role
consulted:
  - Name or role
informed:
  - Name or role
---

# ADR 2026-03-25: Title of Decision

## Context and Problem Statement

Describe the context and problem statement in two to three sentences.
Articulate the problem as a question when possible.

When the ADR meets the qualifying Mermaid test above, place the diagram directly under the section it clarifies and keep the prose authoritative.

## Decision Drivers

- Driver 1 (e.g., performance requirement)
- Driver 2 (e.g., team expertise)

## Considered Options

- Option 1
- Option 2
- Option 3

## Decision Outcome

Chosen option: "Option N", because [justification].

### Consequences

- Good, because [positive consequence]
- Bad, because [negative consequence]

### Confirmation

Describe how compliance with this ADR will be confirmed (e.g., code review, architecture test, CI check).

## Pros and Cons of the Options

### Option 1

Description or pointer to more information.

- Good, because [argument]
- Neutral, because [argument]
- Bad, because [argument]

### Option 2

Description or pointer to more information.

- Good, because [argument]
- Bad, because [argument]

## More Information

Links to related ADRs, RFCs, design documents, or external references.

If the ADR would normally benefit from Mermaid but intentionally omits one, add a brief omission rationale near the relevant discussion or in this section.
````

## Core Principles

- MADR 4.0.0 is the standard; keep the mandatory minimum low, optional sections available.
- Immutability preserves historical reasoning; supersede, never edit.
- Published in Docusaurus so the whole team can find and read decisions.
- Canonical frontmatter identity keeps merge safety and cross-reference stability separate from human-readable filenames and routes.
- Mermaid should raise clarity for complex flows and structures, not become decorative ceremony.

## References

- MADR 4.0.0: <https://adr.github.io/madr/>
- Key principles: `docs/key-principles/architecture-decision-records.md`
- ADR Keeper agent: `.github/agents/cs-adr-keeper.agent.md`
- Documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
