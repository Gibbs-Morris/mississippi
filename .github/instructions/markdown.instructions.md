---
applyTo: '**/*.md'
---

# Markdown and markdownlint

Governing thought: All Markdown must pass the configured markdownlint rules with no suppressions; fix content instead of disabling rules.

> Drift check: Lint configs live in `.markdownlint-cli2.jsonc` and `.github/linters/.markdown-lint.yml`; open them before changing guidance.

## Rules (RFC 2119)

- Markdown **MUST** comply with all active markdownlint rules (MD001–MD059 except MD013 per config); lint warnings **MUST** be treated as build blockers. Why: Ensures consistent, accessible docs.
- Contributors **MUST NOT** disable/suppress/reconfigure markdownlint rules (including inline `markdownlint-disable`) unless explicitly instructed for a single case. Why: Prevents standards erosion.
- Content **MUST** use GitHub Flavored Markdown and **MUST** render correctly on GitHub. Why: Primary consumption channel.
- Authors **MUST** run markdownlint locally and fix all findings before submitting. Why: Catches issues early.
- Authors **SHOULD** prefer plain Markdown over inline HTML and **SHOULD** keep content accessible (descriptive links, alt text, semantic headings). Why: Portability and accessibility.

## Scope and Audience

All Markdown authors/reviewers.

## At-a-Glance Quick-Start

- Structure with a single top-level heading; keep lists/tables/fences separated by blank lines.
- Provide alt text and meaningful link text.
- Lint: `npx markdownlint-cli2 "**/*.md"` (or use configured runner); fix findings instead of suppressing.

## Core Principles

- Fix-at-source keeps docs clean and CI reliable.
- Accessibility is part of quality.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
