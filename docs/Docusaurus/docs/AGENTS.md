---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx},docs/Docusaurus/docs/**/event-sourcing-*.md'
---

# Public documentation contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root and Markdown contracts remain effective. Public documentation policy follows the current docs/contributing guides and Docusaurus behavior. ADRs use their dedicated child policy/template rather than product-page layouts, while shared evidence/metadata/validation rules remain applicable.

## Rules (RFC 2119)

### Public documentation

Scope: docs/Docusaurus/docs/**/*.{md,mdx} and public-documentation authoring/review content. Classification is based on reader intent, not merely folder layout.

- DOC1: Authors **MUST** optimize correctness/clarity/navigation/maintenance, never marketing tone.
- DOC1.2: Authors **MUST NOT** invent APIs/config/defaults/guarantees/limits/exceptions/runtime.
- DOC1.3: Claims **MUST** have code/tests/verified samples/design/ADR/runtime evidence.
- DOC1.4: Unverifiable claims **MUST NOT** appear as fact.
- DOC1.5: Guaranteed/default/typical/implementation/unsupported/future behavior **MUST** be distinguished.
- DOC2: Each product page **MUST** answer one question and use exactly one type.
- DOC2.2: Before drafting/validating, authors/reviewers **MUST** read/follow selected author-technical-documentation skill contract and corresponding local guide from the table below.
- DOC3: Pages **MUST** state answer/scope in their opening and honor page-type contract over hybrid folder history.
- DOC3.2: Multiple intents **MUST** become cross-linked separate pages.
- DOC3.3: Placement **SHOULD** reinforce type.
- DOC3.4: Placement **MUST NOT** override page type.
- DOC4: Public frontmatter **MUST** include title/description/sidebar_position.
- DOC4.2: Optional sidebar_label/pagination_label/slug/tags/draft/id **MAY** support stability.
- DOC4.3: Authors **MUST** use .md unless genuinely needing MDX components.
- DOC5: Internal links **MUST** be relative Markdown.
- DOC5.2: Tabs **MUST** represent real parallel OS/language/hosting variants.
- DOC5.3: Admonitions **MUST** materially change behavior and retain internal blank lines.
- DOC6: Mermaid **SHOULD** replace screenshot diagrams.
- DOC6.2: Every diagram **MUST** have an introduction/main point.
- DOC6.3: Flowcharts over 4 nodes **MUST** use flowchart TB.
- DOC6.4: Flowchart LR **MAY** occur only at 4 or fewer nodes to avoid narrow-site overflow.
- DOC7: Runnable examples **MUST** derive from verified/newly verified samples or executable test/build evidence.
- DOC7.2: Prerequisites **MUST** be explicit.
- DOC7.3: Use plain language without hype.
- DOC7.4: End pages with relevant next steps/links.
- DOC8: Runtime/lifecycle/persistence/messaging/deployment/failure pages **MUST** apply relevant distributed-systems checklist topics at contributing/documentation-guide.md#distributed-systems-checklist.
- DOC9: Completion **MUST** require complete frontmatter, resolving links, Docusaurus build, verified examples, repository terminology, and adjacent-content links.
- DOC10: Use the existing shared author-technical-documentation skill; if automatic discovery fails, read its selected contract and local guide directly. All policy remains mandatory without skill selection. ADRs use their dedicated policy/template, keeping shared evidence/metadata/validation.
- DOC11: Feature folders **MUST** honor page type.
- DOC11.2: Feature folders **MUST NOT** be catch-all pages.
- DOC11.3: Multi-type topics **MUST** split/cross-link.
- DOC11.4: Existing untouched folders **MAY** remain.
- DOC11.5: New public folders **SHOULD** use _category_.yml/generated indexes.
- DOC11.6: Filenames/neighbors **SHOULD** reveal page type.
- DOC11.7: Entry pages **SHOULD** orient/link rather than absorb categories.
- DOC11.8: Migration **MUST** stay separate from release notes/generic overviews.
- DOC11.9: Troubleshooting **MUST** stay symptom-first.

### Registration alternatives

Scope: Source path docs/Docusaurus/docs/**/event-sourcing-*.md is sufficient to select this section; actual branching pattern applies only after a valid page type and where generated AND manual alternatives exist (sagas/aggregates/UX projections/other Inlet features). It does not replace page-type policy.

- ALT1: Pages using the branching pattern **MUST** keep one primary type.
- ALT1.2: Shared setup **MUST** appear before branching.
- ALT1.3: The generated path **MUST** appear first.
- ALT1.4: The generated path **MUST** be marked recommended when it is the preferred repo path.
- ALT1.5: Manual registration **MUST** explain why a reader would choose it and what it makes explicit.
- ALT1.6: Both branches **MUST** describe equivalent runtime intent.
- ALT1.7: Both branches **MUST NOT** imply different guarantees unless evidence shows real behavioral differences.
- ALT1.8: A :::tip Registration Options callout **SHOULD** introduce divergence.
- ALT1.9: Branching **MUST NOT** combine concept/tutorial/reference into a giant page.

## References

- docs/Docusaurus/docs/contributing/documentation-guide.md (public authority, #file-and-navigation-rules, #migration-stance, #distributed-systems-checklist)
- Unchanged .agents/skills/author-technical-documentation/SKILL.md and its selected references; docs/Docusaurus/docs/adr/AGENTS.md

## Selected page contracts

| Page type | Portable contract | Local guide |
| --- | --- | --- |
| getting-started | [Contract](../../../.agents/skills/author-technical-documentation/references/getting-started.md) | [Guide](contributing/documentation-getting-started.md) |
| tutorials | [Contract](../../../.agents/skills/author-technical-documentation/references/tutorials.md) | [Guide](contributing/documentation-tutorials.md) |
| how-to | [Contract](../../../.agents/skills/author-technical-documentation/references/how-to.md) | [Guide](contributing/documentation-how-to.md) |
| concepts | [Contract](../../../.agents/skills/author-technical-documentation/references/concepts.md) | [Guide](contributing/documentation-concepts.md) |
| reference | [Contract](../../../.agents/skills/author-technical-documentation/references/reference.md) | [Guide](contributing/documentation-reference.md) |
| operations | [Contract](../../../.agents/skills/author-technical-documentation/references/operations.md) | [Guide](contributing/documentation-operations.md) |
| troubleshooting | [Contract](../../../.agents/skills/author-technical-documentation/references/troubleshooting.md) | [Guide](contributing/documentation-troubleshooting.md) |
| migration | [Contract](../../../.agents/skills/author-technical-documentation/references/migration.md) | [Guide](contributing/documentation-migration.md) |
| release-notes | [Contract](../../../.agents/skills/author-technical-documentation/references/release-notes.md) | [Guide](contributing/documentation-release-notes.md) |
