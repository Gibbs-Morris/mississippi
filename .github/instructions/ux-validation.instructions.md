---
applyTo: '**'
---

# UX Validation and PR Evidence

Governing thought: User-visible changes are validated against the rendered browser experience, not source code alone.

> Drift check: Use `README.md`, `testing.instructions.md`, and the applicable L3 test project to select the canonical Playwright command and browser setup.

## Rules (RFC 2119)

- Agents **MUST** use Playwright against the final running application whenever a change affects user-visible layout, styling, interaction, navigation, content presentation, accessibility behavior, or browser-facing loading, empty, or error states and the final application/browser path is available. Why: Compilation and source inspection cannot prove rendered UX.
- Agents **MUST** capture one or more screenshots from the final rendered output at every affected viewport or interaction state needed to validate the change when the final application/browser path is available and Playwright executes successfully. Why: Visual evidence records what users actually see.
- Agents **MUST** inspect the screenshots against the intended behavior. Why: Visual comparison is required to validate the rendered UX.
- Agents **MUST** record the route or state, viewport, and relevant Playwright command in the PR. Why: Reviewers need reproducible evidence rather than an assertion that the UI was checked.
- For accessibility-only changes, agents **MUST** add at least one affected Playwright semantic or interaction assertion, such as an accessible-tree/ARIA snapshot or keyboard interaction, when the final application/browser path is available and Playwright executes successfully. Why: Rendered pixels can remain unchanged while accessible behavior changes.
- PRs containing a UX change **MUST** include the final screenshots as rendered image attachments or Markdown images in the PR description or a top-level PR comment, with a concise caption for each. Why: The evidence must be available to reviewers in the PR.
- If the application cannot be run, Playwright cannot execute, or the screenshots cannot be posted, agents **MUST** report the exact limitation and the unvalidated states in the PR. Why: Missing evidence must remain visible.
- Agents **MUST NOT** claim that UX validation passed when the application cannot run, Playwright cannot execute, or the screenshots cannot be posted. Why: An unavailable validation path cannot establish a pass.
- Screenshot evidence **SHOULD** cover responsive, keyboard, focus, loading, empty, and error states when those states are affected by the change. Why: A single happy-path image can miss the changed experience.

## Scope and Audience

All agents and contributors making user-visible UI/UX or browser-behavior changes, regardless of which source file implements the behavior.

## At-a-Glance Quick-Start

- Identify the changed user-visible states and affected viewport sizes.
- Run the applicable browser journey with Playwright against the final application state.
- Capture and inspect screenshots after the last implementation fix.
- Add the screenshots, captions, route/state, viewport, and command to the PR.
- If blocked, record the limitation and remaining validation gap in the PR.

## Core Principles

- Rendered behavior is the source of truth for UX.
- Visual evidence complements, rather than replaces, automated browser assertions.
- A validation gap is reported explicitly instead of being treated as a pass.

## References

- Testing and browser levels: `.github/instructions/testing.instructions.md`
- Blazor component guidance: `.github/instructions/blazor-ux-guidelines.instructions.md`
- PR evidence requirements: `.github/instructions/pr-description.instructions.md`
- Repository test setup: `README.md`
