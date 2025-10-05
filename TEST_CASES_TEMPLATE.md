# Test Case Specification Template

Use this structure for each project `suggested-test-cases.md` document.

## Legend

- PRIORITY: (H)igh / (M)edium / (L)ow
- TYPE: Unit / Integration / Contract / Mutation-Focus
- ID: Stable anchor for cross‑referencing when implementing tests.

## Document Structure

1. Project Metadata
2. Global Invariants & Cross-Cutting Concerns
3. Per File Sections
   - File Path
   - Summary
   - Per Type (class / record / struct / interface / static class)
   - Per Member
4. Deferred / Open Questions

## File Section Format

```markdown
### File: <relative path>
<short purpose>

#### Type: <TypeName> (<kind>)
<type responsibility sentence(s)>

Member: `<signature>`
Purpose: <concise behavior description>
Invariants:
- <bullet list of conditions that must always hold>
Test Cases:
| ID | Scenario | Given / When / Then | Edge Focus | Priority | Type |
|----|----------|---------------------|------------|----------|------|
| T1 | ... | G: ... W: ... T: ... | e.g. Null / Bounds / Concurrency | H | Unit |

Notes:
- <optional clarifications>
```

Keep scenarios behaviorally distinct; avoid duplicating pure parameter permutations unless they cover new logic branches.

## Writing Good Scenarios

- Prefer observable outcomes (return value, state change, interaction) over implementation details.
- Cover: Happy path, boundary, error/exception, concurrency (if relevant), cancellation (if token accepted), performance guard (oversized input), serialization (if annotated), logging side-effects (if structured logging).

## Mutation Testing Considerations

- Include at least one assertion per logical branch.
- Include tests that would fail if comparison operators changed (`>`, `>=`, etc.) where critical.

---
End of template.
