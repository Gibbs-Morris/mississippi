---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Documentation Page Focus

Governing thought: Every Mississippi documentation page serves one reader intent, and that intent governs both its placement and its structure.

> Drift check: The canonical page-type model lives in `docs/Docusaurus/docs/contributing/documentation-guide.md`; keep this file aligned with that public guide.

## Rules (RFC 2119)

- Authors **MUST** classify each non-navigation page as exactly one of `getting-started`, `tutorials`, `how-to`, `concepts`, `reference`, `operations`, `troubleshooting`, `migration`, `release-notes`, or `decision-record` before writing. Authored category entry pages are navigation artifacts and **MUST** add durable orientation. Why: Structure and evidence requirements depend on the page type, while empty index pages add noise.
- Pages **MUST NOT** blend tutorial, how-to, concept, and reference content into one undifferentiated page. Why: Mixed intent makes navigation and maintenance worse.
- Each page **MUST** answer one primary question and **MUST** state that answer or scope directly in its opening. Why: Readers should know immediately whether the page matches their need.
- Page type **MUST** be treated as the writing contract and, for product documentation, the primary placement rule. Why: A reader should be able to predict content from both its location and its structure.
- Numbered files under `adr/` **MUST** use the repository's decision-record format instead of the generic documentation section template. Why: Architecture decisions need stable MADR semantics rather than artificial Summary and Next Steps sections.
- If a topic genuinely needs multiple page types, authors **MUST** split it into separate pages and cross-link them. Why: Readers should not dig through irrelevant sections to find the right material.
- Placement **MUST** follow the canonical information architecture, and the filename or title **SHOULD** make the page type clear. Why: The filesystem and the writing contract should reinforce each other without duplicating taxonomies.

## Scope and Audience

All contributors and agents writing or updating public docs under `docs/Docusaurus/docs/`.

## At-a-Glance Quick-Start

- Choose the page type first.
- Place the page in the matching reader-intent area.
- State the page outcome or scope in the opening.
- Keep one page for one reader intent.
- Split and cross-link when the topic spans multiple intents.

## Classification Questions

Ask these questions before writing:

- Is the reader trying to get to a first success?
- Is the reader trying to learn by following a guided sequence?
- Is the reader trying to complete a specific task quickly?
- Is the reader trying to understand a model, guarantee, or trade-off?
- Is the reader trying to look up exact facts?
- Is the reader trying to run Mississippi safely in production?
- Is the reader diagnosing a symptom?
- Is the reader upgrading between versions?
- Is the reader scanning a release summary?
- Is the reader recording a durable architecture decision and its consequences?

## Core Principles

- **One Intent**: One page should answer one main question.
- **Reader Job First**: Placement and page structure make the same promise.
- **Package Ownership Second**: Name and link the owning subsystem where it helps, but do not make it a competing tree.
- **Split Before Stuffing**: If the page wants to do multiple jobs, break it apart.

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-guide.md`
- Documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
- Information architecture: `.github/instructions/documentation-information-architecture.instructions.md`
