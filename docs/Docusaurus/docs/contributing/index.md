---
sidebar_position: 1
title: Contributing to Documentation
description: How to contribute to Mississippi documentation
---

This section contains guides for documentation contributors.

## Documentation Standards

The [Documentation Standards](./documentation-standards.md) guide defines:

- Information architecture and folder structure
- Document types and their purposes
- Page structure and required sections
- Writing style and voice guidelines
- Code example conventions
- Cross-referencing patterns
- Quality checklist for all pages

All contributors should read this guide before making documentation changes.

## Quick Reference

### Required Frontmatter

```yaml
---
sidebar_position: 1
title: Page Title
description: One-sentence description (max 160 chars)
---
```

### Document Types

| Type | Purpose | Example |
| --- | --- | --- |
| Tutorial | Teach through doing | "Your First Aggregate" |
| How-to Guide | Solve a problem | "Add Custom Storage" |
| Concept | Explain ideas | "Event Sourcing Basics" |
| Reference | Specifications | "IBrookStorage API" |

### Validation

Run before committing:

```bash
npx markdownlint-cli2 "docs/**/*.md"
```

## Related Topics

- [Documentation Standards](./documentation-standards.md)
