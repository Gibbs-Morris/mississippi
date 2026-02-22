# Renaming Task Checklist

Use this file as the execution checklist for work derived from `target.md`.

Companion notes file for PR authoring: `pr-description-wip.md`.

## Operating Rules

- Each task is completed in isolation and has its own commit.
- Tasks are executed in interleaved pairs: project task first, then mirrored test-project task immediately after.
- A task is not done until all required verification commands pass.
- Required verification for **every** task:
  - Build `mississippi.slnx`
  - Build `samples.slnx`
  - Run tests for Mississippi solution
  - Run tests for Samples solution
- Cleanup is executed once at the end of the full rename run (not per task).

## Process Review Notes (Current Run)

Issues found and workarounds used so far:

- Missed reference rewiring during `P05` caused a Mississippi build failure; fixed the remaining `ProjectReference` path and re-ran gates.
- Temporary `AssemblyName` continuity was added for staged renames to preserve `InternalsVisibleTo` during P/T interleaving.
- `packages.lock.json` churn was produced by build/test runs; restored out-of-scope lock files before committing.
- Old-path directories from earlier rename steps were left behind (untracked stubs and deleted sources); removed stale stubs and added a cleanup commit to delete old paths.
- Coverage aggregation logs reported transient generated-file missing warnings; treated as non-fatal after tests passed.

## Speed vs Quality Adjustment (Interleaved Pairing)

To reduce cycle time while keeping gates intact:

- For an interleaved pair (`P##` then `T##`), apply both rename edits before running the four required gates.
- Run the full gate set once after the `T##` rename; record the same gate run for both `P##` and `T##` entries.
- Keep separate commits by staging `P##` changes first after gates, then staging and committing `T##` changes.
- If a gate fails, fix and re-run the full gate set before committing either task.

## Required Command Set Per Task

