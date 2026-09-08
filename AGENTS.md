---
applyTo: '**'
---

# Agents

Governing thought: Agents follow repository instructions and iterate from correctness to quality to performance.

> Drift check: Review `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` before relying on this summary.

## Rules (RFC 2119)

- Agents MUST read `.github/copilot-instructions.md` first, then all instruction files under `.github/instructions/`, before making changes. Why: Ensures all work follows the repository's authoritative policies and conventions.
- Agents MUST follow every rule and guideline in those documents when planning or writing code. Why: Keeps contributions consistent, reviewable, and compliant with quality gates.
- Agents MUST follow [token efficiency and reassessment](.github/instructions/agent-efficiency.instructions.md), including during persistent goals. Why: Repeated effort needs new evidence or a better approach while preserving the full outcome and required gates.
- Agents MUST follow the [mutation-testing policy](.github/instructions/mutation-testing.instructions.md): report results and significant gaps, prioritize correctness and strong unit tests, and keep mutation work proportionate. Why: Mutation testing is an additional quality signal with no mandatory repository score threshold or ordinary completion gate.
- Agents MUST follow the "make it work, make it right, make it fast" loop: get tests passing first, then refactor for clarity and correctness, then optimize only where needed. Why: Surfaces issues early and avoids premature optimization.
- Agents SHOULD consult `docs/key-principles/` and apply the concepts within when planning, writing, reviewing, or documenting work. Why: These documents capture the team's foundational thinking, reasoning frameworks, and quality standards.
- Agents MUST follow [PR size and stacked delivery](.github/instructions/pr-size-and-stacking.instructions.md), including the 600-line target, justified exceptions, and the CI/review gate before starting the next dependent PR. Why: Small, complete changes keep review manageable.
- Agents MUST use the [gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md) for stack work. Why: Native GitHub stacks need correct branch placement and lifecycle commands.

## Scope and Audience

All agents working in this repository.

## At-a-Glance Quick-Start

- Read `.github/copilot-instructions.md`, then all `.github/instructions/*.instructions.md`.
- Consult `docs/key-principles/` for foundational thinking and reasoning frameworks.
- Prioritize correctness first, cleanup next, and performance improvements last.

## Verify a Change Through Spring

1. Run the relevant fast tests with `test-project-quality.ps1 -SkipMutation` while implementing.
2. Run `pwsh ./test-spring.ps1 -Doctor` to check the selected SDK and Docker Linux daemon access.
3. Run `pwsh ./test-spring.ps1` for the L3 smoke suite in `samples/Spring/Spring.L3Tests/Smoke` against a fresh Aspire test host.
4. Read the emitted `SUMMARY` JSON path; `PASS` requires executed, passing tests. `READY` only reports prerequisite checks.
5. Inspect `spring.trx`, `test.log`, resource logs, `banking.png`, and `banking.zip` in that run's artifact directory if validation fails.
6. Run `pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full` for API and authorization contracts, and `pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full` for all browser journeys, then the existing repository quality gates.

Use separate worktrees for simultaneous builds. The test fixture owns its app and containers; avoid process-name cleanup or Docker prune commands.
For prerequisites, diagnostics, and interactive Aspire workflows, see [Spring validation](README.md#validate-spring-after-a-change).
For test placement and CI scheduling, see [Spring test levels and suites](samples/Spring/TESTING.md). Smoke is a suite within a level; browser journeys belong in L3.

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
