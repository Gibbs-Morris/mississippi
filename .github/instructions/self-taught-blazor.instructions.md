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

- Agents **MUST** correlate protected HTTP read outcomes with the originating request, entity and persona when presenting current authorization. Why: Spring displayed an earlier allowed HTTP 200 and event count after a newer persona received HTTP 401 in the flagship review; obsolete outcomes must not replace current proof.

- Agents **SHOULD** verify denied streaming HTTP reads through the actual status and settled current UI, and bound any extra body or completion waits. Why: Spring's `ResponseHeadersRead` projection GET displayed HTTP 401 while an unbounded Playwright `FinishedAsync` stalled L3; status, denial, absent-count and enabled-refresh checks completed, while successful reads were checked against real response JSON.

- Agents **SHOULD** verify keyboard focus separately from fragment scrolling in Blazor, and explicitly focus same-page jump targets when navigation only scrolls. Why: Spring's account jumps preserved both IDs and scrolled correctly while focus stayed on an offscreen link; panel `FocusAsync` fixed the failing browser assertions while real hrefs and modified-click navigation remained intact.

- Agents **SHOULD** constrain grid tracks and item minimum widths around wide scrollers. Why: Spring's expanded 600px history widened a 390px page to 660px despite `overflow: auto`; `minmax(0, 1fr)` and `min-width: 0` restored its keyboard-scrollable containment.

- Agents **SHOULD** make failed projection read outcomes exclusive from loading, empty or healthy cached outcomes. Why: Spring PR #1029 reproduced contradictory balance, ledger and transfer status displays in 15 rendered tests.

- Agents **SHOULD** keep entity ID drafts separate from selections that start reads or subscriptions. Why: Typing `p` in Spring PR #1029 replaced the `auth-proof` subscription and read an unfinished ID.

- Agents **SHOULD** share page-owned projection subscriptions when multiple views select the same exact entity ID, releasing them only when no view retains that ID. Why: Spring PR #1029 emitted duplicate subscribe actions for equal panel IDs and unsubscribed a still-selected account when one panel changed.

- When testing Blazor cleanup with bUnit, agents **SHOULD** await renderer disposal before asserting released resources. Why: Spring PR #1029's rendered-wrapper disposal produced no component unsubscribe actions; awaited `DisposeComponentsAsync` exercised the real disposal path.

## References

- Self-improvement governance: [Self-Improvement Learning System](self-improvement.instructions.md).
- Component guidance: [Blazor UX Guidelines](blazor-ux-guidelines.instructions.md).
