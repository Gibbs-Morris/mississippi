---
sidebar_position: 1
title: Documentation Standards
description: Style guide, structure, and conventions for Mississippi documentation
---

This guide defines the standards for all Mississippi documentation. Following
these conventions ensures consistency, discoverability, and maintainability as
the documentation scales.

## Core Principles

1. **Scannable** — Readers scan before they read. Use headings, tables, and
   visual hierarchy to enable quick orientation.
2. **Task-oriented** — Most readers want to accomplish something. Lead with
   what they can do, not what the system is.
3. **Progressive disclosure** — Start simple, add complexity. Each section
   should build on the previous.
4. **Single source of truth** — Define concepts once, link elsewhere. Avoid
   duplicating explanations across pages.
5. **Maintainable at scale** — Structure and conventions must work for 10,000+
   documents across multiple contributors.

## Information Architecture

### Folder Hierarchy

```text
docs/
├── index.md                    # Landing page / Start Here
├── getting-started/            # Onboarding sequence
│   ├── index.md               # Overview of getting started
│   ├── installation.md        # Package installation
│   ├── first-project.md       # Tutorial: first project
│   └── next-steps.md          # Where to go after tutorial
├── concepts/                   # Explanatory / conceptual content
│   ├── index.md               # Concepts overview
│   ├── architecture.md        # System architecture
│   ├── event-sourcing.md      # ES fundamentals
│   └── cqrs.md                # CQRS fundamentals
├── guides/                     # Task-oriented how-to guides
│   ├── index.md               # Guides overview
│   ├── aggregates/            # Aggregate guides
│   ├── projections/           # Projection guides
│   └── deployment/            # Deployment guides
├── reference/                  # API and configuration reference
│   ├── index.md               # Reference overview
│   ├── api/                   # Generated API docs
│   ├── configuration/         # Config options
│   └── cli/                   # CLI reference
├── components/                 # Component deep-dives
│   ├── aggregates.md          # Aggregates reference
│   ├── brooks.md              # Brooks reference
│   ├── projections.md         # Projections reference
│   └── ...                    # Other components
└── contributing/               # Docs about docs
    ├── documentation-standards.md  # This file
    └── ...
```

### Document Types

Each document should fit one of these types. The type determines structure and
tone.

| Type | Purpose | Tone | Example |
| --- | --- | --- | --- |
| **Tutorial** | Teach through doing | Guided, encouraging | "Your First Aggregate" |
| **How-to Guide** | Solve a specific problem | Direct, practical | "How to Add Custom Storage" |
| **Concept** | Explain ideas and architecture | Educational, thorough | "Event Sourcing Fundamentals" |
| **Reference** | Provide specifications | Precise, complete | "IBrookStorage Interface" |

### Naming Conventions

**Folders:**

- Use lowercase with hyphens: `getting-started/`, `how-to-guides/`
- Use plural for collections: `components/`, `guides/`
- Use singular for concepts: `architecture.md`, `security.md`

**Files:**

- Use lowercase with hyphens: `custom-event-storage.md`
- Use `index.md` for folder landing pages
- Match URL slug to filename

**Titles:**

- Title case for page titles: "Getting Started with Aggregates"
- Sentence case for headings within pages: "Defining command handlers"

## Page Structure

### Required Frontmatter

Every document must include YAML frontmatter:

```yaml
---
sidebar_position: 1
title: Page Title
description: One-sentence description for SEO and link previews (max 160 chars)
---
```

Optional frontmatter fields:

```yaml
---
sidebar_label: Short Label    # Shorter sidebar label if title is long
keywords: [keyword1, keyword2] # Additional SEO keywords
---
```

### Standard Page Sections

Pages should follow this general structure (adapt as needed):

```markdown
---
frontmatter
---

Opening paragraph that answers: What is this? Why should I care?

## Overview (optional)

Brief orientation if the opening paragraph isn't sufficient.

## Main Content

Core content organized by logical sections.

## Examples

Code examples demonstrating usage.

## Common Patterns (optional)

Patterns and best practices.

## Troubleshooting (optional)

Common issues and solutions.

## Next Steps / Related Topics

Links to continue learning. Use **Next Steps** for sequential content (tutorials,
getting-started guides) and **Related Topics** for reference documentation.
```

### Heading Hierarchy

