# Builder Program Notes

## 2026-03-11

### Current status

- Created the Reservoir-first builder migration plan in `plan/2026-03-11/reservoir-builder-migration/PLAN.md`.
- Scoped the first builder slice to Reservoir on the current Blazor WebAssembly host builder.
- Included built-in Blazor lifecycle and navigation features in the first slice.
- Kept Reservoir DevTools out of scope for this slice.

### Key reasoning

- Active samples and docs currently compose Reservoir from `WebAssemblyHostBuilder`, so the first builder surface should meet the repo where it is today.
- No checked-in shared `ClientBuilder` or `UseMississippi(...)` contract was found on this branch, so waiting for that would block Reservoir unnecessarily.
- The repo is pre-v1, so replacing the legacy Reservoir registration APIs in-sequence is acceptable as long as in-repo consumers are updated in the same work.
- `IReservoirBuilder` and nested feature-builder contracts should live in `Reservoir.Abstractions`, which lets `Reservoir.Core` expose reducer/effect extensions and `Reservoir.Client` expose built-in or future DevTools extensions over the same shared contract.

### Verified dependencies

- Spring client feature helpers currently call Reservoir legacy reducer/effect registration methods directly.
- `Inlet.Client` and `Inlet.Client.Generators` currently emit or depend on Reservoir legacy client registration primitives.
- Those direct dependencies are allowed to change only as much as needed to keep the branch compiling after Reservoir API removal.

### Next plan / execution context

- The next implementation slice should create the Reservoir builder contracts and WASM-host attach path first.
- It should then migrate Reservoir built-ins, direct sample helpers, and affected Inlet client generator output.
- After consumers are moved, it should remove legacy Reservoir registration entrypoints, update active docs, and append the outcome here.

### Ongoing instruction

- Keep updating this file after each builder slice until the user explicitly says the overall builder effort is complete.
- Copy rule-level decisions that future slices must preserve into `working/handover.md` instead of relying on this dated log alone.
- Do not add new dependencies to any `.csproj` for this builder work unless the user explicitly changes that rule later.

### Implementation update

- Implemented the Reservoir-first builder slice end-to-end across Reservoir contracts, core translation, client attach, built-in lifecycle/navigation, and direct in-repo consumers.
- Added non-obsolete `IReservoirBuilder`-based Inlet client entrypoints so active client startup no longer depends on obsolete `IServiceCollection` composition helpers.
- Updated Inlet client generators to register mappers directly in DI instead of calling the obsolete mapping registration helper.
- Migrated the active Spring sample startup and Spring sample docs to the builder-first client composition story.
- Migrated remaining Reservoir and Inlet tests off deleted legacy Reservoir registration APIs and removed `src/Reservoir.Core/ReservoirRegistrations.cs`.
- Verified `dotnet build .\samples.slnx -c Release -v minimal` succeeds cleanly.
- Verified `dotnet build .\tests\Inlet.Client.L0Tests\Inlet.Client.L0Tests.csproj -c Release -v minimal` succeeds cleanly.
- Verified `dotnet build .\mississippi.slnx -c Release -v minimal` exits with code 0 after the final Inlet builder changes.
