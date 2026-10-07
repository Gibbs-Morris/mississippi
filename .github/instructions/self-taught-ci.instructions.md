---
applyTo: '.github/workflows/*.yml'
---

# Self-Taught Lessons: CI

Governing thought: Validate queue candidates as combined changes against their queue base.

> Drift check: Compare the current event payload, checkout, and workflow regression fixtures before applying this lesson.

## Rules (RFC 2119)

- Merge-group diff selection **SHOULD** compare the verified `base_sha` and `head_sha` for the complete candidate. Why: The cleanup regression fixture with a docs-only follower still requires cleanup of its predecessor's code.

## Scope and Audience

Agents maintaining GitHub Actions workflows that select files for queue validation.

## References

- [Cleanup workflow fixtures](../../eng/tests/agent-scripts/CleanupWorkflow.Tests.ps1)
- [Self-improvement governance](self-improvement.instructions.md)
