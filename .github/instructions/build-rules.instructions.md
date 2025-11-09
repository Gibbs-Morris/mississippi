---
applyTo: "**"
---

# Build & Quality Gates

## Scope
Build/test/mutation scripts. Quality gates. See global for zero-warnings policy and commands.

## Solutions
Mississippi (`mississippi.slnx`): full tests + mutation. Samples (`samples.slnx`): minimal tests, no mutation.

## Quality Gates
Build: zero errors/warnings. Tests: 100% pass. Mutation: maintain/raise score (Mississippi only). Cleanup: ReSharper passes.

## CI Workflows
`full-build.yml`, `unit-tests.yml`, `stryker.yml`, `cleanup.yml`, `sonar-cloud.yml`. All MUST pass for merge.

## Enforcement
See global for script index. Run `pwsh ./go.ps1` before PR. Mutations: 30min runs, wait for completion.
