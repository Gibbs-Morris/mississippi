---
applyTo: '**'
---

# Pull Request Review Coaching Guide

Governing thought: This document coaches reviewers to lead elite-quality pull request outcomes across the Mississippi codebase through disciplined, high-signal reviews that protect architecture, testing depth, and documentation standards.

## Rules (RFC 2119)

- Reviewers **MUST** pause reviews when diffs exceed ~600 changed lines and coach authors to split before continuing.  
  Why: Keeps cognitive load manageable and prevents mixed-concern changes.
- Reviewers **MUST** fail reviews when L0 tests are missing for new code paths.  
  Why: Every behavior change requires high-quality unit test coverage.
- Reviewers **MUST** verify that authors ran `pwsh ./go.ps1` or targeted quality scripts before approving.  
  Why: Ensures all quality gates passed before merge.
- Reviewers **MUST** enforce single-responsibility PRs; each submission **MUST** own one change narrative.  
  Why: Prevents bundled changes that mix refactors, features, and cleanup.
- Reviewers **SHOULD** request splits when size, mixed concerns, or cross-cutting edits appear.  
  Why: Maintains focused, reviewable changes.
- Reviewers **SHOULD** offer actionable alternatives rather than vague feedback.  
  Why: Helps authors understand and implement improvements quickly.
- Reviewers **SHOULD** balance critique with reinforcement.  
  Why: Teammates learn which patterns to repeat alongside what to fix.

## Scope and Audience

**Audience:** Code reviewers conducting pull request reviews in the Mississippi repository.

**In scope:** Review workflow, coaching techniques, quality focus points, feedback craft.

**Out of scope:** Specific technical implementations, detailed testing strategies (see testing.instructions.md).

## At-a-Glance Quick-Start

- Scan description, linked work items, and screenshots before touching the diff.
- Check the description labels the change as a new feature, maintenance, or fix and frames a single outcome for the PR.
- Pause if the diff exceeds ~600 changed lines (add/remove combined). Coach the author to split before review continues.
- Confirm the author ran `pwsh ./go.ps1` or the targeted quality scripts and shared outcomes.
- Read commits in logical order; keep feedback focused on behavior, tests, and docs.
- Record TODOs and risks while you read so the summary stays crisp.

> **Drift check:** When you reference build or test commands in review feedback, open the script under `eng/src/agent-scripts/` to confirm its current switches and behavior. Scripts remain authoritative; this guide covers coaching posture.

## Purpose

This guide provides a framework for conducting thorough, constructive pull request reviews that maintain code quality standards while mentoring team members.

## Core Principles

- **Enforce single-responsibility PRs.** Ensure each submission owns one change narrative; coach authors to spin off enabling refactors or follow-up features instead of bundling them here.
- **Coach for small batches.** Fewer than 600 touched lines keeps cognitive load manageable; push harder if changes mix concerns.
- **Mentor on SOLID, DRY, KISS every time.** Ask how the change honors single responsibility, avoids duplication, and stays simple.
- **Treat tests as first-class citizens.** Every behavior change needs high-quality L0 coverage and, where appropriate, deeper layers per `.github/instructions/testing.instructions.md`.
- **Demand explicit documentation.** Require updated XML docs, README snippets, and inline comments where future readers need context.
- **Stay friendly, stay direct.** Celebrate wins, but flag regressions or risky choices unequivocally.

## Review Workflow

1. **Absorb intent.** Validate the description, linked issues, acceptance criteria, and screenshots tell a coherent story, clearly state whether this is a feature, maintenance, or fix PR, and explain why the work fits a single responsibility.
2. **Assess scope.** If size, mixed concerns, or cross-cutting edits appear, request a split before proceeding. Offer a suggested slicing plan.
3. **Replay the safety checks.** If artifacts or logs are missing, ask for the latest results from `pwsh ./go.ps1` or project-scoped scripts.
4. **Walk the architecture.** Ensure the design maintains SOLID boundaries, keeps DI seams clean, and respects logging/service-registration rules.
5. **Inspect tests and docs.** Confirm tests cover new paths, mutation risk, and edge cases; ensure docs and changelogs match the behavior shift.
6. **Summarize with intent.** Close with what must change, what looks great, and the next validation step before approval.

## Quality Focus Points

### Architecture & Design

- Watch for leaked responsibilities, missing abstractions, or new inheritance where composition would do.
- Require dependency injection to follow the private get-only pattern; call out any service registrations that drift from `.github/instructions/service-registration.instructions.md`.
- Verify logging additions route through LoggerExtensions and respect `.github/instructions/logging-rules.instructions.md`.

### Tests & Verification

- Expect L0 tests for every code path and fail the review if they are missing.
- Ask for additional L1/L2/L3 coverage when infrastructure seams, async flows, or UI pieces are touched.
- Check mutation expectations: survivors must be killed or justified per `.github/instructions/mutation-testing.instructions.md`.

### Documentation & Communication

- Look for README, changelog, or deployment note updates when public behavior shifts.
- Ensure XML docs and inline comments remain factual and align with `.github/instructions/naming.instructions.md` comment rules.
- Require PR descriptions to note side effects, rollout steps, and monitoring plans.

## PR Size Coaching

- Treat >600 changed lines as a hard stop unless there is a one-off emergency fix approved by the team.
- Suggest slicing strategies: feature toggles, schema-first PRs, contract stubs, or parallel documentation PRs.
- Encourage authors to land enabling refactors separately before feature work starts.

## Feedback Craft

- Lead with curiosity: ask clarifying questions when intent is unclear before rejecting changes.
- Offer actionable alternatives ("extract a validator", "move integration wiring into `ServiceRegistration`") rather than vague "this feels off".
- When something violates policy (tests missing, logging skipped, size too large), be direct: name the rule and state the expectation to resolve it.
- Balance critique with reinforcement so teammates know which patterns to repeat.

## Anti-Patterns to Call Out Immediately

- Changes that bundle refactors, feature work, and clean-up in one PR.
- Lack of tests for new logic or mutation survivors left unresolved.
- Direct `ILogger` usage, service registrations scattered outside approved extension methods, or DI properties breaking the get-only rule.
- Documentation debt: missing release notes, stale XML docs, or unexplained migrations.
- Silent behavioral shifts without telemetry, metrics, or logging to observe the rollout.
