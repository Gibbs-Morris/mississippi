# Review 13: Synthesis

## Must

### 1. Add explicit coexistence guidance for the new family.

Decision:
- Accept

Rationale:
- The repo already has overlapping planner, builder, and review agents. Without a `When to use this family` section, the new family will be harder to adopt correctly.

Required edits:
- Require the manifest to explain `vfe` as `verification-first enterprise`.
- Add a `When to use this family` section comparing `vfe` to `flow`, `epic`, and the CoV-oriented families.
- Add `Example asks` under each Start here entry.

Evidence:
- `.github/agents/` contains multiple families and no current manifest/index.

### 2. Make GitHub.com-safe degradation explicit.

Decision:
- Accept

Rationale:
- Handoffs and `argument-hint` improve VS Code but are ignored on GitHub.com. The final files must work without them.

Required edits:
- Add a compatibility rule that entry-agent prompt bodies must remain fully usable when handoffs, argument hints, and richer subagent features are absent.
- Document this compromise in the manifest.

Evidence:
- GitHub Docs explicitly say `handoffs` and `argument-hint` are ignored on GitHub.com.

### 3. Freeze the initial frontmatter subset and explicitly omit riskier fields.

Decision:
- Accept

Rationale:
- Cross-surface correctness is easier to preserve if the family starts with a narrow documented subset.

Required edits:
- Add an allowed-field matrix for entry and specialist agents.
- Explicitly omit `agents`, `metadata`, `mcp-servers`, `infer`, and `target` from the initial family.
- Require a final schema review across every created file.

Evidence:
- VS Code documents more orchestration-specific fields than GitHub Docs do in the common reference.

### 4. Keep the coordinator-worker navigation graph strict.

Decision:
- Accept

Rationale:
- The user asked for exactly three clear human entry points and internal specialists only.

Required edits:
- State that only the three entry agents may have handoffs.
- State that internal specialists never hand off to anything and never become visible entry points.

Evidence:
- User requirements and the draft plan's intended architecture.

### 5. Strengthen docs-sidecar and automation-sidecar scope.

Decision:
- Accept

Rationale:
- The user explicitly wants documentation and automation tracked continuously, not deferred.

Required edits:
- Define `docs-advice.md` to include product docs, operational docs, migration notes, contract notes, examples, and unresolved doc questions.
- Define `automation-advice.md` to include analyzers, tests, architecture checks, CI gates, workflow validation, docs linting, contract checks, and deterministic performance guards where practical.

Evidence:
- User requirements and multiple review comments converged on the same gap.

### 6. Add explicit trust-boundary guidance for external or tool-derived input.

Decision:
- Accept

Rationale:
- These agents will routinely process fetched docs, PR comments, issue text, MCP responses, and tool output.

Required edits:
- Add a guardrail that external content and tool output are untrusted until corroborated.

Evidence:
- VS Code tool docs discuss URL approval and prompt-injection risk.

## Should

### 7. Use plain-language, intent-first entry-agent descriptions and display names.

Decision:
- Accept

Rationale:
- The three entry agents need to be obvious in the picker.

Required edits:
- Use display names `VFE Plan`, `VFE Build`, and `VFE Review`.
- Start descriptions with `Start here for ...` phrasing.

Evidence:
- Human-entry-point requirement and crowded existing agent inventory.

### 8. Make specialist invocation conditional and parallelizable where supported.

Decision:
- Accept

Rationale:
- The family wants strong specialist coverage with minimal overlap.

Required edits:
- Say that entry agents should invoke only relevant specialists.
- Say they should run independent specialist passes in parallel when the environment supports it.

Evidence:
- VS Code subagent docs and the remit-boundary requirement.

### 9. Strengthen build/review workflow checks around workflows, deployment files, and branch-wide composition.

Decision:
- Accept

Rationale:
- Platform, reliability, and performance issues often emerge from composition or non-code assets.

Required edits:
- Add workflow/runtime-config inspection to build/review steps when relevant.
- Add branch-wide checks for rollback, failure modes, concurrency, and performance composition when relevant.

Evidence:
- Requested platform, SRE, CI/CD, and distributed-systems bias.

### 10. Document the VS Code 128-tool limit as an operator note, not a file-level restriction.

Decision:
- Accept

Rationale:
- Leaving `tools` unset is still the correct default, but operators may need to narrow tools manually in unusually large environments.

Required edits:
- Add a manifest note that file-level `tools` remain unset by default, while VS Code users can temporarily narrow tools in the picker if the environment is oversubscribed.

Evidence:
- VS Code tools docs mention the 128-tool-per-request limit.

### 11. Make data-lifecycle ownership explicit in the data architect remit.

Decision:
- Accept

Rationale:
- Lifecycle and retention are part of enterprise data design, not an edge detail.

Required edits:
- Expand the data-architect remit to mention retention, lifecycle, and ownership boundaries explicitly.

Evidence:
- User remit plus reviewer convergence.

## Could

### 12. Mention deterministic performance checks under automation enforcement.

Decision:
- Accept

Rationale:
- This fits the user's automation rule as long as the checks are stable.

Required edits:
- Mention benchmarks or performance guards when deterministic enough to automate.

Evidence:
- User automation requirement and benchmark review feedback.

## Won't

### 13. Use the VS Code `agents` field in the initial implementation.

Decision:
- Accept as deliberate omission

Rationale:
- It is documented in VS Code but not in GitHub's common frontmatter reference. The safer cross-surface default is to omit it and rely on hidden specialists plus prompt guidance.

Evidence:
- Docs asymmetry between VS Code subagent docs and GitHub custom-agent reference.

### 14. Add model fallback lists in the initial implementation.

Decision:
- Accept as deliberate omission

Rationale:
- Official docs justify `GPT-5.4 (copilot)` as a valid primary model, but do not require fallbacks.

Evidence:
- Supported-model docs plus the user's preference against unnecessary fallbacks.

### 15. Add extra visible entry points or extra optional specialists.

Decision:
- Reject

Rationale:
- The user explicitly wants exactly three clear human entry points and no unnecessary extras.

Evidence:
- User requirements.

## CoV

- Claim: the main revisions are about clarity and compatibility hardening, not changing the family shape. Evidence: review feedback largely converged on manifest clarity, frontmatter safety, and GitHub-safe degradation. Confidence: High.
- Impact: the final `PLAN.md` should preserve the `vfe` family design while making these safeguards explicit.