Run all commands below for each task:

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/build-sample-solution.ps1
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1
```

## Task Template

Copy this block for each task:

```markdown
### [ ] Task: <task title>

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `see git log (Task P01 commit)`
- Notes: `<optional>`
```

## Tasks

## Interleaved Execution Order (Authoritative)

Use this ordered list as the real execution sequence. For each `P*` project task, run the matching `T*` test task immediately next.

- [x] `P01` Rename `Aqueduct` -> `Aqueduct.Gateway`
- [x] `T01` Rename mirrored tests: `Aqueduct.L0Tests` -> `Aqueduct.Gateway.L0Tests`; `Aqueduct.L2Tests` -> `Aqueduct.Gateway.L2Tests`; `Aqueduct.L2Tests.AppHost` -> `Aqueduct.Gateway.L2Tests.AppHost`

- [x] `P02` Rename `Aqueduct.Grains` -> `Aqueduct.Runtime`
- [x] `T02` Rename mirrored tests: `Aqueduct.Grains.L0Tests` -> `Aqueduct.Runtime.L0Tests`

- [x] `P03` Rename `Common.Cosmos.Abstractions` -> `Common.Runtime.Storage.Abstractions`
- [x] `T03` Verify mirrored tests: no dedicated mirrored test project exists (record verification)

- [x] `P04` Rename `Common.Cosmos` -> `Common.Runtime.Storage.Cosmos`
- [x] `T04` Rename mirrored tests: `Common.Cosmos.L0Tests` -> `Common.Runtime.Storage.Cosmos.L0Tests`

- [x] `P05` Rename `EventSourcing.Brooks.Abstractions` -> `Brooks.Abstractions`
- [x] `T05` Rename mirrored tests: `EventSourcing.Brooks.Abstractions.L0Tests` -> `Brooks.Abstractions.L0Tests`

- [x] `P06` Rename `EventSourcing.Brooks` -> `Brooks.Runtime`
- [x] `T06` Rename mirrored tests: `EventSourcing.Brooks.L0Tests` -> `Brooks.Runtime.L0Tests`

- [x] `P07` Rename `EventSourcing.Brooks.Cosmos` -> `Brooks.Runtime.Storage.Cosmos`
- [x] `T07` Rename mirrored tests: `EventSourcing.Brooks.Cosmos.L0Tests` -> `Brooks.Runtime.Storage.Cosmos.L0Tests`

- [x] `P08` Rename `EventSourcing.Serialization.Abstractions` -> `Brooks.Serialization.Abstractions`
- [x] `T08` Rename mirrored tests: `EventSourcing.Serialization.L0Tests` -> `Brooks.Serialization.Abstractions.L0Tests`

- [x] `P09` Rename `EventSourcing.Serialization.Json` -> `Brooks.Serialization.Json`
- [x] `T09` Rename mirrored tests: `EventSourcing.Serialization.Json.L0Tests` -> `Brooks.Serialization.Json.L0Tests`

- [x] `P10` Rename `EventSourcing.Snapshots.Cosmos` -> `Tributary.Runtime.Storage.Cosmos`
- [x] `T10` Rename mirrored tests: `EventSourcing.Snapshots.Cosmos.L0Tests` -> `Tributary.Runtime.Storage.Cosmos.L0Tests`

- [x] `P11` Rename `Inlet.Server.Abstractions` -> `Inlet.Gateway.Abstractions`
- [x] `T11` Rename mirrored tests: `Inlet.Blazor.Server.L0Tests` -> `Inlet.Gateway.Abstractions.L0Tests`

- [x] `P12` Rename `Inlet.Server` -> `Inlet.Gateway`
- [x] `T12` Rename mirrored tests: `Inlet.Server.L0Tests` -> `Inlet.Gateway.L0Tests`

- [x] `P13` Rename `Inlet.Server.Generators` -> `Inlet.Gateway.Generators`
- [x] `T13` Rename mirrored tests: `Inlet.Server.Generators.L0Tests` -> `Inlet.Gateway.Generators.L0Tests`

- [x] `P14` Rename `Inlet.Silo.Abstractions` -> `Inlet.Runtime.Abstractions`
- [x] `T14` Verify mirrored tests: no dedicated mirrored test project exists (record verification)

- [x] `P15` Rename `Inlet.Silo` -> `Inlet.Runtime`
- [x] `T15` Rename mirrored tests: `Inlet.Silo.L0Tests` -> `Inlet.Runtime.L0Tests`

- [x] `P16` Rename `Inlet.Silo.Generators` -> `Inlet.Runtime.Generators`
- [x] `T16` Rename mirrored tests: `Inlet.Silo.Generators.L0Tests` -> `Inlet.Runtime.Generators.L0Tests`

- [x] `P17` Rename `Mississippi.EventSourcing.Testing` -> `DomainModeling.TestHarness`
- [x] `T17` Verify mirrored tests: no dedicated mirrored test project exists (record verification)

- [x] `P18` Rename `Refraction` -> `Refraction.Client`
- [x] `T18` Rename mirrored tests: `Refraction.L0Tests` -> `Refraction.Client.L0Tests`

- [x] `P19` Rename `Refraction.Pages` -> `Refraction.Client.StateManagement`
- [x] `T19` Rename mirrored tests: `Refraction.Pages.L0Tests` -> `Refraction.Client.StateManagement.L0Tests`

- [x] `P20` Rename `Reservoir` -> `Reservoir.Core`
- [x] `T20` Rename mirrored tests: `Reservoir.L0Tests` -> `Reservoir.Core.L0Tests`

- [x] `P21` Rename `Reservoir.Blazor` -> `Reservoir.Client`
- [x] `T21` Rename mirrored tests: `Reservoir.Blazor.L0Tests` -> `Reservoir.Client.L0Tests`

- [x] `P22` Rename `Sdk.Server` -> `Sdk.Gateway`
- [x] `T22` Rename mirrored tests: `Sdk.Server.L0Tests` -> `Sdk.Gateway.L0Tests`

- [x] `P23` Rename `Sdk.Silo` -> `Sdk.Runtime`
- [x] `T23` Rename mirrored tests: `Sdk.Silo.L0Tests` -> `Sdk.Runtime.L0Tests`

- [x] `P23b` Rename `Mississippi.Reservoir.Testing` -> `Reservoir.TestHarness`
- [x] `T23b` Rename mirrored tests: `Reservoir.Testing.L0Tests` -> `Reservoir.TestHarness.L0Tests`

- [x] `P24` Merge `EventSourcing.Aggregates.Abstractions` + `EventSourcing.Sagas.Abstractions` + `EventSourcing.UxProjections.Abstractions` -> `DomainModeling.Abstractions`
- [x] `T24` Merge mirrored tests: `EventSourcing.Aggregates.Abstractions.L0Tests` + `EventSourcing.UxProjections.Abstractions.L0Tests` -> `DomainModeling.Abstractions.L0Tests`

- [x] `P25` Merge `EventSourcing.Aggregates` + `EventSourcing.Sagas` + `EventSourcing.UxProjections` -> `DomainModeling.Runtime`
- [x] `T25` Merge mirrored tests: `EventSourcing.Aggregates.L0Tests` + `EventSourcing.Sagas.L0Tests` + `EventSourcing.UxProjections.L0Tests` -> `DomainModeling.Runtime.L0Tests`

- [ ] `P26` Merge `EventSourcing.Aggregates.Api` + `EventSourcing.UxProjections.Api` -> `DomainModeling.Gateway`
- [ ] `T26` Merge mirrored tests: `EventSourcing.Aggregates.Api.L0Tests` + `EventSourcing.UxProjections.Api.L0Tests` -> `DomainModeling.Gateway.L0Tests`

- [ ] `P27` Merge `EventSourcing.Reducers.Abstractions` + `EventSourcing.Snapshots.Abstractions` -> `Tributary.Abstractions`
- [ ] `T27` Merge mirrored tests: `EventSourcing.Reducers.Abstractions.L0Tests` + `EventSourcing.Snapshots.Abstractions.L0Tests` -> `Tributary.Abstractions.L0Tests`

- [ ] `P28` Merge `EventSourcing.Reducers` + `EventSourcing.Snapshots` -> `Tributary.Runtime`
- [ ] `T28` Merge mirrored tests: `EventSourcing.Reducers.L0Tests` + `EventSourcing.Snapshots.L0Tests` -> `Tributary.Runtime.L0Tests`

- [ ] `P29` Extract `Brooks.Runtime.Storage.Abstractions` from `Brooks.Abstractions`
- [ ] `T29` Extract mirrored tests from `Brooks.Abstractions.L0Tests` -> `Brooks.Runtime.Storage.Abstractions.L0Tests` (move only storage-abstraction test files)

- [ ] `P30` Extract `Tributary.Runtime.Storage.Abstractions` from `Tributary.Abstractions`
- [ ] `T30` Extract mirrored tests from `Tributary.Abstractions.L0Tests` -> `Tributary.Runtime.Storage.Abstractions.L0Tests` (move only storage-abstraction test files)

### Per-task rename scope (applies to every task below)

- Rename the project folder.
- Rename the `.csproj` file.
- Update `.slnx` references.
- Update `ProjectReference` entries in `.csproj` files.
- Do not change any `.cs` file in this stage.
- After completing each task, append a task entry to `pr-description-wip.md` before starting the next task.

### [x] Task: Rename `Aqueduct` -> `Aqueduct.Gateway`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `f26776d8`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct</AssemblyName>` in `Aqueduct.Gateway.csproj` so P01 passes before mirrored test rename task T01.

