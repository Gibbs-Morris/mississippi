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
  - Run cleanup for Mississippi solution
  - Run cleanup for Samples solution
  - Run tests for Mississippi solution
  - Run tests for Samples solution

## Required Command Set Per Task

Run all commands below for each task:

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/build-sample-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1
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
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
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

- [ ] `P04` Rename `Common.Cosmos` -> `Common.Runtime.Storage.Cosmos`
- [ ] `T04` Rename mirrored tests: `Common.Cosmos.L0Tests` -> `Common.Runtime.Storage.Cosmos.L0Tests`

- [ ] `P05` Rename `EventSourcing.Brooks.Abstractions` -> `Brooks.Abstractions`
- [ ] `T05` Rename mirrored tests: `EventSourcing.Brooks.Abstractions.L0Tests` -> `Brooks.Abstractions.L0Tests`

- [ ] `P06` Rename `EventSourcing.Brooks` -> `Brooks.Runtime`
- [ ] `T06` Rename mirrored tests: `EventSourcing.Brooks.L0Tests` -> `Brooks.Runtime.L0Tests`

- [ ] `P07` Rename `EventSourcing.Brooks.Cosmos` -> `Brooks.Runtime.Storage.Cosmos`
- [ ] `T07` Rename mirrored tests: `EventSourcing.Brooks.Cosmos.L0Tests` -> `Brooks.Runtime.Storage.Cosmos.L0Tests`

- [ ] `P08` Rename `EventSourcing.Serialization.Abstractions` -> `Brooks.Serialization.Abstractions`
- [ ] `T08` Rename mirrored tests: `EventSourcing.Serialization.L0Tests` -> `Brooks.Serialization.Abstractions.L0Tests`

- [ ] `P09` Rename `EventSourcing.Serialization.Json` -> `Brooks.Serialization.Json`
- [ ] `T09` Rename mirrored tests: `EventSourcing.Serialization.Json.L0Tests` -> `Brooks.Serialization.Json.L0Tests`

- [ ] `P10` Rename `EventSourcing.Snapshots.Cosmos` -> `Tributary.Runtime.Storage.Cosmos`
- [ ] `T10` Rename mirrored tests: `EventSourcing.Snapshots.Cosmos.L0Tests` -> `Tributary.Runtime.Storage.Cosmos.L0Tests`

- [ ] `P11` Rename `Inlet.Server.Abstractions` -> `Inlet.Gateway.Abstractions`
- [ ] `T11` Rename mirrored tests: `Inlet.Blazor.Server.L0Tests` -> `Inlet.Gateway.Abstractions.L0Tests`

- [ ] `P12` Rename `Inlet.Server` -> `Inlet.Gateway`
- [ ] `T12` Rename mirrored tests: `Inlet.Server.L0Tests` -> `Inlet.Gateway.L0Tests`

- [ ] `P13` Rename `Inlet.Server.Generators` -> `Inlet.Gateway.Generators`
- [ ] `T13` Rename mirrored tests: `Inlet.Server.Generators.L0Tests` -> `Inlet.Gateway.Generators.L0Tests`

- [ ] `P14` Rename `Inlet.Silo.Abstractions` -> `Inlet.Runtime.Abstractions`
- [ ] `T14` Verify mirrored tests: no dedicated mirrored test project exists (record verification)

- [ ] `P15` Rename `Inlet.Silo` -> `Inlet.Runtime`
- [ ] `T15` Rename mirrored tests: `Inlet.Silo.L0Tests` -> `Inlet.Runtime.L0Tests`

- [ ] `P16` Rename `Inlet.Silo.Generators` -> `Inlet.Runtime.Generators`
- [ ] `T16` Rename mirrored tests: `Inlet.Silo.Generators.L0Tests` -> `Inlet.Runtime.Generators.L0Tests`

- [ ] `P17` Rename `Mississippi.EventSourcing.Testing` -> `DomainModeling.TestHarness`
- [ ] `T17` Verify mirrored tests: no dedicated mirrored test project exists (record verification)

