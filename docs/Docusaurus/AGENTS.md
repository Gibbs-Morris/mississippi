---
applyTo: '**/*.md'
---

# Markdown contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance remains effective. This profile is reused for Markdown anywhere in the repository through root selection; its location does not restrict the source **/*.md scope. Inspect active .markdownlint-cli2.jsonc and .github/linters/.markdown-lint.yml before changing guidance.

## Rules (RFC 2119)

### Markdown

Scope: All **/*.md paths and Markdown authored/reviewed as content.

- M1: Markdown **MUST** meet active markdownlint rules (MD001–MD059 except MD013 per current config).
- M1.2: Lint warnings **MUST** block builds.
- M2: Rules **MUST NOT** be disabled/suppressed/reconfigured, including inline markdownlint-disable, unless explicitly instructed for one case.
- M3: Content **MUST** use GitHub Flavored Markdown and render correctly on GitHub.
- M4: Authors **MUST** run configured markdownlint locally.
- M4.2: Authors **MUST** fix all markdownlint findings before submission. Existing verify-change bindings select its actual runner, including the retained npx markdownlint-cli2 example.
- M5: Plain Markdown over inline HTML and accessible links/alt text/semantic headings **SHOULD** be preferred. Retain one top-level heading and blank separation around lists/tables/fences; fix findings rather than suppress them.

## References

- .markdownlint-cli2.jsonc; .github/linters/.markdown-lint.yml (active configuration authority)
- .github/agent-guidance/verify-change-bindings.md#authority-and-check-selection (current runner; retained example: npx markdownlint-cli2 "**/*.md")
