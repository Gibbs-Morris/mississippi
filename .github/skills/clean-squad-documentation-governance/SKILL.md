---
name: clean-squad-documentation-governance
description: 'Documentation governance guidance for Clean Squad. Use when deciding doc scope, coordinating writer and reviewer work, or aligning ADR and C4 outputs with published docs.'
user-invocable: false
---

# Clean Squad Documentation Governance

Use this skill for documentation-scope analysis and documentation delivery.

## Scope rules

1. Decide documentation need from diff-backed evidence, not intuition.
2. Record why documentation is required or skippable.
3. If documentation is skipped, record the reason in `08-documentation/scope-assessment.md`.

## Delivery rules

1. Technical Writer owns final doc authoring.
2. Doc Reviewer owns independent documentation acceptance review.
3. ADR Keeper and C4 Diagrammer support documentation when architecture evidence needs durable publication.

## Evidence rules

- Distinguish verified behavior from implementation detail.
- Prefer code, tests, and approved artifacts over prose-only claims.
- Keep one primary question per page.
