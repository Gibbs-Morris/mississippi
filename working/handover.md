# Builder Program Handover

This file keeps the important rules and architectural guardrails that must survive across builder slices.

Use it for concise persistent guidance, not for day-by-day progress logging.

## Current rules

### 1. Reservoir builder contracts live in abstractions

- Rule: `IReservoirBuilder` and `IFeatureStateBuilder<TState>` live in `Reservoir.Abstractions`.
- Why: both `Reservoir.Core` and `Reservoir.Client` need to extend the same public contract surface without owning the root contract themselves.
- Applies to: Reservoir builder contract placement, public API design, and future extension methods.
- Consequence for next slices: `Reservoir.Core` adds reducer/effect/feature-state extensions over the abstractions contracts, and `Reservoir.Client` adds built-in and future DevTools extensions over the same contracts.

### 2. Host attach stays thin and client-owned

- Rule: the `WebAssemblyHostBuilder` attach extension belongs in `Reservoir.Client` and should remain a thin edge adapter.
- Why: the host-specific entrypoint should not own reusable contracts or business composition semantics.
- Applies to: startup composition, host integration, and future reuse by a shared client builder.
- Consequence for next slices: descriptor collection and validation logic should stay reusable behind the host attach layer.

### 3. Direct consumer changes must stay narrowly scoped

- Rule: non-Reservoir edits are allowed only when they are direct compile-preserving adaptations required by removed Reservoir public APIs.
- Why: the broader PR is builder-focused, and scope creep will make it harder to finish all builder work in one PR.
- Applies to: Spring sample helpers, `Inlet.Client`, `Inlet.Client.Generators`, and similar direct dependencies.
- Consequence for next slices: update dependent consumers only as much as needed to keep the branch compiling and aligned with the new builder contracts.

### 4. Working docs are persistent program state

- Rule: `working/index.md`, `working/notes.md`, and this file must be kept current until the user explicitly says the builder effort is complete.
- Why: transient `plan/` folders are deleted, so persistent context must live here.
- Applies to: every future planner and builder slice in this PR.
- Consequence for next slices: record progress in `working/notes.md` and copy durable rule-level guidance into `working/handover.md`.

### 5. Do not add new csproj dependencies for this builder work

- Rule: do not add any new package or project dependencies to any `.csproj` as part of this builder plan unless the user explicitly changes that rule later.
- Why: this PR is meant to build out the builder model and remove legacy registrations without widening the dependency graph.
- Applies to: all implementation slices under this broader builder effort.
- Consequence for next slices: solve the work using existing packages/projects already in the repo and treat any proposed dependency addition as blocked until the user approves it explicitly.

### 6. Replace obsolete composition helpers at the owning package boundary

- Rule: when a removed Reservoir API exposes obsolete downstream startup helpers, add the new builder-first entrypoint in the owning package instead of hard-coding equivalent DI in samples.
- Why: this keeps the new composition story reusable, removes warnings at the root, and avoids sample-specific service wiring drift.
- Applies to: `Inlet.Client`, generated client registrations, and future host-specific builder migrations.
- Consequence for next slices: prefer builder-first replacement APIs plus generator output updates over inline sample-only workarounds.
