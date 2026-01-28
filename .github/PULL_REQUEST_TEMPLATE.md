# [Title: Concise summary of the change]

## Business Value

[Explain why this change matters to users, operators, or the business. What problems does it solve? What capabilities does it enable?]

**Key benefits:**

1. [Benefit 1]
2. [Benefit 2]
3. [Benefit 3]

## Common Use Cases

| Domain | Use Case | Example |
|--------|----------|---------|
| [Domain 1] | [Use case description] | [Concrete example] |
| [Domain 2] | [Use case description] | [Concrete example] |
| [Domain 3] | [Use case description] | [Concrete example] |

## How It Works

### Overview

[Provide a high-level explanation of the feature/change. Include architecture diagrams if helpful.]

```text
[ASCII diagram or Mermaid diagram showing the flow]
```

### Key Design Decisions

- **[Decision 1]**: [Rationale]
- **[Decision 2]**: [Rationale]
- **[Decision 3]**: [Rationale]

### Code Example

```csharp
// Show a minimal working example of how to use the feature
```

## Observability

[If applicable, describe metrics, logging, or tracing added.]

| Metric/Log | Type | Description |
|------------|------|-------------|
| [Name] | [Counter/Histogram/Log] | [What it measures] |

## Files Changed

### New Files

| File | Purpose |
|------|---------|
| [path/to/file.cs](path/to/file.cs) | [Brief description] |

### Modified Files

| File | Change |
|------|--------|
| [path/to/file.cs](path/to/file.cs) | [What changed and why] |

### New Tests

| File | Coverage |
|------|----------|
| [path/to/tests.cs](path/to/tests.cs) | [What scenarios are tested] |

## Quality Gates

- [ ] Build passes with zero warnings
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated (if applicable)

## Migration Notes

[If this is a breaking change, explain what users need to do to migrate.]

```csharp
// Before
[old code pattern]

// After
[new code pattern]
```

## Related Issues

- Fixes #[issue number]
- Related to #[issue number]

---

<!--
PR Description Guidelines:

1. Start with business value - why does this matter?
2. Include concrete use cases across different domains
3. Explain how it works holistically with diagrams
4. Document design decisions and their rationale
5. List all files changed with brief descriptions
6. Include code examples that users can copy
7. Document any breaking changes and migration paths

See .github/instructions/pr-description.instructions.md for full guidance.
-->
