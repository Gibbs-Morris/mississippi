---
name: "clean-squad-troubleshoot"
description: "Troubleshoot Clean Squad customization loading, hooks, prompt, skill, or subagent issues."
argument-hint: "Describe the symptom, for example: worker appears in picker, hook did not run, or prompt could not find a tool set."
agent: "agent"
tools: ["clean-squad-reader", "clean-squad-review", "read", "search", "execute"]
---

Use [WORKFLOW](../clean-squad/WORKFLOW.md) and the agent frontmatter in `.github/agents/cs-*.agent.md` to diagnose the reported symptom.

Produce:

1. likely cause
2. concrete checks already performed
3. recovery steps
4. whether the issue is in authoritative workflow, executable metadata, optional ergonomics, or preview-hook behavior

If the named Clean Squad tool sets are unavailable in this VS Code profile, continue with the remaining built-in tools.
