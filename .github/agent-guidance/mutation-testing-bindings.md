# Mutation testing local bindings

This repository-specific reference supplies commands, targets, and report
locations for the portable `run-mutation-testing` skill. It is not part of the
portable package and does not change the mutation policy.

## Supported targets and configuration

- The on-demand solution target is `mississippi.slnx`; the solution mutation
  wrapper generates the legacy `.sln` only for Stryker compatibility.
- Focused mutation uses
  `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name>
  [-SourceProject <path>] -Configuration Release` without `-SkipMutation`.
  The script runs the selected test project with coverage first, requires at
  least one executed test and a current Cobertura report, then records a
  per-invocation manifest and mutation report.
- Before that focused command, run
  `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
  -Configuration Release` and stop if it fails. This canonical preflight restores
  tools and packages and builds `mississippi.slnx` with `--no-incremental` and
  `--warnaserror`; the focused quality command alone does not establish that
  warning-free clean build.
- Routine conventional test and coverage validation uses the same script with
  `-SkipMutation`; it does not select mutation execution.
- Solution-wide mutation uses
  `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
  -Configuration Release`. It restores tools, generates the legacy solution,
  restores packages, builds with warnings as errors, and invokes Stryker.
  `pwsh ./go.ps1 -IncludeMutation` is reserved for an explicitly authorized
  full pipeline.
- Existing report inspection uses
  `pwsh ./eng/src/agent-scripts/summarize-mutation-survivors.ps1
  -SkipMutationRun -RunPath <run-directory>`. The selected manifest and reports
  must belong to the requested run and scope; older reports are not fallback
  evidence for a newer incomplete run. `-SkipMutationRun` still writes enriched
  and summary JSON/Markdown, plus optional tasks or skeletons, so report-only
  assessment must inspect raw reports or use the summarizer only when those
  writes are authorized.
- `-NoBuild` is supported by the focused quality script only after the required
  clean preflight has already been established. A switch name does not prove
  that its preconditions or report outputs exist.

The root `.config/dotnet-tools.json` binds the Stryker, coverage, GitVersion,
SlnGen, and cleanup tools used by these scripts. The root `stryker-config.json`
uses complete mutation, per-test coverage, JSON/Markdown/HTML reporting, and
mutator exclusions for generated files and logger-extension files. Its 80/60/50
thresholds and ignored String mutations are tooling behavior, not a repository
acceptance gate.

## Report contract and scope

Mutation output is written below `.scratchpad/mutation-test-results`. A selected
run has `project-results.json` plus one `mutation-report.json` for each declared
target with authored source. A declared no-authored-source target is an explicit
local exemption: the automation may mark it `Skipped` with `Success=true` and
the summarizer omits it, but it was not tested and is not passing evidence for
that target. Each remaining required target is usable for score or survivor
claims only when its manifest status and report path are valid and its report
belongs inside the selected run. Completed and failed tool runs may be analyzed
when their required reports are complete; missing, stale, pending, skipped,
interrupted, or incomplete required targets are reported as such and never pass.

The mutation manifest alone does not record revision provenance. Focused quality
runs also emit `.scratchpad/validation-evidence/<run-id>/evidence.json` with
`HeadRevision`, source fingerprints before/after execution, and the exact mutation
report's path, SHA-256, and length in `ArtifactMetadata`. Associate that record
with the selected report and invocation, check its fields and report hash, and
use `Test-ValidationEvidence` from `ValidationEvidence.psm1` to check reuse.
For every status, including failed runs with complete reports, also require
`SourceBefore.Fingerprint == SourceAfter.Fingerprint == current.Fingerprint`
and `SourceChangedDuringRun=false` before attributing results to that source.
The helper rejects source changes during execution only for PASS records;
`Valid=true`/`Fresh=true` alone can accept a failed run against moving inputs.
Changed or missing before/after/current provenance remains unverified, even
when the report is complete and its recorded hash matches.
That focused fingerprint omits `stryker-config.json`, `.config/dotnet-tools.json`,
and the mutation runner/imported helpers: at least `test-project-quality.ps1`,
`RepositoryAutomation.psm1`, and `ValidationEvidence.psm1` under `eng/src/agent-scripts/`.
Inspect the actual invoked/imported dependency closure and include any additional
execution or report-validation inputs. `Fresh=true` alone does not establish
those inputs' freshness, especially in an already-dirty worktree. Before a run,
record their SHA-256 values with the invocation and compare them on reuse,
or establish clean source-before, source-after and current states at the same
Git revision with all those tracked inputs unchanged. If neither record exists,
report configuration/runner freshness as unverified even when `Test-ValidationEvidence` passes.
The focused `InputPath` is a fixed list of files present when collection starts;
it cannot detect later-added wildcard source/test files in an already-dirty
worktree. Before, after and on reuse, record and compare the authored input
inventory (paths and hashes) for selected source/test project directories and
their referenced projects, including untracked files, additions and deletions.
Include authored inputs outside those directories when the project includes
them; generated build output is not an authored input. Use the actual project
input scope rather than comparing only the recorded fixed list or dirty flag.
Missing or changed inventories leave current-source freshness unverified.
Only claim current-source and configuration freshness with stable source and
inventory proof plus configuration/runner checks supported;
report changed inputs or missing provenance as a gap. A historical report needs evidence for
its historical source, not a guessed revision from the current checkout.
Standalone solution mutation has no equivalent report-bound provenance record,
and `go.ps1`'s outer evidence records coverage artifacts rather than mutation
report hashes. Without separately established invocation/source/report evidence,
their manifests support no revision-specific score claim. Do not infer a revision
from a filename, timestamp, or a passing outer pipeline.

The focused manifest's `Failed` status combines native tool, reporter, and
threshold failures; it does not identify the category. A complete report below
the configured threshold is a score, not proof of a threshold-only exit. Classify
a threshold failure only when the invocation's diagnostics establish that cause
and its required reports are valid; otherwise report a failed run with an
unverified cause. Keep build/test preflight failures separate. The solution
wrapper's explicit `ThresholdFailed` follows successful report validation and
its own threshold comparison; do not infer that status for a focused run.

Current repository policy targets the primary solution's supported projects.
The sample solution is outside this repository's mutation requirement, but this
is a local binding rather than a portable prohibition; the skill must discover
the consuming project's supported targets and policy elsewhere.

## Ownership

The mutation skill owns execution evidence and proportionate survivor analysis.
The test engineer owns test implementation and semantic-consistency evidence.
The build-failure skill owns restore, build, or test failures. The test policy,
quality scripts, report summarizer, and Stryker configuration remain authoritative
for their local behavior.
