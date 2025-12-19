---
applyTo: '**/*.instructions.md'
---

# Instruction Authoring Guide

Governing thought: Every instruction file follows the same concise template—front matter, governing thought, a single Rules section, then supporting detail.

> Drift check: When referencing scripts, open them under `eng/src/agent-scripts/`; scripts remain authoritative.

## Rules (RFC 2119)

- Each instruction **MUST** include YAML front matter with `applyTo`, an H1 title, a one-sentence governing thought, a Drift check note near the top, and a single consolidated **Rules (RFC 2119)** section. Why: Enables predictable parsing.
- RFC 2119 keywords **MUST** stay uppercase and **MUST NOT** appear outside the Rules section except in quoted examples. Why: Prevents accidental policy drift.
- Files **MUST** live in `.github/instructions/` with kebab-case `<topic>.instructions.md`; one cohesive topic per file **SHOULD** be maintained. Why: Improves discoverability.
- Authoring **MUST** use concise, factual US English; Rules bullets **MUST** contain one requirement per sentence with a brief “Why” when not obvious. Why: Reduces tokens and ambiguity.
- Command examples **MUST** reference real scripts/tools; secrets **MUST NOT** appear in content or examples. Why: Keeps docs actionable and safe.
- Changes to instructions **MUST** follow repository review policy and **MUST** be mirrored to Cursor `.mdc` files per sync instructions. Why: Maintains parity across tools.

## Scope and Audience

Anyone creating or updating `*.instructions.md`.

## At-a-Glance Quick-Start

- Copy the standard section order: front matter → title + governing thought → Drift check → Rules → Scope and Audience → Quick-Start → Core Principles → Procedures/Examples/References (as needed).
- Keep RFC 2119 keywords only in Rules; keep prose concise.
- Link to authoritative scripts/configs instead of duplicating details.

## Core Principles

- Predictable structure enables humans and automation to consume instructions.
- Concise, factual wording minimizes tokens and misinterpretation.
- Canonical sources (scripts/configs) trump narrative text.

## References

- RFC keywords: `.github/instructions/rfc2119.instructions.md`
- Sync policy: `.github/instructions/instruction-mdc-sync.instructions.md`