- [ ] `P18` Rename `Refraction` -> `Refraction.Client`
- [ ] `T18` Rename mirrored tests: `Refraction.L0Tests` -> `Refraction.Client.L0Tests`

- [ ] `P19` Rename `Refraction.Pages` -> `Refraction.Client.StateManagement`
- [ ] `T19` Rename mirrored tests: `Refraction.Pages.L0Tests` -> `Refraction.Client.StateManagement.L0Tests`

- [ ] `P20` Rename `Reservoir` -> `Reservoir.Core`
- [ ] `T20` Rename mirrored tests: `Reservoir.L0Tests` -> `Reservoir.Core.L0Tests`

- [ ] `P21` Rename `Reservoir.Blazor` -> `Reservoir.Client`
- [ ] `T21` Rename mirrored tests: `Reservoir.Blazor.L0Tests` -> `Reservoir.Client.L0Tests`

- [ ] `P22` Rename `Sdk.Server` -> `Sdk.Gateway`
- [ ] `T22` Rename mirrored tests: `Sdk.Server.L0Tests` -> `Sdk.Gateway.L0Tests`

- [ ] `P23` Rename `Sdk.Silo` -> `Sdk.Runtime`
- [ ] `T23` Rename mirrored tests: `Sdk.Silo.L0Tests` -> `Sdk.Runtime.L0Tests`

- [ ] `P23b` Rename `Mississippi.Reservoir.Testing` -> `Reservoir.TestHarness`
- [ ] `T23b` Rename mirrored tests: `Reservoir.Testing.L0Tests` -> `Reservoir.TestHarness.L0Tests`

- [ ] `P24` Merge `EventSourcing.Aggregates.Abstractions` + `EventSourcing.Sagas.Abstractions` + `EventSourcing.UxProjections.Abstractions` -> `DomainModeling.Abstractions`
- [ ] `T24` Merge mirrored tests: `EventSourcing.Aggregates.Abstractions.L0Tests` + `EventSourcing.UxProjections.Abstractions.L0Tests` -> `DomainModeling.Abstractions.L0Tests`

- [ ] `P25` Merge `EventSourcing.Aggregates` + `EventSourcing.Sagas` + `EventSourcing.UxProjections` -> `DomainModeling.Runtime`
- [ ] `T25` Merge mirrored tests: `EventSourcing.Aggregates.L0Tests` + `EventSourcing.Sagas.L0Tests` + `EventSourcing.UxProjections.L0Tests` -> `DomainModeling.Runtime.L0Tests`

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
- [x] Cleanup: Mississippi passes
- [x] Cleanup: Samples passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `f26776d8`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct</AssemblyName>` in `Aqueduct.Gateway.csproj` so P01 passes before mirrored test rename task T01.

### [x] Task: Rename `Aqueduct.Grains` -> `Aqueduct.Runtime`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Cleanup: Mississippi passes
- [x] Cleanup: Samples passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `2c64f3a4`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct.Grains</AssemblyName>` in `Aqueduct.Runtime.csproj` so P02 passes before mirrored test rename task T02.

### [x] Task: Rename `Aqueduct.Grains.L0Tests` -> `Aqueduct.Runtime.L0Tests`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Cleanup: Mississippi passes
- [x] Cleanup: Samples passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `faa34bd3`
- Notes: Added temporary `<AssemblyName>Mississippi.Aqueduct.Grains.L0Tests</AssemblyName>` in `Aqueduct.Runtime.L0Tests.csproj` to preserve internal-access compatibility for T02.

### [x] Task: Rename `Common.Cosmos.Abstractions` -> `Common.Runtime.Storage.Abstractions`

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Cleanup: Mississippi passes
- [x] Cleanup: Samples passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [x] Commit created for this task only
- Commit SHA: `6036a41f`
- Notes: No mirrored dedicated test project exists for this abstractions project; T03 records this explicitly.

