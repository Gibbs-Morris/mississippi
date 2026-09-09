---
applyTo: 'docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md'
---

# Architecture Decision Records (MADR)

Governing thought: ADRs use the MADR 4.0.0 template, live in `docs/Docusaurus/docs/adr/` for Docusaurus publishing, and are immutable once accepted.

> Drift check: Review the MADR 4.0.0 specification at <https://adr.github.io/madr/> before modifying the [skill's MADR body template](../../.agents/skills/author-architecture-decision/assets/madr-body.md); check `docs/key-principles/architecture-decision-records.md` for foundational thinking.

## Rules (RFC 2119)

- ADRs **MUST** live in `docs/Docusaurus/docs/adr/` and follow the MADR 4.0.0 body template in the [ADR authoring skill](../../.agents/skills/author-architecture-decision/SKILL.md), adding the frontmatter required below. Why: Single published location ensures discoverability via the Docusaurus site.
- ADR filenames **MUST** follow the pattern `NNNN-title-with-dashes.md` where `NNNN` is a zero-padded sequential number and the title uses lowercase dashes. Why: MADR convention; sequential numbers provide stable cross-references.
- When a feature branch adds ADRs, their branch-local numbers **MUST** be treated as provisional until merge preparation; before merging, the author **MUST** rebase onto the latest `main`, renumber new ADRs as a contiguous block after the highest ADR on `main`, and update filenames, `ADR-NNNN` titles, `sidebar_position`, and relative ADR links. Why: Parallel PRs can race on sequential numbering and create filename/sidebar conflicts.
- ADR frontmatter **MUST** include `title`, `description`, `sidebar_position` (matching the `NNNN` number), `status`, and `date`; `decision_makers`, `consulted`, and `informed` fields **SHOULD** be included when applicable. Why: Combines Docusaurus rendering requirements with MADR metadata.
- The required MADR sections **MUST** be: `Context and Problem Statement`, `Considered Options`, and `Decision Outcome` (with `Chosen option:` sentence). Why: These three sections are the MADR 4.0.0 mandatory minimum.
- Optional MADR sections (`Decision Drivers`, `Consequences`, `Confirmation`, `Pros and Cons of the Options`, `More Information`) **SHOULD** be included when they add value and **MAY** be omitted for simple decisions. Why: Keeps lightweight decisions lightweight while allowing rigour when needed.
- New ADRs, substantively revised mutable ADRs, and new superseding ADRs **SHOULD** include Mermaid when they explain a multi-step flow or a multi-component structural relationship that prose alone would make materially harder to understand; trivial, metadata-only, and link-only edits **SHOULD NOT** trigger new diagram work. Why: Diagram effort should track comprehension benefit, not create churn for small edits.
- When Mermaid is included in an ADR, the prose **MUST** remain authoritative, the diagram **MUST** stay aligned with that prose, and the diagram **SHOULD** appear directly under the section it clarifies. Why: Diagrams improve comprehension only when they support rather than compete with the written decision record.
- When an ADR meets the normal Mermaid trigger and omits a diagram, the author **SHOULD** include a short omission rationale near the relevant discussion or in a clearly labeled note. Why: This keeps omissions intentional and reviewable without forcing low-value diagrams.
- Authors **SHOULD** prefer `sequenceDiagram` for interactions over time, `flowchart` for process or decision flow, and simple architecture or C4-style Mermaid for structural relationships. Why: Default mappings reduce diagram-choice churn and improve reviewer consistency.
- Accepted ADRs **MUST NOT** have their Context or Decision Outcome sections modified; if a decision changes, a new ADR **MUST** be created with status `superseded by [ADR-NNNN](NNNN-title.md)` on the original. Why: Immutability preserves historical reasoning.
- Status values **MUST** be one of: `proposed`, `accepted`, `deprecated`, `superseded by [ADR-NNNN](NNNN-title.md)`. Why: Defined lifecycle with traceability.
- ADR cross-references **MUST** use relative Markdown links. Why: Keeps links valid across environments.
- ADRs **SHOULD** be written during or before the decision, not after. Why: Writing forces clarity; retrospective ADRs lose context.
- Only decisions meeting the ADR threshold **SHOULD** be recorded: affects structure, hard to reverse, crosses component boundaries, involves significant trade-offs, or sets a precedent. Why: Prevents trivial decisions from cluttering the log.

## Scope and Audience

All contributors creating or modifying ADRs; the cs ADR Keeper agent is the primary author within the Clean Squad workflow.

## Procedure and Template

Use the [author-architecture-decision skill](../../.agents/skills/author-architecture-decision/SKILL.md)
for drafting, revisions, superseding decisions, and verification. Its
[portable MADR body](../../.agents/skills/author-architecture-decision/assets/madr-body.md)
replaces the inline template; add the repository's required frontmatter and
apply all rules above before saving an ADR. Those rules apply even when the
skill is not selected or available. Read the linked files directly if automatic
skill discovery is unavailable.

Retain these local bindings from the former template when filling the body:

| Field                    | Local binding                                        |
| ------------------------ | ---------------------------------------------------- |
| Frontmatter title and H1 | `ADR-NNNN: <Decision title>`                         |
| `sidebar_position`       | Numeric value of `NNNN`, matching the ADR identifier |
| Date                     | `YYYY-MM-DD`                                         |
| Initial status           | `proposed`; change to `accepted` after approval      |

## References

- MADR 4.0.0: <https://adr.github.io/madr/>
- Key principles: `docs/key-principles/architecture-decision-records.md`
- ADR Keeper agent: `.github/agents/cs-adr-keeper.agent.md`
- Documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
