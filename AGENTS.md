---
applyTo: '**'
---

# Agents

Governing thought: Agents follow repository instructions and iterate from correctness to quality to performance.

> Drift check: Review `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` before relying on this summary.

## Rules (RFC 2119)

- Agents MUST read `.github/copilot-instructions.md` first, then all instruction files under `.github/instructions/`, before making changes. Why: Ensures all work follows the repository's authoritative policies and conventions.
- Agents MUST follow every rule and guideline in those documents when planning or writing code. Why: Keeps contributions consistent, reviewable, and compliant with quality gates.
- Agents MUST follow the "make it work, make it right, make it fast" loop: get tests passing first, then refactor for clarity and correctness, then optimize only where needed. Why: Surfaces issues early and avoids premature optimization.
- Agents SHOULD consult `docs/key-principles/` and apply the concepts within when planning, writing, reviewing, or documenting work. Why: These documents capture the team's foundational thinking, reasoning frameworks, and quality standards.

## Scope and Audience

All agents working in this repository.

## At-a-Glance Quick-Start

- Read `.github/copilot-instructions.md`, then all `.github/instructions/*.instructions.md`.
- Consult `docs/key-principles/` for foundational thinking and reasoning frameworks.
- Prioritize correctness first, cleanup next, and performance improvements last.

## Key Principles Knowledge Base

`docs/key-principles/` contains reference documents that define the team's core thinking, reasoning processes, and quality standards. Agents SHOULD apply these concepts whenever they are relevant to the work at hand.

| Document | Topic |
|----------|-------|
| `minto-pyramid-principle.md` | Minto Pyramid structured communication |
| `first-principles-thinking.md` | First-principles reasoning and decomposition |
| `chain-of-verification.md` | Chain-of-Verification (CoVe) for factual accuracy |
| `clean-code.md` | Clean Code principles and SOLID design |
| `clean-agile.md` | Clean Agile values and practices |
| `agile-sdlc.md` | Agile SDLC, Three Amigos, and shift-left testing |
| `pull-request-best-practices.md` | PR authoring and review responsibilities |
| `architecture-decision-records.md` | ADR format, lifecycle, and best practices |
| `rfc-2119.md` | RFC 2119 requirement-level keywords |
| `github-copilot-agents.md` | GitHub Copilot agent extensibility model |
| `markdown.md` | Markdown authoring (CommonMark and GFM) |
| `mermaid.md` | Mermaid diagram types and syntax |

## Procedures

Use PowerShell to review instruction files:

```powershell
Get-ChildItem -Path .github -Recurse -Filter "*.instructions.md" |
    Sort-Object FullName |
    ForEach-Object { Get-Content -Path $_.FullName -Raw }
```

## Core Principles

- Authoritative instructions come first.
- Key principles inform thinking; apply them when planning, coding, and reviewing.
- Iterative delivery reduces risk.
