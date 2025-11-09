---
applyTo: ".github/**/*"
---

# PR Review Coaching

## Scope
Reviewer guidance only. Minimal to avoid over-application.

## Core Principles
Single-responsibility PRs. <600 touched lines. SOLID + tests + docs. Check for split opportunities. Verify quality scripts ran (`./go.ps1`). Friendly but direct feedback.

## Focus Areas
Architecture (SOLID, DI seams), tests (L0+ coverage, mutation), docs (XML, README, changelog), PR size (suggest splits).

## Anti-Patterns
❌ Bundled concerns. ❌ Missing tests. ❌ Size >600 lines. ❌ Silent behavior changes.

## Enforcement
Review checklists: scope, size, tests, docs, scripts run. Flag regressions or risks clearly.
