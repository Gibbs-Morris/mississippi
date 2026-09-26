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