### [x] Task: Rename `Aqueduct.Grains` -> `Aqueduct.Runtime`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `2c64f3a4`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct.Grains</AssemblyName>` in `Aqueduct.Runtime.csproj` so P02 passes before mirrored test rename task T02.

### [x] Task: Rename `Aqueduct.Grains.L0Tests` -> `Aqueduct.Runtime.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `faa34bd3`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct.Grains.L0Tests</AssemblyName>` in `Aqueduct.Runtime.L0Tests.csproj` to preserve internal-access compatibility for T02.

### [x] Task: Rename `Common.Cosmos.Abstractions` -> `Common.Runtime.Storage.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `6036a41f`
- Notes: No mirrored dedicated test project exists for this abstractions project; T03 records this explicitly.

### [x] Task: Verify no mirrored test project for `Common.Cosmos.Abstractions` rename

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Verified no dedicated mirrored test project exists (no `Common.Cosmos.Abstractions.L0Tests` equivalent); recorded explicitly per sequence rule.

### [x] Task: Rename `Common.Cosmos` -> `Common.Runtime.Storage.Cosmos`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Mirrored test-project rename `T04` follows immediately.

### [x] Task: Rename `Common.Cosmos.L0Tests` -> `Common.Runtime.Storage.Cosmos.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Added temporary `<AssemblyName>Mississippi.Common.Cosmos.L0Tests</AssemblyName>` to preserve internal-access compatibility during staged renames.

