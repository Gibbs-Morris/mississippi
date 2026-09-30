# Docs policy current-main migration map

This slice rebuilds the Docs portion of [original PR #809](https://github.com/Gibbs-Morris/mississippi/pull/809) from current `main`. The source is the six instruction bodies at current base `64ae58b89b6e19a8c226c02e758d789bb7b5b0b5`; these bodies are unchanged from `92a7a2b9f3c7eff10ef1eb971210f313f3f71693`. Targets remain mandatory even when no skill activates. The instruction filenames and `applyTo` values remain exact compatibility selectors.

| Source | Path, content, and audience trigger | Target |
| --- | --- | --- |
| `markdown.instructions.md` | `**/*.md`; Markdown authors and reviewers anywhere in the repository | `docs/Docusaurus/AGENTS.md` through the unchanged Markdown adapter, including for files outside Docusaurus |
| `documentation-authoring.instructions.md` | `docs/Docusaurus/docs/**/*.{md,mdx}`; public documentation authors and reviewers | `docs/Docusaurus/docs/AGENTS.md` public documentation section |
| `documentation-page-focus.instructions.md` | Same public documentation paths and audience; reader intent controls type | Same public documentation section |
| `feature-documentation-structure.instructions.md` | Same paths; feature folder authors during the hybrid migration | Same public documentation section |
| `feature-docs-pattern.instructions.md` | `docs/Docusaurus/docs/**/event-sourcing-*.md` selects guidance; branching requires a valid primary page type and real generated and manual alternatives | `docs/Docusaurus/docs/AGENTS.md` registration alternatives section |
| `adr.instructions.md` | Numbered ADR paths; ADR authors and ADR-threshold decisions, including ADR Keeper work | `docs/Docusaurus/docs/adr/AGENTS.md` ADR lifecycle section; shared Docs evidence, metadata and validation continue to apply |

`R` below numbers Rules bullets in source order, excluding quick-start, checklists and references. Each source clause retains its original force, condition and exception in the named target. The unchanged adapter for that source requires a complete target read.

| Source rule | Original obligation or exception | Target clause |
| --- | --- | --- |
| Markdown R1 | Active lint and blocking warnings | M1, M1.2 |
| Markdown R2 | No suppressions except explicit single-case instruction | M2 |
| Markdown R3 | GFM and correct GitHub rendering | M3 |
| Markdown R4 | Local configured lint and fix before submission | M4, M4.2 |
| Markdown R5 | Prefer plain Markdown and accessibility | M5 |
| Docs author R1 | Correct engineering guidance, no marketing tone | DOC1 |
| Docs author R2 | No invented contracts or runtime behavior | DOC1.2 |
| Docs author R3 | Evidence required; unverified claims not fact | DOC1.3, DOC1.4 |
| Docs author R4 | Distinguish guarantee, default, typical, detail, unsupported, future | DOC1.5 |
| Docs author R5 | One question and exactly one page type | DOC2 |
| Docs author R6 | Selected skill contract and matching local guide before drafting/validation | DOC2.2, DOC10, selected page contracts table |
| Docs author R7 | Required and optional frontmatter fields | DOC4, DOC4.2 |
| Docs author R8 | `.md` unless MDX components needed | DOC4.3 |
| Docs author R9 | Relative internal links | DOC5 |
| Docs author R10 | Tabs only for real variants | DOC5.2 |
| Docs author R11 | Behavior-changing admonitions with blank lines | DOC5.3 |
| Docs author R12 | Mermaid preference and diagram explanation | DOC6, DOC6.2 |
| Docs author R13 | Flowchart direction and four-node boundary | DOC6.3, DOC6.4 |
| Docs author R14 | Verified runnable examples | DOC7 |
| Docs author R15 | Prerequisites, plain language, no hype, next steps | DOC7.2, DOC7.3, DOC7.4 |
| Docs author R16 | Runtime topics apply distributed-systems checklist | DOC8; existing public guide checklist |
| Docs author R17 | Complete metadata, links, build, examples, terminology, adjacency | DOC9 |
| Page focus R1 | Classify into the same nine types before writing | DOC2, selected page contracts table |
| Page focus R2 | No blended types | DOC2, DOC11.2 |
| Page focus R3 | One question and direct opening answer or scope | DOC2, DOC3 |
| Page focus R4 | Page contract over hybrid physical folder | DOC3 |
| Page focus R5 | Split and cross-link multiple intents | DOC3.2 |
| Page focus R6 | Prefer aligned placement, never let it override type | DOC3.3, DOC3.4 |
| Feature structure R1 | Type before historical folder shape | DOC11 |
| Feature structure R2 | Untouched folder allowance; new category and generated-index preference | DOC11.4, DOC11.5 |
| Feature structure R3 | No catch-all feature pages | DOC11.2 |
| Feature structure R4 | Split and cross-link multiple types | DOC11.3 |
| Feature structure R5 | Filename or neighbors reveal page type where practical | DOC11.6 |
| Feature structure R6 | Orientation entry pages with narrow links | DOC11.7 |
| Feature structure R7 | Migration isolated from release notes and overview | DOC11.8 |
| Feature structure R8 | Symptom-first troubleshooting even in feature folders | DOC11.9 |
| Feature pattern R1 | Primary page type remains required | ALT1 |
| Feature pattern R2 | Shared setup before branch | ALT1.2 |
| Feature pattern R3 | Generated first; mark recommended only when preferred | ALT1.3, ALT1.4 |
| Feature pattern R4 | Explain manual choice and explicit behavior | ALT1.5 |
| Feature pattern R5 | Equal runtime intent; evidence for any different guarantees | ALT1.6, ALT1.7 |
| Feature pattern R6 | Optional registration callout | ALT1.8 |
| Feature pattern R7 | No mixed-type giant page | ALT1.9 |
| ADR R1 | Published location, MADR 4.0.0 body, local metadata | ADR1, ADR8 |
| ADR R2 | Zero-padded sequential filename | ADR1.2 |
| ADR R3 | Provisional branch number; before-merge rebase, renumber, update links/metadata | ADR2, ADR2.2 |
| ADR R4 | Required and conditional frontmatter fields | ADR3, ADR3.2, ADR9 |
| ADR R5 | Three MADR minimum sections and chosen-option sentence | ADR4 |
| ADR R6 | Useful optional sections; simple-decision omission | ADR4.2, ADR4.3 |
| ADR R7 | Diagram trigger; trivial edit exclusion | ADR5, ADR5.2 |
| ADR R8 | Prose authority, diagram alignment and placement | ADR5.3, ADR5.4, ADR5.5 |
| ADR R9 | Omission rationale for diagram trigger | ADR5.6 |
| ADR R10 | Diagram type preferences | ADR5.7 |
| ADR R11 | Accepted immutability and supersession | ADR6, ADR6.2 |
| ADR R12 | Exact status set | ADR6.3 |
| ADR R13 | Relative ADR cross-references | ADR6.4 |
| ADR R14 | Prefer decisions written during or before choice | ADR7 |
| ADR R15 | ADR significance threshold | ADR7.2 |

Existing quick-start guidance remains represented by M5, M4.2, DOC2/DOC3/DOC11, ALT1 and ADR lifecycle clauses. The full distributed-systems checklist remains in `docs/Docusaurus/docs/contributing/documentation-guide.md#distributed-systems-checklist`. Old ADR template bindings remain in ADR9. Public pages, route structure and custom agents are unchanged by this slice. Current main already includes the Cosmos provider reference Overview and error subsection from PR #820; the Docs route comparison includes that page.

| Source reference or route | Current binding |
| --- | --- |
| Markdown active configuration and local lint | `.markdownlint-cli2.jsonc`, `.github/linters/.markdown-lint.yml` control active rules; the old fixed-range shorthand omitted disabled MD024, MD025 and MD060. `npx markdownlint-cli2 "**/*.md"` remains available. Original #809's verify-change binding is absent on current main, so no adapter points to it. |
| Product-page procedure and nine type contracts | Existing `.agents/skills/author-technical-documentation/SKILL.md`, its selected reference, and corresponding current `docs/Docusaurus/docs/contributing/documentation-*.md` guide. |
| ADR procedure and MADR body | Existing `.agents/skills/author-architecture-decision/SKILL.md`, `assets/madr-body.md`, MADR 4.0.0 specification, key principles and ADR Keeper agent. |
| Public docs authority | Existing documentation guide and its file/navigation, migration and distributed-systems sections. |
| Verification | Configured Markdown lint, Docusaurus typecheck and build, route/404 browser test; canonical repository gates remain in force. |

Docusaurus extends `GlobExcludeDefault` with `**/AGENTS.md`, preserving the installed defaults and ordinary page discovery. `@docusaurus/utils` is an exact `3.9.2` devDependency; the lock change is root metadata only. The remaining #809 domains require separate current-main successors or evidence-backed retirement. This slice does not complete #561/#532 cross-agent conformance or authorize closing original #809.

The original cross-references to global shared policies, Markdown standards, public Docs authoring, page focus, feature structure, ADR authoring, the ADR Keeper, the MADR specification, and key principles still resolve at their existing paths. The adapters and profiles retain direct links for their applicable paths.
