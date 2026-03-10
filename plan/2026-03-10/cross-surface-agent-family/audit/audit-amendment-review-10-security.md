# Amendment Review 10: Security Engineer

- Finding: restricting subagent use to `vfe` agents by default reduces prompt-routing ambiguity and accidental delegation to agents with broader or weaker guardrails. Proposed change: none. Evidence: updated subagent restrictions in prompt and visibility sections. Confidence: High.
- Finding: requiring review reruns after material plan changes reduces the chance of unreviewed security scope drift during implementation. Confidence: High.
