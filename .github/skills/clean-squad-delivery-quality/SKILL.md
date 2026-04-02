---
name: clean-squad-delivery-quality
description: 'Implementation and QA discipline for Clean Squad. Use when producing increments, validating quality gates, or reviewing release readiness for delivery work.'
user-invocable: false
---

# Clean Squad Delivery Quality

Use this skill for implementation, testing, QA, and commit-discipline work.

## Increment discipline

1. Work in small, reviewable increments.
2. Prefer red-green-refactor where practical.
3. Keep each increment buildable and testable before moving on.

## Validation expectations

- Run the narrowest relevant validation first.
- Escalate to broader build and test coverage before declaring the increment complete.
- Record any residual risk explicitly instead of implying green status.

## Delivery guardrails

- No hidden warnings.
- No speculative refactors outside scope.
- No undocumented quality-gate exceptions.
- No direct final-go/no-go decision when River owns workflow progression.
