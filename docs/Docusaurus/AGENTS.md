---
applyTo: '**/*.md'
---

# Markdown contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance remains effective. This profile is reused for Markdown anywhere in the repository through root selection; its location does not restrict the source `**/*.md` scope. Inspect active .markdownlint-cli2.jsonc and .github/linters/.markdown-lint.yml before changing guidance.

## Rules (RFC 2119)

### Markdown

Scope: All `**/*.md` paths and Markdown authored/reviewed as content.

- M1: Markdown **MUST** meet every active markdownlint rule in the current configurations.
- M1.2: Lint warnings **MUST** block builds.
- M2: Rules **MUST NOT** be disabled/suppressed/reconfigured, including inline markdownlint-disable, unless explicitly instructed for one case.
- M3: Content **MUST** use GitHub Flavored Markdown and **MUST** render correctly on GitHub.
- M4: Authors **MUST** run configured markdownlint locally.
- M4.2: Authors **MUST** fix all markdownlint findings before submission. The configured local runner remains `npx markdownlint-cli2 "**/*.md"`.
- M5: Plain Markdown over inline HTML and accessible links/alt text/semantic headings **SHOULD** be preferred. Retain one top-level heading and blank separation around lists/tables/fences; fix findings rather than suppress them.

## References

- .markdownlint-cli2.jsonc; .github/linters/.markdown-lint.yml (active configuration authority)
- Configured runner: `npx markdownlint-cli2 "**/*.md"` (use the active Markdown configuration)
- [Shared guardrails](../../.github/instructions/shared-policies.instructions.md) remain global.
