---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Documentation Page Focus

Governing thought: Every Mississippi documentation page should serve one reader intent and one page type, even while the repository is still migrating from older feature-oriented layouts.

> Drift check: The canonical page-type model lives in `docs/Docusaurus/docs/contributing/documentation-guide.md`; keep this file aligned with that public guide.

## Rules (RFC 2119)

- Authors **MUST** classify each page as exactly one of `getting-started`, `tutorials`, `how-to`, `concepts`, `reference`, `operations`, `troubleshooting`, `migration`, or `release-notes` before writing. Why: Structure and evidence requirements depend on the page type.
- Pages **MUST NOT** blend tutorial, how-to, concept, and reference content into one undifferentiated page. Why: Mixed intent makes navigation and maintenance worse.
- Each page **MUST** answer one primary question and **MUST** state that answer or scope directly in its opening. Why: Readers should know immediately whether the page matches their need.
- Page type **MUST** be treated as the writing contract even when physical folder layout still reflects an older feature-oriented structure. Why: Mississippi is in a hybrid transition.
- If a topic genuinely needs multiple page types, authors **MUST** split it into separate pages and cross-link them. Why: Readers should not dig through irrelevant sections to find the right material.
- Placement within the docs tree **SHOULD** reinforce the page type when practical, but physical placement **MUST NOT** override the page-type contract. Why: Folder layout is migrating incrementally.

## Scope and Audience

All contributors and agents writing or updating public docs under `docs/Docusaurus/docs/`.

## Core Principles

- **One Intent**: One page should answer one main question.
- **Page Type Before Placement**: Content contract matters more than folder history.
- **Split Before Stuffing**: If the page wants to do multiple jobs, break it apart.

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-guide.md`
- Documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
- Feature documentation structure: `.github/instructions/feature-documentation-structure.instructions.md`
- Page-contract selection: [Author technical documentation](../../.agents/skills/author-technical-documentation/SKILL.md#select-the-page-contract).