### [x] Task: Verify no mirrored test project for `Common.Cosmos.Abstractions` rename

- [x] Implement changes
- [x] Build: `mississippi.slnx` passes
- [x] Build: `samples.slnx` passes
- [x] Cleanup: Mississippi passes
- [x] Cleanup: Samples passes
- [x] Tests: Mississippi passes
- [x] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: Verified no dedicated mirrored test project exists (no `Common.Cosmos.Abstractions.L0Tests` equivalent); recorded explicitly per sequence rule.

### [ ] Task: Rename `Common.Cosmos` -> `Common.Runtime.Storage.Cosmos`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Brooks.Abstractions` -> `Brooks.Abstractions`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Brooks` -> `Brooks.Runtime`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Brooks.Cosmos` -> `Brooks.Runtime.Storage.Cosmos`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Serialization.Abstractions` -> `Brooks.Serialization.Abstractions`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Serialization.Json` -> `Brooks.Serialization.Json`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `EventSourcing.Snapshots.Cosmos` -> `Tributary.Runtime.Storage.Cosmos`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Server.Abstractions` -> `Inlet.Gateway.Abstractions`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Server` -> `Inlet.Gateway`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Server.Generators` -> `Inlet.Gateway.Generators`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Silo.Abstractions` -> `Inlet.Runtime.Abstractions`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Silo` -> `Inlet.Runtime`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Inlet.Silo.Generators` -> `Inlet.Runtime.Generators`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Mississippi.EventSourcing.Testing` -> `DomainModeling.TestHarness`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Refraction` -> `Refraction.Client`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Refraction.Pages` -> `Refraction.Client.StateManagement`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Reservoir` -> `Reservoir.Core`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Reservoir.Blazor` -> `Reservoir.Client`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Sdk.Server` -> `Sdk.Gateway`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Rename `Sdk.Silo` -> `Sdk.Runtime`

- [ ] Implement changes
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

## Merge Tasks (Phase 2)

### Per-task merge scope (applies to every merge task below)

- Create the new target project folder and `.csproj`.
- Add the new project to the appropriate `.slnx` file(s).
- Update `.csproj` `ProjectReference` entries to point to the new target project.
- Move all `.cs` files from source project(s) into the target project.
- Delete old source `.csproj` file(s) and folder(s) after files are moved.
- Do not modify `.cs` file contents in this stage; move files only.
- After completing each task, append a task entry to `pr-description-wip.md` before starting the next task.

### [ ] Task: Merge `EventSourcing.Aggregates.Abstractions` + `EventSourcing.Sagas.Abstractions` + `EventSourcing.UxProjections.Abstractions` -> `DomainModeling.Abstractions`

- [ ] Create target project/folder (`DomainModeling.Abstractions`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Merge `EventSourcing.Aggregates` + `EventSourcing.Sagas` + `EventSourcing.UxProjections` -> `DomainModeling.Runtime`

- [ ] Create target project/folder (`DomainModeling.Runtime`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`

### [ ] Task: Merge `EventSourcing.Aggregates.Api` + `EventSourcing.UxProjections.Api` -> `DomainModeling.Gateway`

- [ ] Create target project/folder (`DomainModeling.Gateway`)
- [ ] Add target project to `.slnx`
- [ ] Move all `.cs` files from all source projects into target
- [ ] Update `ProjectReference` entries to target project
- [ ] Delete old source `.csproj` files/folders
- [ ] Build: `mississippi.slnx` passes
- [ ] Build: `samples.slnx` passes
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
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
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
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
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
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
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
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
- [ ] Cleanup: Mississippi passes
- [ ] Cleanup: Samples passes
- [ ] Tests: Mississippi passes
- [ ] Tests: Samples passes
- [ ] WIP notes appended to `pr-description-wip.md`
- [ ] Commit created for this task only
- Commit SHA: `<fill after commit>`
- Notes: `<optional>`
