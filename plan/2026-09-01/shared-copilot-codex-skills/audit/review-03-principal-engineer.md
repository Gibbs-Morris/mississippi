# Review 03: Principal Engineer

## Findings

- **Blocking — The root/path decision must be contract-gated.** Existing
  repository guidance still makes `.github/skills` authoritative; #540 must
  update the skill-builder and all consumers before #544. Confidence high.
- **Blocking — Evaluation is not operational without a runner, oracle, schema,
  thresholds, and CI entry point.** Make #543 concrete and replayable.
  Evidence: no existing skill harness; PowerShell/Pester is the repository
  automation pattern; confidence high.
- **High — Pilot go/revise/stop must control dependencies.** A revise or stop
  result must invalidate or defer downstream work, not merely be recorded.
  Confidence high.
- **High — Security boundaries must account for privileged gh-aw.** Live
  validation must use an isolated fixture checkout, least privilege, and no
  `--allow-all-tools`/`--allow-all-paths` against untrusted PR content.
  Confidence high.
- **Medium — Gates need path-based proportionality.** Guidance-only changes
  should not inherit runtime mutation gates; README is user-facing while
  `global.json` and scripts select executable SDK/tools. Confidence high.

## Strengths

The plan correctly separates invariants, workflow skills, host agents, and
deterministic enforcement and preserves reversible strangler sequencing.

## CoV

The assessment used issue requirements, repository automation and version files,
existing agent contracts, and the completed PR #530 behavior.
