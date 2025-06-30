# go.ps1 - Convenience wrapper to run the master orchestration script from the repository root

<#
    go.ps1 – Top-level build & test orchestrator

    What happens when you run it:
    1. Delegates to scripts/orchestrate-solutions.ps1 (all args are forwarded).

    orchestrate-solutions.ps1 performs the following, in order, with fail-fast semantics:

      Mississippi solution
      ─────────────────────
      • build-mississippi-solution.ps1
            – Restores local dotnet tools (e.g., GitVersion, SlnGen).
            – dotnet restore mississippi.slnx
            – dotnet build mississippi.slnx (Release by default).
      • unit-test-mississippi-solution.ps1
            – Restores tools & packages.
            – dotnet test mississippi.slnx → TRX results under ./test-results.
      • mutation-test-mississippi-solution.ps1
            – Generates mississippi.sln from mississippi.slnx using SlnGen.
            – Runs Stryker.NET mutation tests on the generated solution → reports in ./StrykerOutput.
      • clean-up-mississippi-solution.ps1
            – Generates mississippi.sln via SlnGen.
            – Runs ReSharper CleanupCode (Built-in: Full Cleanup) across the solution.

      Sample solution
      ───────────────
      • build-sample-solution.ps1
            – Restores tools & packages.
            – Builds samples.slnx.
      • unit-test-sample-solution.ps1
            – Executes unit tests for samples.slnx → results in ./test-results.
      • clean-up-sample-solution.ps1
            – Generates samples.sln then runs ReSharper CleanupCode for code style cleanup.

      Final gate
      ───────────
      • final-build-solutions.ps1
            – Performs a strict (warnings-as-errors) Release build of BOTH solutions to ensure merge readiness.

    Net result
    ───────────
    • Clean, reproducible build pipelines for both solutions.
    • All unit tests executed.
    • Mutation testing executed for Mississippi solution.
    • Automated code cleanup applied.
    • Final safety build with warnings treated as errors.

    Usage:  pwsh ./go.ps1 [additional-args]
            Any parameters you pass are forwarded to orchestrate-solutions.ps1 (and thus to the underlying steps).
#>

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Args
)

# Resolve the path to the real script located in the scripts sub-directory
$orchestrateScript = Join-Path -Path $PSScriptRoot -ChildPath "scripts/orchestrate-solutions.ps1"

if (-not (Test-Path $orchestrateScript)) {
    Write-Error "Could not find orchestrate-solutions.ps1 at expected path: $orchestrateScript"
    exit 1
}

& $orchestrateScript @Args 