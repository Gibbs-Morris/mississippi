---
applyTo: 'docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md'
---

# Architecture decisions

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root, Markdown, and applicable shared public-doc evidence/metadata/validation remain effective. The cs ADR Keeper remains primary author in Clean Squad. Inspect MADR4.0.0 before changing the portable body template and consult foundational architecture-decision-records principles.

## Rules (RFC 2119)

### ADR lifecycle

Scope: numbered `docs/Docusaurus/docs/adr/NNNN-*.md` files (four digits); contributors creating/modifying ADRs and ADR-threshold decisions, including role-driven work.

- ADR1: ADRs **MUST** live in docs/Docusaurus/docs/adr/ and follow the existing author-architecture-decision MADR4.0.0 body template plus local frontmatter.
- ADR1.2: ADR filenames **MUST** be zero-padded sequential NNNN-lowercase-dashed-title.md.
- ADR2: Branch-local ADR numbers **MUST** be provisional.
- ADR2.2: Before merge, the ADR author **MUST** rebase latest main, assign a contiguous block after main's highest ADR, and update filenames/ADR-NNNN titles/sidebar_position/relative links to avoid parallel conflicts.
- ADR3: Frontmatter **MUST** include title/description/sidebar_position matching NNNN/status/date.
- ADR3.2: Frontmatter decision_makers/consulted/informed **SHOULD** appear when applicable.
- ADR4: Body **MUST** contain Context and Problem Statement, Considered Options, Decision Outcome with Chosen option: sentence.
- ADR4.2: Decision Drivers/Consequences/Confirmation/Pros and Cons of the Options/More Information **SHOULD** appear when useful.
- ADR4.3: Optional MADR sections **MAY** be omitted for simple decisions.
- ADR5: New/substantively revised mutable/superseding ADRs **SHOULD** use Mermaid for materially clearer multi-step/component relationships.
- ADR5.2: Trivial/metadata/link ADR edits **SHOULD NOT** require new diagram work.
- ADR5.3: ADR prose **MUST** stay authoritative.
- ADR5.4: ADR diagrams **MUST** align with authoritative prose.
- ADR5.5: ADR diagrams **SHOULD** sit directly under their clarified section.
- ADR5.6: Omitting a diagram when the normal trigger applies **SHOULD** have a short rationale.
- ADR5.7: Authors **SHOULD** prefer sequenceDiagram(time), flowchart(process/decisions), simple architecture/C4(structure).
- ADR6: Accepted Context/Decision Outcome **MUST NOT** change.
- ADR6.2: Changed accepted decisions **MUST** create a new ADR and set original status to `superseded by [ADR-NNNN](NNNN-title.md)`.
- ADR6.3: ADR status **MUST** be proposed/accepted/deprecated/or that supersession link.
- ADR6.4: ADR cross-references **MUST** be relative Markdown.
- ADR7: ADRs **SHOULD** accompany or precede decisions, avoiding retrospective lost context.
- ADR7.2: ADRs **SHOULD** record only structural/hard-to-reverse/cross-component/significant-tradeoff/precedent decisions, avoiding trivial clutter.
- ADR8: For drafting/revising/superseding/verification contributors **MUST** use `.agents/skills/author-architecture-decision/SKILL.md` and `.agents/skills/author-architecture-decision/assets/madr-body.md`; contributors **MUST** read both directly when discovery is unavailable. Local Rules apply even if the skill is absent/unselected.
- ADR9: Authors **MUST** retain former-template bindings: frontmatter title is quoted YAML `"ADR-NNNN: <Decision title>"`; H1 is `ADR-NNNN: <Decision title>`; sidebar_position is numeric NNNN; date YYYY-MM-DD; initial status proposed, accepted only after approval.

## References

- <https://adr.github.io/madr/> (MADR4.0.0); docs/key-principles/architecture-decision-records.md
- Unchanged .agents/skills/author-architecture-decision/SKILL.md and .agents/skills/author-architecture-decision/assets/madr-body.md; unchanged .github/agents/cs-adr-keeper.agent.md
- Parent public-doc policy is applicable for shared evidence, metadata, validation; product-page layouts do not override MADR
- [Documentation authoring](../../../../.github/instructions/documentation-authoring.instructions.md) remains applicable for shared evidence, metadata, and validation.
