---
applyTo: '**'
---

# Agents

Governing thought: Agents follow repository instructions and iterate from correctness to quality to performance.

> Drift check: Review `.github/copilot-instructions.md` and discover applicable `.github/instructions/*.instructions.md` using the instruction-loading procedure below before relying on this summary.

## Rules (RFC 2119)

- Agents MUST read `.github/copilot-instructions.md` first, then all globally scoped and task-applicable instruction files under `.github/instructions/`, before making changes. Why: Preserves authoritative requirements without preloading unrelated bodies.
- Agents MUST follow every rule and guideline in those documents when planning or writing code. Why: Keeps contributions consistent, reviewable, and compliant with quality gates.
- Instruction selection MUST cover the task's edited, reviewed, and generated content, languages, frameworks, and role/workflow, not just changed filenames. Why: A C# example or runtime explanation still needs its relevant guidance.
- Agents MUST read an instruction whose applicability is unclear, whose metadata is missing or unrecognized, or whose scope may overlap the task before excluding it. Why: Uncertainty cannot silently remove requirements.
- Agents MUST reassess instruction selection when the task expands. Why: Initial context selection does not cover requirements introduced by later work.
- Agents MUST follow relevant policy references and explicit skill routes. Why: Selected guidance can require additional task-specific instructions.
- Agents MUST follow [token efficiency and reassessment](.github/instructions/agent-efficiency.instructions.md), including during persistent goals. Why: Repeated effort needs new evidence or a better approach while preserving the full outcome and required gates.
- Agents MUST follow the [mutation-testing policy](.github/instructions/mutation-testing.instructions.md): report results and significant gaps, prioritize correctness and strong unit tests, and keep mutation work proportionate. Why: Mutation testing is an additional quality signal with no mandatory repository score threshold or ordinary completion gate.
- Agents MUST follow the "make it work, make it right, make it fast" loop: get tests passing first, then refactor for clarity and correctness, then optimize only where needed. Why: Surfaces issues early and avoids premature optimization.
- Agents SHOULD consult `docs/key-principles/` and apply the concepts within when planning, writing, reviewing, or documenting work. Why: These documents capture the team's foundational thinking, reasoning frameworks, and quality standards.
- Agents MUST follow [PR size and stacked delivery](.github/instructions/pr-size-and-stacking.instructions.md), including the 600-line target, justified exceptions, and the CI/review gate before starting the next dependent PR. Why: Small, complete changes keep review manageable.
- Agents MUST use the [gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md) for stack work. Why: Native GitHub stacks need correct branch placement and lifecycle commands.

## Scope and Audience

All agents working in this repository.

## At-a-Glance Quick-Start

- Read `.github/copilot-instructions.md`, then discover instruction scopes and load all global plus task-applicable guidance.
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

## Instruction Loading

Discover current instruction names and `applyTo` metadata before selecting
bodies; do not use a fixed filename allowlist:

```powershell
rg --files --glob '*.instructions.md' .github/instructions
rg --line-number --max-count 1 --glob '*.instructions.md' '^applyTo:' .github/instructions
```

PowerShell fallback when `rg` is unavailable:

```powershell
$instructionFiles = Get-ChildItem -LiteralPath .github/instructions -Recurse -File -Filter '*.instructions.md' |
    Sort-Object FullName
$instructionFiles.FullName
Select-String -LiteralPath $instructionFiles.FullName -Pattern '^applyTo:' -List
```

The file inventory includes candidates even when the scope search returns no
match. The scope search reports candidate lines; it does not parse YAML.
Check each candidate against the opening YAML frontmatter's `---` delimiters.
A line number alone does not prove that a match is metadata. Treat missing or
invalid delimiters and matches outside that block as unknown scope and inspect
the file directly.
Honor additional host-supplied guidance and scoped `AGENTS.md` files for the
task's directories; this procedure does not replace their discovery or precedence.

1. Read every instruction with global `applyTo: '**'` in full. Global scopes
   remain global; selection does not weaken their rules or quality gates.
2. Read instructions matching repository-relative task paths and all relevant
   content/domain scopes. Include files being reviewed, examples being written,
   questions about runtime behavior, and any active agent workflow. Use the
   metadata, filenames, and the file's stated scope together; a path match is
   sufficient to include guidance, not the only reason to include it.
3. Follow the selected policies' relevant references and skill routes. Read the
   relevant skill and supporting reference when its task applies, using the
   linked file directly if the host cannot discover skills. Avoid loading every
   skill body or every unrelated reference as a startup checklist.
4. If discovery is incomplete, metadata cannot be interpreted, or scope is
   uncertain, inspect the candidate files directly. Do not treat a denied read
   or a missing match as proof that no instruction applies.
5. Reuse already-read, unchanged guidance. Refresh selection when new paths,
   languages, frameworks, or workflow requirements enter the task.

Specialized full-inventory duties, such as rule maintenance and overlapping-scope
conflict checks, still apply when that work is requested. This procedure reduces
irrelevant context loading; it does not change a policy's scope or obligation.

## Core Principles

- Authoritative instructions come first.
- Key principles inform thinking; apply them when planning, coding, and reviewing.
- Iterative delivery reduces risk.
