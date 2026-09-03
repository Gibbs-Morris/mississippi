---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Technical Documentation Information Architecture

Governing thought: Mississippi documentation has one public hierarchy based on what the reader is trying to achieve, not on the repository's internal package seams.

> Drift check: Align this file with `docs/Docusaurus/docs/contributing/documentation-guide.md`, `docs/Docusaurus/sidebars.ts`, and the current published folder tree.

## Rules (RFC 2119)

- The current public technical docs **MUST** use the canonical top-level areas `getting-started/`, `tutorials/`, `how-to/`, `concepts/`, `reference/`, `adr/`, and `contributing/`. Why: Reader intent provides one predictable entry model for people and retrieval tools.
- `getting-started/` **MUST** contain only verified first-success paths, `tutorials/` guided learning sequences, `how-to/` focused tasks, `concepts/` mental models and trade-offs, and `reference/` exact lookup material. Why: Folder names must make a reliable promise about what the reader will find.
- Subsystem and package names **MUST NOT** become a competing top-level navigation tree. Their ownership and entry points **SHOULD** be consolidated in reference material and linked from relevant task or concept pages. Why: Readers should not need to decode internal seams before finding a useful path.
- `adr/` and `contributing/` **MAY** remain separate governance collections. Why: Decisions and contributor policy are maintained sequences rather than product-learning paths.
- A nested folder **MUST** contain multiple durable pages with a shared reader purpose. Why: Folders must represent real depth rather than a desired taxonomy matrix.
- Authors **MUST NOT** create pages or categories merely to make the taxonomy look complete. Reserved `operations/`, `troubleshooting/`, `migration/`, and `release-notes/` areas **MAY** be added only when at least one specific evidenced page exists. Why: Honest gaps are more useful than navigation filler.
- A category **MAY** use a generated index when labels and descriptions provide enough orientation; an authored index **MUST** add durable guidance rather than repeat its children. Why: Navigation pages must earn their maintenance cost.
- Public routes **MUST** derive from the filesystem by default. A custom `slug` or `id` **MAY** be used only for a deliberate stable route, concise category route, or compatibility requirement. Why: Parallel path metadata creates a second hierarchy that drifts.
- Historical material **MUST** live outside `docs/Docusaurus/docs/` and **MUST NOT** be presented as current guidance. Why: Git history and the source archive preserve context without polluting the active manual.

## Scope and Audience

Contributors and agents placing or restructuring public technical documentation.

## At-a-Glance Quick-Start

- Choose the reader's job first: first success, guided learning, focused task, understanding, or lookup.
- Put the page in the matching intent folder.
- Put subsystem and package ownership in `reference/`, then link to it from task and concept pages.
- Add a nested folder only after the topic has real depth.
- Let the filesystem define the public route unless compatibility requires an exception.

## Canonical Tree

```text
docs/
├── index.md
├── getting-started/
├── tutorials/
├── how-to/
├── concepts/
├── reference/
├── adr/
└── contributing/
```

## Core Principles

- **One Primary Axis**: The filesystem follows the reader's job.
- **Internal Seams Are Reference**: Package ownership remains discoverable without becoming the front door.
- **Depth Must Be Earned**: Folders exist for multiple useful pages, never for symmetry.
- **Paths Stay Literal**: Filesystem and public routes describe the same model by default.
- **Current Means Current**: Historical guidance stays outside the published corpus.

## References

- Documentation guide: `docs/Docusaurus/docs/contributing/documentation-guide.md`
- Documentation page focus: `.github/instructions/documentation-page-focus.instructions.md`
- Documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
