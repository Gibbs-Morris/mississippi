---
applyTo: 'docs/**/*.md'
---

# Documentation Authoring

Governing thought: Every documentation page exists to enable a decision or action through clear structure, consistent tone, and predictable navigation.

> Drift check: This file defines documentation standards for `docs/Docusaurus/`; update as documentation patterns evolve.

## Rules (RFC 2119)

- Authors **MUST** define the goal and target audience (administrator, developer, architect, executive) before writing. Why: Documentation without clear purpose creates confusion.
- Authors **MUST** use active voice and present tense; marketing language and unexplained jargon **MUST NOT** appear. Why: Clarity enables action.
- Authors **MUST** use RFC 2119 keywords ("MUST," "SHOULD," "MAY") consistently in rules sections only. Why: Prevents ambiguity in normative statements.
- Every page **MUST** include YAML front matter with `id`, `title`, and `sidebar_label`; filenames **MUST** use kebab-case. Why: Ensures consistent metadata and URLs.
- Pages **MUST** follow: title/summary (H1), body sections (H2/H3 max), related links, and references. Why: Predictable layout aids navigation.
- Documentation **MUST** comply with markdownlint rules per `.github/instructions/markdown.instructions.md`. Why: Ensures consistent, accessible Markdown.
- Each page **MUST** cover a single cohesive concept or procedure; pages exceeding 5–10 minutes reading time **SHOULD** be split. Why: Focused content is easier to maintain and consume.
- Sidebar nesting **MUST NOT** exceed two levels; deeper structures **MUST** be flattened with cross-links. Why: Deep hierarchies obscure the whole.
- Content **MUST** start with overview then deepen detail in later sections. Why: Readers can stop when they have enough context.
- Authors **MUST** link concepts, features, and commands to their descriptions on first mention. Why: Enables bottom-up navigation.
- Code samples **MUST** be minimal but complete, placed in fenced blocks with language identifiers. Why: Working examples accelerate understanding.
- Diagrams and flowcharts **SHOULD** be used for architecture, processes, and data relationships; all images **MUST** include alt text. Why: Accessibility and comprehension.
- Authors **MUST** update docs when features, performance characteristics, or components change; pages **MUST** note last updated date. Why: Prevents drift and staleness.

## Scope and Audience

All contributors writing or updating documentation in `docs/Docusaurus/`.

## At-a-Glance Quick-Start

- Define purpose and audience before writing.
- Use YAML front matter: `id`, `title`, `sidebar_label`, optional `description`/`keywords`.
- Structure: H1 title/summary → body sections (H2/H3) → related links → references.
- Keep pages focused (one topic, 5–10 minutes), sidebar shallow (max two levels).
- Link concepts on first mention; provide working code examples.
- Add alt text to images; use consistent terminology.
- Verify markdownlint passes before committing.

## Core Principles

- **Predictable Structure**: Readers orient quickly when pages follow the same layout.
- **Progressive Disclosure**: Start broad, deepen gradually; readers stop when satisfied.
- **Single Source of Truth**: Link instead of duplicating; maintain one authoritative page per concept.
- **Accessibility**: Alt text, clear language, and semantic HTML aid all readers.

## Procedures

### Creating a New Documentation Page

1. Define purpose and target audience.
2. Add YAML front matter with required fields (`id`, `title`, `sidebar_label`).
3. Write H1 title and one-sentence summary.
4. Draft body sections (H2/H3); keep each section focused.
5. Add "See also" links and references at bottom.
6. Note last updated date in metadata or content.
7. Place file in correct section; verify sidebar depth stays ≤2 levels.
8. Run markdownlint and fix findings.
9. Review checklist before committing.

### Writing Checklist

Before publishing:

- Purpose and audience clearly defined.
- YAML front matter complete: `id`, `title`, `sidebar_label`, optional `description`/`keywords`.
- Structure complete: title, summary, body sections (H2/H3 max), related links, references.
- Language concise; terminology consistent; active voice.
- Inline links connect concepts on first mention.
- Code examples minimal but complete with language identifiers.
- Visuals accurate, alt text present.
- Sidebar remains shallow (max two levels).
- Markdownlint passes with no warnings.

## References

- Markdown standards: `.github/instructions/markdown.instructions.md`
- Instruction authoring template: `.github/instructions/authoring.instructions.md`
- RFC 2119 keywords: `.github/instructions/rfc2119.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