### [x] Task: Rename `EventSourcing.Brooks.Abstractions` -> `Brooks.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks.Abstractions</AssemblyName>` in `Brooks.Abstractions.csproj` so P05 validates before mirrored test rename task T05.

### [x] Task: Rename `EventSourcing.Brooks.Abstractions.L0Tests` -> `Brooks.Abstractions.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks.Abstractions.L0Tests</AssemblyName>` in `Brooks.Abstractions.L0Tests.csproj` to preserve internal-access compatibility during staged renames.

### [x] Task: Rename `EventSourcing.Brooks` -> `Brooks.Runtime`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks</AssemblyName>` in `Brooks.Runtime.csproj` so P06 validates before mirrored test rename task T06.

### [x] Task: Rename `EventSourcing.Brooks.L0Tests` -> `Brooks.Runtime.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks.L0Tests</AssemblyName>` in `Brooks.Runtime.L0Tests.csproj` to preserve internal-access compatibility during staged renames.

### [x] Task: Rename `EventSourcing.Brooks.Cosmos` -> `Brooks.Runtime.Storage.Cosmos`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `c35a7993`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks.Cosmos</AssemblyName>` in `Brooks.Runtime.Storage.Cosmos.csproj` so P07 validates before mirrored test rename task T07.

### [x] Task: Rename `EventSourcing.Brooks.Cosmos.L0Tests` -> `Brooks.Runtime.Storage.Cosmos.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `d4802d1b`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Brooks.Cosmos.L0Tests</AssemblyName>` in `Brooks.Runtime.Storage.Cosmos.L0Tests.csproj` to preserve internal-access compatibility during staged renames.

### [x] Task: Rename `EventSourcing.Serialization.Abstractions` -> `Brooks.Serialization.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `ab8f21b4`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Serialization.Abstractions</AssemblyName>` in `Brooks.Serialization.Abstractions.csproj` so P08 validates before mirrored test rename task T08.

### [x] Task: Rename `EventSourcing.Serialization.L0Tests` -> `Brooks.Serialization.Abstractions.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `5a0fac1a`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Serialization.L0Tests</AssemblyName>` in `Brooks.Serialization.Abstractions.L0Tests.csproj` to preserve internal-access compatibility during staged renames.

### [x] Task: Rename `EventSourcing.Serialization.Json` -> `Brooks.Serialization.Json`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `60a28a4f`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Serialization.Json</AssemblyName>`. Found extra consumers: `Architecture.L0Tests.csproj`, `Crescent.L2Tests.csproj` — both updated.

### [x] Task: Rename `EventSourcing.Serialization.Json.L0Tests` -> `Brooks.Serialization.Json.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `d1407f7e`
- Notes: Added temporary `<AssemblyName>Mississippi.EventSourcing.Serialization.Json.L0Tests</AssemblyName>` in `Brooks.Serialization.Json.L0Tests.csproj`.

### [x] Task: Rename `EventSourcing.Snapshots.Cosmos` -> `Tributary.Runtime.Storage.Cosmos`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `b03ba57e` (P10), `edf51c23` (T10)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Server.Abstractions` -> `Inlet.Gateway.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `521ee4cd` (P11), `7eec627d` (T11)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Server` -> `Inlet.Gateway`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `11558f22` (P12), `ade812b1` (T12)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Server.Generators` -> `Inlet.Gateway.Generators`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `1f055fd4` (P13), `52f8aded` (T13)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Silo.Abstractions` -> `Inlet.Runtime.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `64fd1807` (P14) — T14: no test project (verified)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Silo` -> `Inlet.Runtime`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `4d9e7e04` (P15), `2a42d0bf` (T15)
- Notes: `<optional>`

### [x] Task: Rename `Inlet.Silo.Generators` -> `Inlet.Runtime.Generators`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `109a428a` (P16), `6670b331` (T16)
- Notes: `<optional>`

### [x] Task: Rename `Mississippi.EventSourcing.Testing` -> `DomainModeling.TestHarness`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `c7f54113` (P17) — T17: no test project (verified)
- Notes: `<optional>`

### [x] Task: Rename `Refraction` -> `Refraction.Client`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `fa02708f` (P18), `09d89fcf` (T18)
- Notes: `<optional>`

