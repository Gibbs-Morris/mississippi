---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Documentation Authoring

Governing thought: Mississippi documentation exists to help engineers make correct decisions and complete real work without invented behavior, blurred guarantees, or navigation debt.

> Drift check: The public authoring model lives under `docs/Docusaurus/docs/contributing/`; keep this instruction aligned with that Docusaurus guidance.

## Rules (RFC 2119)

- Authors **MUST** optimize for correctness, clarity, navigability, and maintainability; authors **MUST NOT** optimize for marketing tone. Why: Mississippi docs are engineering guidance, not promotional copy.
- Authors **MUST NOT** invent APIs, configuration keys, defaults, guarantees, limits, exception types, or runtime behavior. Why: Documentation is a contract surface.
- Claims **MUST** be backed by source code, tests, verified samples, design docs, ADRs, or runtime evidence; if a claim cannot be verified, it **MUST NOT** be published as fact. Why: Truthfulness is non-negotiable.
- Authors **MUST** distinguish guaranteed behavior, default behavior, typical behavior, implementation detail, unsupported behavior, and future intent. Why: Readers need to know what Mississippi actually promises.
- Each page **MUST** answer one primary question and **MUST** use exactly one page type. Why: Mixed page types produce confusing documents.
- For the nine product-documentation page types, authors and reviewers **MUST** read and follow the selected contract in the [technical-documentation skill](../../.agents/skills/author-technical-documentation/SKILL.md), together with the corresponding local guide below, before drafting or validating. Why: Required page-type behavior stays explicit while unrelated types load only when needed.
- Every public page **MUST** include `title`, `description`, and `sidebar_position` in frontmatter; authors **MAY** add `sidebar_label`, `pagination_label`, `slug`, `tags`, `draft`, and `id` when needed. Why: Core metadata keeps pages navigable while allowing repo-compatible stability fields.
- Authors **MUST** use `.md` unless the page genuinely needs MDX components. Why: Plain Markdown is easier to maintain.
- Internal doc links **MUST** use relative Markdown links. Why: Relative links survive route and branch changes more reliably.
- Tabs **MUST** be used only for true parallel variants such as operating system, language, or hosting mode. Why: Tabs hide information and should be reserved for real alternatives.
- Admonitions **MUST** be used only when the note materially changes user behavior, and blank lines **MUST** be left inside them. Why: Admonitions should be high-signal and robust under formatters.
- Mermaid **SHOULD** be preferred over screenshots for diagrams, and every diagram **MUST** include an introductory sentence and a clear main point. Why: Source diagrams are reviewable and easier to maintain.
- Mermaid flowcharts with more than four nodes **MUST** use `flowchart TB` (top-to-bottom); `flowchart LR` **MAY** be used only when the diagram has four or fewer nodes. Why: Docs render at a fixed width and readers scroll vertically; wide LR diagrams overflow or become unreadably compressed.
- Runnable code examples **MUST** come from verified samples, newly verified samples, or executable verification tied to tests or builds. Why: Sample drift is worse than no sample.
- Authors **MUST** make prerequisites explicit, use plain language, avoid hype, and end pages with relevant next steps or related links. Why: Readers need clear action, not filler.
- The distributed-systems checklist **MUST** be applied when a page describes runtime semantics, lifecycle, persistence, messaging, deployment, or failure behavior. Why: Those topics are where under-specified docs cause the most damage.
- A documentation change **MUST NOT** be considered complete until frontmatter is complete, links resolve, the Docusaurus site builds, examples are verified, terminology is repo-consistent, and adjacent content is linked. Why: Documentation quality is part of the build contract.

## Scope and Audience

All contributors and agents writing or updating public docs under `docs/Docusaurus/docs/`.

## Procedure and Page-Type Contracts

Use the shared skill to classify the page and read only its selected contract.
All rules above remain mandatory when a skill is unavailable or not selected;
read the linked files directly if automatic discovery is unavailable. ADRs use
their [dedicated policy and template](adr.instructions.md) rather than these
product-page layouts; shared evidence, metadata, and validation rules still apply.

| Page type | Portable contract | Local authoring guide |
| --- | --- | --- |
| `getting-started` | [Contract](../../.agents/skills/author-technical-documentation/references/getting-started.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-getting-started.md) |
| `tutorials` | [Contract](../../.agents/skills/author-technical-documentation/references/tutorials.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-tutorials.md) |
| `how-to` | [Contract](../../.agents/skills/author-technical-documentation/references/how-to.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-how-to.md) |
| `concepts` | [Contract](../../.agents/skills/author-technical-documentation/references/concepts.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-concepts.md) |
| `reference` | [Contract](../../.agents/skills/author-technical-documentation/references/reference.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-reference.md) |
| `operations` | [Contract](../../.agents/skills/author-technical-documentation/references/operations.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-operations.md) |
| `troubleshooting` | [Contract](../../.agents/skills/author-technical-documentation/references/troubleshooting.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-troubleshooting.md) |
| `migration` | [Contract](../../.agents/skills/author-technical-documentation/references/migration.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-migration.md) |
| `release-notes` | [Contract](../../.agents/skills/author-technical-documentation/references/release-notes.md) | [Guide](../../docs/Docusaurus/docs/contributing/documentation-release-notes.md) |

## Distributed-Systems Checklist

Apply the relevant subset of these topics when the page describes runtime behavior:

- activation or lifecycle boundaries
- concurrency or scheduling assumptions
- ordering guarantees and non-guarantees
- retry behavior and timeout behavior
- persistence or durability semantics
- failure handling and recovery implications
- serialization and version compatibility implications
- deployment or cluster assumptions
- diagnostics or telemetry needed to validate behavior
- security constraints
- unsupported or dangerous patterns

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-guide.md`
- Markdown standards: `.github/instructions/markdown.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
