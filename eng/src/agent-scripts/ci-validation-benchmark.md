# Compare consolidated validation with legacy jobs

Refs [issue #1085](https://github.com/Gibbs-Morris/mississippi/issues/1085).

Use the two additional workflows to compare one shared solution build with the existing independent level jobs.
Mississippi runs L0, L1 and L2. Samples runs L0, authoritative L1 discovery, L2 and Spring L3 Smoke.
An empty Samples L1 inventory reports `NOT_APPLICABLE`; newly declared L1 projects run automatically.
Discovery errors and test failures stop subsequent levels. Mississippi currently declares no L3 project.

Both jobs restore locked dependencies even after a NuGet cache hit, build Release with GitVersion's current
`Version`, no incremental build and warnings as errors, and use the canonical per-project MTP/TRX runner.
Aspire tooling follows L1; Chromium and Linux browser dependencies follow L2. Each Spring suite starts
its own application and containers through the existing fixture and retains its owned cleanup.

## Before you begin

You need GitHub Actions read access and the GitHub CLI to inspect jobs, logs and artifacts.
Collect actual PR-event runs; a new workflow's manual dispatch may be unavailable before it reaches main.
A bounded repeat needs Actions write access. Keep all legacy workflows and protection settings intact.

## Build reuse

The Samples build writes `artifacts/ci-benchmark/build.json` only after a successful build with stable inputs.
`test-spring.ps1 -PrebuiltReceiptPath artifacts/ci-benchmark/build.json -BuildVersion <current-version>`
requires the same Actions run, attempt, job, runner, checkout path, clean source fingerprint, SDK,
configuration, version/build properties and hashed build outputs/dependency assets.
The digest covers every `bin` configuration because a Release solution build can emit Debug outputs for referenced projects.
Missing required Spring outputs or any mismatch fails before browser installation and test execution.
Receipts and `bin`/`obj` are never cached. Sonar-instrumented outputs are rejected.
The normal `test-spring.ps1` invocation still restores and builds its selected test project.
The execution lease, SDK/Docker prerequisites, real results and minimum-test requirement apply to both paths.

## Caches and evidence

NuGet keeps the legacy `~/.nuget/packages` path and exact OS/lockfile-hash key.
GitVersion caches the actual `runner.tool_cache/GitVersion.Tool/6.5.1` subtree by OS, architecture and version;
the pinned GitTools actions still set up the tool and calculate the current version.
Aspire uses `artifacts/tools/aspire-<central-version>` with OS/architecture/version/selected-SDK identity
and verifies the executable's version after restoring the tool cache.

Browser caching is omitted because no separate browser transfer saving has been established.
The historical combined Spring cache has confounded timings and includes unrelated packages/tools.
Playwright also [advises against browser caching](https://playwright.dev/dotnet/docs/ci#caching-browsers)
without evidence that transfer beats installation; Linux system dependencies still need installation.
Only the current generated Playwright installer installs matching Chromium. Its cost remains measurable.

Artifacts retain runner image/version, checked-out SHA, event, SDK, tool versions, project inventory,
build receipt, cache exact-hit states, command phase timings, per-project TRX, and Spring diagnostics.
Cache keys and transfer sizes/times are available in the action logs. An empty cache-hit value means
unavailable or not reached, not a proven miss. The step summary reports command phases even after failure.

## Reproduce the comparison

First select complete successful job sets on the same checked-out SHA and event, with matching solution inventory,
SDK, Release configuration and observed runner image. Check the checkout and setup logs; API head SHA
alone does not prove the checked-out PR merge revision. Record every run ID, job ID and attempt.
Do not assign retained earlier-attempt jobs to a rerun attempt.

Then read jobs and steps for each run attempt using `gh api --paginate repos/Gibbs-Morris/mississippi/actions/runs/<run-id>/attempts/<attempt>/jobs`.
For each job, execution seconds are `([DateTimeOffset]$job.completed_at - [DateTimeOffset]$job.started_at).TotalSeconds`.
Waiting from trigger/request to runner assignment is excluded. Step timestamps include cache transfer,
tooling, build, test, artifact upload and cleanup costs; retained command phases supplement those steps.
Spring's test invocation includes fresh deployment and teardown. Phase medians do not sum to total median.

Finally report each legacy job separately with n, mean and median, then sum the legacy occupied runner seconds
per complete set and compare that resource use with its consolidated job. The longest legacy individual
job is only an overlap proxy. For the two new jobs, sum is resource use and max is a simultaneous-start
estimate; neither represents measured end-to-end feedback with unequal starts.
Separate cache hit/miss observations, show actual test counts, and link every source run/job.

## Verify the result

Check retained TRX and Spring `PASS` summaries against the declared project inventory and suite.
Samples L1 must have either executed projects or valid `NOT_APPLICABLE` evidence; prerequisite `READY` is not a test pass.
Exclude failed and incomplete sets from successful duration statistics, while retaining their diagnostics.

The legacy L3 job builds only the selected Spring test project without the L0–L2 GitVersion override
and no-incremental flag. The new Samples job builds the entire solution with those properties.
Record that unavoidable difference, and any `ubuntu-latest` versus `ubuntu-24.04` image mismatch.
One complete paired set plus one bounded same-head repeat is provisional evidence, not a large sample.
Keep existing checks and rules intact; this experiment grants no merge or queue activation authorization.