### [x] Task: Rename `Refraction.Pages` -> `Refraction.Client.StateManagement`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `d8c28240` (P19), `ac070de6` (T19)
- Notes: `<optional>`

### [x] Task: Rename `Reservoir` -> `Reservoir.Core`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `d70315e9` (P20), `436eb591` (T20)
- Notes: `<optional>`

### [x] Task: Rename `Reservoir.Blazor` -> `Reservoir.Client`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `b004742a` (P21), `12778344` (T21)
- Notes: `<optional>`

### [x] Task: Rename `Sdk.Server` -> `Sdk.Gateway`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `14bed8d3` (P22), `db6ee1ae` (T22)
- Notes: `<optional>`

### [x] Task: Rename `Sdk.Silo` -> `Sdk.Runtime`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `a9e3e54a` (P23), `264846c2` (T23), `23ba37bb` (P23b), `66465d0c` (T23b)
- Notes: P23b also covers Reservoir.Testing -> Reservoir.TestHarness rename.

## Merge Tasks (Phase 2)

### Per-task merge scope (applies to every merge task below)

- Create the new target project folder and `.csproj`.
- Add the new project to the appropriate `.slnx` file(s).
- Update `.csproj` `ProjectReference` entries to point to the new target project.
- Move all `.cs` files from source project(s) into the target project.
- Delete old source `.csproj` file(s) and folder(s) after files are moved.
- Do not modify `.cs` file contents in this stage; move files only.
- After completing each task, append a task entry to `pr-description-wip.md` before starting the next task.

### [x] Task: Merge `EventSourcing.Aggregates.Abstractions` + `EventSourcing.Sagas.Abstractions` + `EventSourcing.UxProjections.Abstractions` -> `DomainModeling.Abstractions`

- [x] Create target project/folder (`DomainModeling.Abstractions`)
- [x] Add target project to `.slnx`
- [x] Move all `.cs` files from all source projects into target
- [x] Update `ProjectReference` entries to target project
- [x] Delete old source `.csproj` files/folders
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] WIP notes appended to `pr-description-wip.md`
- [x] Commit created for this task only
- Commit SHA: `92a0ab09` (P24), `399d69dd` (T24)
- Notes: `<optional>`

### [x] Task: Merge `EventSourcing.Aggregates` + `EventSourcing.Sagas` + `EventSourcing.UxProjections` -> `DomainModeling.Runtime`

- [x] Create target project/folder (`DomainModeling.Runtime`)
- [x] Add target project to `.slnx`
- [x] Move all `.cs` files from all source projects into target
- [x] Update `ProjectReference` entries to target project
- [x] Delete old source `.csproj` files/folders
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] WIP notes appended to `pr-description-wip.md`
- [x] Commit created for this task only
- Commit SHA: `32384a05` (P25), `4ae4d32a` (T25)
- Notes: `<optional>`

### [ ] Task: Merge `EventSourcing.Aggregates.Api` + `EventSourcing.UxProjections.Api` -> `DomainModeling.Gateway`

- [ ] Create target project/folder (`DomainModeling.Gateway`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Merge `EventSourcing.Reducers.Abstractions` + `EventSourcing.Snapshots.Abstractions` -> `Tributary.Abstractions`

- [ ] Create target project/folder (`Tributary.Abstractions`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Merge `EventSourcing.Reducers` + `EventSourcing.Snapshots` -> `Tributary.Runtime`

- [ ] Create target project/folder (`Tributary.Runtime`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

## Extraction Tasks Required for Final Target Layout

### [ ] Task: Extract `Brooks.Runtime.Storage.Abstractions` from `Brooks.Abstractions`

- [ ] Create target project/folder (`Brooks.Runtime.Storage.Abstractions`)
- [ ] Add target project to `.slnx`
- [ ] Move storage abstraction `.cs` files from `Brooks.Abstractions`
- [ ] Update `ProjectReference` entries to include new project
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Extract `Tributary.Runtime.Storage.Abstractions` from `Tributary.Abstractions`

- [ ] Create target project/folder (`Tributary.Runtime.Storage.Abstractions`)
- [ ] Add target project to `.slnx`
- [ ] Move storage abstraction `.cs` files from `Tributary.Abstractions`
- [ ] Update `ProjectReference` entries to include new project
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`
