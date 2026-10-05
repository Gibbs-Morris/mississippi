# Concepts

## Rules

- This reference **MUST** be applied only when the page is classified as `concepts`. Why: Concepts pages are explanation surfaces, not task guides.
- Concept pages **MUST** use the structure: direct explanation statement, `## The problem this solves`, `## Core idea`, `## How it works`, `## Guarantees`, `## Non-guarantees` or `## Limits`, `## Trade-offs`, and `## Related tasks and reference`. Why: Readers need a predictable explanation structure.
- Concept pages **MUST** explain ordering, concurrency, durability, visibility of state changes, failure boundaries, cancellation behavior, and versioning implications when relevant. Why: Runtime concepts often depend on distributed-systems semantics.
- Comparisons **MUST** be evidence-based and **MUST NOT** imply equivalence to another system without proof. Why: Similarity is not identity.
- Concept pages **MUST NOT** become procedural task guides, reference dumps, release notes, or marketing pages. Why: Explanation pages should remain focused on understanding.

## Default structure

1. direct explanation statement
2. `## The problem this solves`
3. `## Core idea`
4. `## How it works`
5. `## Guarantees`
6. `## Non-guarantees` or `## Limits`
7. `## Trade-offs`
8. `## Related tasks and reference`
