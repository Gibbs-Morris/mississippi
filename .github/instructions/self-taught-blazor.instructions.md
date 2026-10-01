---
applyTo: '**/*.razor*'
---

# Self-Taught Lessons: Blazor

Governing thought: Preserve caller attribute inputs while protecting component-owned semantics.

> Drift check: Check overlapping instructions and existing lessons before adding guidance, following `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Agents **SHOULD** preserve dictionary key semantics or explicitly normalize duplicates when forwarding `AdditionalAttributes`. Why: Case-distinct `data-note`/`DATA-NOTE` inputs made SmokeConfirm's case-insensitive `ToDictionary` throw `ArgumentException` during rendering in PR #814.

## References

- Self-improvement governance: [Self-Improvement Learning System](self-improvement.instructions.md).
- Component guidance: [Blazor UX Guidelines](blazor-ux-guidelines.instructions.md).
