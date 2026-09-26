# Legacy test improvement bindings

Read these local sources with the portable
[improve-legacy-tests skill](../../.agents/skills/improve-legacy-tests/SKILL.md).
They bind the workflow to Mississippi without replacing the instruction Rules.

| Decision | Current local source |
| --- | --- |
| Tests-only authority, immediate warning fixes, touched-path coverage, no-build loop | [Legacy improvement policy](../instructions/test-improvement.instructions.md) |
| xUnit v3, xUnit Assert, MTP, levels, naming, deterministic fixtures, coverage targets | [Testing policy](../instructions/testing.instructions.md) |
| Required build/cleanup/test gates and zero warnings | [Build policy](../instructions/build-rules.instructions.md), [shared guardrails](../instructions/shared-policies.instructions.md) |
| Project identity and package conventions | [Project policy](../instructions/projects.instructions.md), [Directory.Build.props](../../Directory.Build.props), [Directory.Packages.props](../../Directory.Packages.props) |
| Runner, SDK, and coverage configuration | [global.json](../../global.json), [testconfig.json](../../testconfig.json), [.editorconfig](../../.editorconfig) |
| Language and test code conventions when editing C# | [C# policy](../instructions/csharp.instructions.md), [naming](../instructions/naming.instructions.md) |
| Spring test placement and browser contracts when relevant | [Spring testing](../../samples/Spring/TESTING.md), [UX policy](../instructions/ux-validation.instructions.md) |

## Select a focused command

Inspect the target project and current scripts before execution. Restore local
tools with `dotnet tool restore`. The ordinary tests-and-coverage loop is
`pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation`.
The [quality script](../../eng/src/agent-scripts/test-project-quality.ps1) accepts
an existing project path or a resolvable test name. When source mapping is
ambiguous, select the verified source explicitly with `-SourceProject` rather
than guessing from the test name.

After a first clean build, `-NoBuild` can shorten a valid iteration; changed
inputs still need current outputs and a build with `-warnaserror`. Use the
required final commands from the build policy, including full cleanup and
`pwsh ./go.ps1`. Preserve nonempty execution checks and per-project TRX evidence;
do not substitute VSTest-only collector or logger arguments for MTP.

## Reuse reports without hiding gaps

Inspect the selected coverage report and touched-file results. The
[coverage summarizer](../../eng/src/agent-scripts/summarize-coverage-gaps.ps1)
supports `-CoverageReportPath` and optional task emission. Preserve scratchpad
ownership and reconcile target/revision evidence before creating gap tasks.
Its default `-Threshold` is tooling behavior, not an additional policy gate.

For explicit mutation outcomes, follow
[run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md) and
[local mutation bindings](mutation-testing-bindings.md). Existing reports use
[summarize-mutation-survivors.ps1](../../eng/src/agent-scripts/summarize-mutation-survivors.ps1)
with `-SkipMutationRun`; a focused report also needs its verified `-RunPath`.
That switch prevents a new mutation run, but the summarizer still writes
summary files. Assessment-only work reads reports directly and does not invoke
write-producing summarizers. Report significant historical gaps and defer
disproportionate remediation unless requested; no score target is added here.
