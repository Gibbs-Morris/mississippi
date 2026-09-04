# Review 09: Developer Experience Reviewer

## Findings

- **High — Existing `.github/skills` references contradict the target.** Make
  #540 the explicit cutover owner and add a zero-stale-reference check.
- **High — Activation needs executable routing fixtures.** Require canonical,
  paraphrase, incomplete, negative, and collision prompts with expected route,
  arbitration, and thresholds.
- **High — Missing-input and unsupported-host messages are unspecified.** Add
  templates naming the missing value, example, and next valid action.
- **High — Rollback is not yet an operational procedure.** Test markers,
  revert order, duplicate-root detection, host restoration, and discovery after
  rollback.
- **Should — Add a generated user-facing catalogue and onboarding path.** Include
  outcome, triggers, invocation, inputs/outputs, fallback, owner, and retired
  mappings in README/Docusaurus.
- **Should — Rationalization must validate picker metadata.** Retained agents
  need clear display names, descriptions, handoffs, invocability, and smoke
  tests; internal roles stay hidden.

## Strengths

Outcome-oriented boundaries, invariant placement, staged migration, broad
negative tests, and tiered validation are good DX choices.

## CoV

The review used current agent metadata, README, key-principles documentation,
skill-builder rules, and the draft's activation/rollback sections.
