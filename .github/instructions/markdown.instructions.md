---
applyTo: "**/*.md"
---

# Markdown Standards

## Scope
Content markdown only. Instruction authoring in separate file. Excludes `.instructions.md`.

## Quick-Start
Use ATX headings (`#`), fenced code blocks with language, alt text for images, meaningful link text. Run `npx markdownlint-cli2 "**/*.md"`.

## Core Principles
GFM syntax. One H1 per file. Blank lines around blocks. No trailing whitespace (except line breaks). MD013 (line length) disabled only. All other markdownlint rules enforced.

## Anti-Patterns
❌ Bare URLs. ❌ Missing alt text. ❌ Generic link text. ❌ Trailing spaces. ❌ Inline HTML unnecessarily.

## Enforcement
Lint failures block PRs. Config: `.markdownlint-cli2.jsonc`, `.github/linters/.markdown-lint.yml`.
