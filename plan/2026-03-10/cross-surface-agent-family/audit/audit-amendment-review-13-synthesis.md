# Amendment Review 13: Synthesis

## Must

### 1. Keep subagent delegation inside the `vfe` family by default.

Decision:
- Accept

Rationale:
- This is the core user requirement for the amendment and aligns with the self-contained coordinator-worker design.

Evidence:
- Updated user instruction.
- Updated final plan sections for compatibility baseline, visibility model, prompt rules, and verification checklist.

Required follow-through:
- Manifest implementation must explain the rule clearly.
- Entry-agent bodies must not invite generic or non-prefixed agent delegation.

## Must

### 2. Require recorded review reruns for material plan changes.

Decision:
- Accept

Rationale:
- The workflow claims verification-first discipline. Material plan changes must not bypass that gate.

Evidence:
- Updated user instruction.
- Updated final plan section `Plan Amendment Review Rule`.

Required follow-through:
- Entry-agent prompts must encode the stop-update-review-synthesize-continue loop.
- Working-directory artifacts must capture the rerun and synthesis.

## Should

### 3. Keep the rule explainable to users, not only enforceable by prompts.

Decision:
- Accept

Rationale:
- Users need to understand why `vfe` stays self-contained and when explicit override is allowed.

Evidence:
- DX, marketing, and architecture amendment reviews converged on this point.

Required follow-through:
- Manifest should describe the family-only delegation rule and the explicit-user-override exception.

## Should

### 4. Preserve cross-surface compatibility by keeping enforcement in prompt logic and verification, not undocumented frontmatter.

Decision:
- Accept

Rationale:
- This keeps the amendment aligned with the earlier cross-surface strategy.

Evidence:
- Source-generator/tooling amendment review and prior docs verification.

Required follow-through:
- Do not add the VS Code-only `agents` field in implementation unless a new docs verification explicitly justifies it.

## Could

### 5. Mention operational artifact carry-forward during plan-amendment reruns.

Decision:
- Defer

Rationale:
- The current plan already preserves review artifacts and optional operability notes, which is sufficient for this stage.

Evidence:
- Platform amendment review.

## CoV

- Claim: this amendment has completed a full review round for the current plan change. Evidence: twelve amendment review files plus this synthesis artifact exist in the audit folder. Confidence: High.
- Claim: the accepted changes are now reflected in the final plan. Evidence: updated `PLAN.md` sections for delegation guardrails, amendment review rule, working-directory expectations, and verification checklist. Confidence: High.
