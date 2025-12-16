---
applyTo: '**'
---

# Build Issue Remediation Protocol

Governing thought: Fix each warning/error with minimal, bounded attempts while keeping the build clean.

## Rules (RFC 2119)

- One issue = one specific warning/error location; agents **MUST** attempt at most five focused fixes before deferring and **MUST** leave the code compiling when deferring.
- Edits **MUST** stay minimal (no broad refactors). Rule severity **MUST NOT** be relaxed; analyzers/NoWarn **MUST NOT** be removed or downgraded. Generated code **MUST NOT** be edited; use the narrowest local suppression only when unavoidable and justified. `[SuppressMessage]` **MUST NOT** be added without approval.
- Package versions **MUST NOT** be added to project files; work **MUST** follow `.editorconfig` and shared props/targets.
- Deferred items **MUST** be captured as `.scratchpad/tasks` entries with `status=deferred` and context. 
- Use repo scripts to reproduce and verify (`build-mississippi-solution.ps1`, `clean-up-mississippi-solution.ps1`, `unit-test-mississippi-solution.ps1`, `mutation-test-mississippi-solution.ps1`, `go.ps1`); mutation runs may take ~30 minutes and **MUST NOT** be cancelled.

## Quick Start

- Build/clean/test to list issues, then fix the smallest/highest-impact items (unused code, nullability, dispose, async).
- Use only narrow `#pragma warning disable/restore` when explicitly approved; restore immediately and document justification.

## Review Checklist

- [ ] ≤5 attempts per issue; scope minimal; compiling state preserved; deferred issues logged in scratchpad.
- [ ] No severity relaxations, package versions, or unapproved suppressions.
- [ ] Repo scripts used; zero-warnings policy respected.
