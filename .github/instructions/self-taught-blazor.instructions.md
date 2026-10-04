---
applyTo: '**/*.razor*'
---

# Self-Taught Lessons: Blazor

Governing thought: Preserve caller attribute inputs while protecting component-owned semantics.

> Drift check: Check overlapping instructions and existing lessons before adding guidance, following `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Agents **SHOULD** preserve dictionary key semantics or explicitly normalize duplicates when forwarding `AdditionalAttributes`. Why: Case-distinct `data-note`/`DATA-NOTE` inputs made SmokeConfirm's case-insensitive `ToDictionary` throw `ArgumentException` during rendering in PR #814.

- Agents **MUST** treat Boolean values as absent when composing string-valued ID references or CSS classes from `AdditionalAttributes`. Why: Conditional attributes became nonexistent `False`/`True` IDs in PR #814 and bogus CSS tokens in PR #815; preserve Razor conditional-attribute semantics instead.

- Agents **MUST** generate unique per-instance ARIA target IDs and retain them across rerenders. Why: Two `EmitterDemo` instances in PR #817 shared fixed title/description IDs, so relationships could resolve to another instance.

## References

- Self-improvement governance: [Self-Improvement Learning System](self-improvement.instructions.md).
- Component guidance: [Blazor UX Guidelines](blazor-ux-guidelines.instructions.md).
