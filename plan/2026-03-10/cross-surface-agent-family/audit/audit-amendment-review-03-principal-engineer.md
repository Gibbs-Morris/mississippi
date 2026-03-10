# Amendment Review 03: Principal Engineer

- Issue: The subagent guardrail needs an escape hatch for explicit user intent, otherwise the family could become brittle. Why it matters: rare exceptions should be deliberate, not impossible. Proposed change: accepted in final plan by allowing explicit user override only. Evidence: updated subagent guardrail language in compatibility baseline, prompt rules, and visibility model. Confidence: High.
- Finding: the amendment preserves the existing cross-surface choice to avoid the undocumented `agents` frontmatter dependency. Confidence: High.
