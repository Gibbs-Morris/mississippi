---
applyTo: '.github/workflows/*.yml'
---

# Self-Taught Lessons: CI

Governing thought: Validate every queued change that has not reached the target branch.

> Drift check: Compare the current event payload, checkout, and workflow regression fixtures before applying this lesson.

## Rules (RFC 2119)

- Merge-group diff selection **SHOULD** compare the candidate with the unique common ancestor of its verified parent and target branch. Why: The payload's `base_sha` can already contain a preceding queue candidate, so a parent-to-head diff can omit queued code.
- Queue regression fixtures **SHOULD** use a preceding synthetic commit as the payload base and distinguish queued changes from changes already on the target branch. Why: A docs-only follower must retain its queued predecessor's cleanup scope without rechecking code already merged.

## Scope and Audience

Agents maintaining GitHub Actions workflows that select files for queue validation.

## References

- [Cleanup workflow fixtures](../../eng/tests/agent-scripts/CleanupWorkflow.Tests.ps1)
- [Self-improvement governance](self-improvement.instructions.md)
