---
applyTo: 'docs/Docusaurus/docs/concepts/**/*.{md,mdx}'
---

# Concept Documentation

Governing thought: Concepts pages explain how Mississippi works, what it guarantees, and what trade-offs or limits the reader must understand.

> Drift check: Keep this file aligned with `docs/Docusaurus/docs/contributing/documentation-concepts.md`.

## Rules (RFC 2119)

- This file **MUST** govern content pages classified as `concepts`; authored category `index.md` pages **MAY** use the navigation-artifact contract instead. Why: Concepts pages are explanation surfaces, while a category entry has a distinct orientation job.
- Concept pages **MUST** use the structure: direct explanation statement, `## The problem this solves`, `## Core idea`, `## How it works`, `## Guarantees`, `## Non-guarantees` or `## Limits`, `## Trade-offs`, and `## Related tasks and reference`. Why: Readers need a predictable explanation structure.
- Concept pages **MUST** explain ordering, concurrency, durability, visibility of state changes, failure boundaries, cancellation behavior, and versioning implications when relevant. Why: Mississippi concepts often depend on distributed-systems semantics.
- Comparisons **MUST** be evidence-based and **MUST NOT** imply equivalence to Orleans or another system without proof. Why: Similarity is not identity.
- Concept pages **MUST NOT** become procedural task guides, reference dumps, release notes, or marketing pages. Why: Explanation pages should remain focused on understanding.

## Scope and Audience

Contributors and agents authoring concept pages for Mississippi documentation.

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-concepts.md`
- General authoring: `.github/instructions/documentation-authoring.instructions.md`