- **H1**: Reserved for page title (from frontmatter `title:`)
- **H2**: Major sections
- **H3**: Subsections within H2
- **H4**: Rare; use for sub-subsections only when necessary

Never skip heading levels (H2 → H4). Keep heading depth ≤ 4.

## Writing Style

### Voice and Tone

- **Active voice**: "The aggregate validates the command" not "The command is
  validated by the aggregate"
- **Second person for instructions**: "You define reducers..." not "One defines
  reducers..."
- **Present tense**: "The store dispatches actions" not "The store will
  dispatch actions"
- **Direct and concise**: Remove filler words. Get to the point.

### Technical Writing Guidelines

**Be precise:**

```markdown
❌ The system handles events quickly.
✅ The projection processes approximately 10,000 events per second.
```

**Be specific:**

```markdown
❌ Configure the storage provider appropriately.
✅ Set `ConnectionString` in `appsettings.json` to your Cosmos DB endpoint.
```

**Avoid jargon without definition:**

```markdown
❌ The grain uses virtual actor semantics.
✅ The grain uses virtual actor semantics—Orleans activates it on demand and
   manages its lifecycle automatically.
```

### Code Examples

**Every code block must have a language identifier:**

````markdown
❌ Unlabeled code block:

```text
var x = 1;
```

✅ Labeled code block:

```csharp
var x = 1;
```
````

**Include context comments when helpful:**

```csharp
// In your aggregate's command handler
public async Task Handle(PlaceOrderCommand cmd)
{
    // Validate against current state
    if (State.IsPlaced)
        throw new InvalidOperationException("Order already placed");

    // Emit the domain event
    await Emit(new OrderPlacedEvent(cmd.OrderId, cmd.Items));
}
```

**Show complete, runnable examples where possible.** Avoid snippets that won't
compile without significant additions.

### Tables

Use tables for structured data, comparisons, and quick reference:

```markdown
| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `connectionString` | `string` | Yes | Cosmos DB connection string |
| `databaseName` | `string` | Yes | Target database name |
| `throughput` | `int` | No | Provisioned RU/s (default: 400) |
```

Always include a header row. Align pipes for readability in source.

### Diagrams

Use Mermaid for diagrams. Include a text description for accessibility:

```markdown
The following diagram shows the event flow from command to storage:

​```mermaid
flowchart LR
    CMD[Command] --> AGG[Aggregate]
    AGG --> BROOK[(Brook)]
​```
```

Prefer simple diagrams. Complex diagrams should be broken into multiple
focused diagrams.

## Cross-Referencing

### Internal Links

Use relative paths for internal links:

```markdown
✅ See [Aggregates](../components/aggregates.md) for details.
✅ The [getting started tutorial](./first-project.md) covers this.
❌ See [Aggregates](/docs/components/aggregates) for details.
```

### Link Text

Link text should describe the destination, not the action:

```markdown
❌ Click [here](./aggregates.md) for more information.
✅ See the [Aggregates reference](./aggregates.md) for more information.
```

### Canonical Definitions

Define terms in one place and link to that definition:

```markdown
A [brook](../components/brooks.md) stores the event stream...
```

## Versioning and Maintenance

### Document Lifecycle

1. **Draft** — Work in progress, may be incomplete
2. **Published** — Complete and reviewed
3. **Deprecated** — Superseded but retained for reference
4. **Archived** — Removed from navigation, retained for URLs

### Breaking Changes

When behavior changes:

1. Update affected documentation
2. Add a note about when the change occurred
3. Provide migration guidance if applicable

```markdown
:::note Version 2.0+
This API changed in version 2.0. For the previous behavior,
see [v1.x documentation](../archive/v1/aggregates.md).
:::
```

## Quality Checklist

Before publishing, verify:

- [ ] Frontmatter includes `sidebar_position`, `title`, and `description`
- [ ] Description is under 160 characters
- [ ] No duplicate H1 headings (frontmatter title is the H1)
- [ ] All code blocks have language identifiers
- [ ] All internal links use relative paths
- [ ] All diagrams have text descriptions
- [ ] Page passes `markdownlint` with zero errors
- [ ] Examples are complete and tested

## Related Topics

- [Markdown Syntax Reference](https://www.markdownguide.org/)
- [Docusaurus Documentation](https://docusaurus.io/docs)
- [Microsoft Writing Style Guide](https://learn.microsoft.com/style-guide/)
