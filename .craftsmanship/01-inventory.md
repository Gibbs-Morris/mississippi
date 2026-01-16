# File Inventory

## Summary

| Category | Count | Pass 1 | Pass 2 |
|----------|-------|--------|--------|
| Root Configuration | 15 | 5 | 5 |
| GitHub Config & Workflows | 47 | 3 | 3 |
| Engineering Scripts | 26 | 0 | 0 |
| Documentation | 12 | 2 | 2 |
| Source Projects (src/) | ~250 | 15 | 15 |
| Test Projects (tests/) | ~200 | 2 | 2 |
| Sample Projects (samples/) | ~250 | 5 | 5 |
| **Total** | **~800** | **32** | **32** |

> **Note:** This review focused on critical files that define patterns and architecture. The full inventory is provided below for completeness, but detailed review was conducted on key files (see 03-file-reviews.md).

---

## Root Configuration Files

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `.config/dotnet-tools.json` | Config | [ ] | [ ] |
| `.editorconfig` | Config | [x] | [x] |
| `Directory.Build.props` | MSBuild | [x] | [x] |
| `Directory.Packages.props` | MSBuild | [x] | [x] |
| `Directory.DotSettings` | ReSharper | [ ] | [ ] |
| `GitVersion.yml` | Config | [ ] | [ ] |
| `global.json` | Config | [x] | [x] |
| `mississippi.slnx` | Solution | [x] | [x] |
| `samples.slnx` | Solution | [ ] | [ ] |
| `stryker-config.json` | Config | [ ] | [ ] |
| `build.ps1` | Script | [ ] | [ ] |
| `clean-up.ps1` | Script | [ ] | [ ] |
| `go.ps1` | Script | [ ] | [ ] |
| `quick-build.ps1` | Script | [ ] | [ ] |
| `README.md` | Doc | [x] | [x] |
| `TEST_CASES_TEMPLATE.md` | Doc | [ ] | [ ] |
| `agents.md` | Doc | [ ] | [ ] |
| `todo.md` | Doc | [ ] | [ ] |

---

## GitHub Configuration

### Workflows

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `.github/workflows/cleanup.yml` | CI | [ ] | [ ] |
| `.github/workflows/copilot-setup-steps.yml` | CI | [ ] | [ ] |
| `.github/workflows/docusaurus.yml` | CI | [ ] | [ ] |
| `.github/workflows/full-build.yml` | CI | [ ] | [ ] |
| `.github/workflows/l0-tests.yml` | CI | [ ] | [ ] |
| `.github/workflows/l1-tests.yml` | CI | [ ] | [ ] |
| `.github/workflows/l2-tests.yml` | CI | [ ] | [ ] |
| `.github/workflows/markdown-lint.yml` | CI | [ ] | [ ] |
| `.github/workflows/powershell-tests.yml` | CI | [ ] | [ ] |
| `.github/workflows/sonar-cloud.yml` | CI | [ ] | [ ] |
| `.github/workflows/stryker.yml` | CI | [ ] | [ ] |
| `.github/dependabot.yml` | Config | [ ] | [ ] |
| `.github/linters/.markdown-lint.yml` | Config | [ ] | [ ] |
| `.github/copilot‑instructions.md` | Doc | [ ] | [ ] |

### Agent Definitions (24 files)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `.github/agents/c4-code-architect.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/c4-component-architect.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/c4-container-architect.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/c4-context-architect.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/cleanup-agent.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/code-reviewer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/cov-skill-architect.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-implementer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-planner.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-reviewer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-skeptic.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-solo.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-solo-spec.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/CoV-enterprise-verifier.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/dev.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/principal-engineer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/qa-engineer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/rules-manager.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/scrum-master.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/tdd-developer.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/work-breakdown.agent.md` | Agent | [ ] | [ ] |
| `.github/agents/xml-doc-writer.agent.md` | Agent | [ ] | [ ] |

### Instructions (28 files)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `.github/instructions/abstractions-projects.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/agent-scratchpad.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/allure-test-naming.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/aspire.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/authoring.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/benchmarks.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/blazor-ux-guidelines.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/build-issue-remediation.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/build-rules.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/csharp.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/domain-modeling.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/dotnet-architecture-good-practices.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/grain-doc-maintenance.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/grain-read-write-paths.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/instruction-mdc-sync.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/keyed-services.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/logging-rules.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/markdown.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/mutation-testing.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/naming.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/orleans-serialization.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/orleans.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/powershell.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/projects.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/pull-request-reviews.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/rfc2119.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/service-registration.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/shared-policies.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/storage-type-naming.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/test-improvement.instructions.md` | Instr | [ ] | [ ] |
| `.github/instructions/testing.instructions.md` | Instr | [ ] | [ ] |

---

## Engineering Scripts (eng/)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `eng/src/agent-scripts/README.md` | Doc | [ ] | [ ] |
| `eng/src/agent-scripts/build-mississippi-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/build-sample-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/clean-up-mississippi-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/clean-up-sample-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/final-build-solutions.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/integration-test-sample-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/orchestrate-solutions.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/summarize-coverage-gaps.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/summarize-mutation-survivors.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/sync-instructions-to-mdc.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/test-project-quality.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/unit-test-mississippi-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/unit-test-sample-solution.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/README.md` | Doc | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/claim-scratchpad-task.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/complete-scratchpad-task.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/defer-scratchpad-task.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/list-scratchpad-task.ps1` | Script | [ ] | [ ] |
| `eng/src/agent-scripts/tasks/new-scratchpad-task.ps1` | Script | [ ] | [ ] |
| `eng/tests/agent-scripts/RepositoryAutomation.Tests.ps1` | Test | [ ] | [ ] |
| `eng/tests/agent-scripts/TaskAutomation.Tests.ps1` | Test | [ ] | [ ] |
| `eng/tests/agent-scripts/run-*.ps1` | Test | [ ] | [ ] |
| `eng/tests/agent-scripts/scratchpad-task-scripts.Tests.ps1` | Test | [ ] | [ ] |
| `eng/tests/agent-scripts/summarize-coverage-gaps.Tests.ps1` | Test | [ ] | [ ] |
| `eng/tests/orchestrate-powershell-tests.ps1` | Test | [ ] | [ ] |

---

## Documentation (docs/)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `docs/Docusaurus/README.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/package.json` | Config | [ ] | [ ] |
| `docs/Docusaurus/package-lock.json` | Lock | [ ] | [ ] |
| `docs/Docusaurus/tsconfig.json` | Config | [ ] | [ ] |
| `docs/Docusaurus/test-docusaurus.ps1` | Script | [ ] | [ ] |
| `docs/Docusaurus/docs/intro.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/_category_.json` | Config | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/index.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/actions.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/effects.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/reducers.md` | Doc | [ ] | [ ] |
| `docs/Docusaurus/docs/reservoir/store.md` | Doc | [ ] | [ ] |

---

## Source Projects (src/)

### Common Libraries

#### Common.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Common.Abstractions/Common.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/Common.Abstractions/MississippiDefaults.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/IMapper.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/IEnumerableMapper.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/IAsyncEnumerableMapper.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/EnumerableMapper.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs` | Source | [ ] | [ ] |
| `src/Common.Abstractions/Mapping/MappingRegistrations.cs` | Source | [ ] | [ ] |

#### Common.Cosmos.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Common.Cosmos.Abstractions/Common.Cosmos.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/Common.Cosmos.Abstractions/Retry/IRetryPolicy.cs` | Source | [ ] | [ ] |

#### Common.Cosmos

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Common.Cosmos/Common.Cosmos.csproj` | Project | [ ] | [ ] |
| `src/Common.Cosmos/Retry/CosmosRetryPolicy.cs` | Source | [ ] | [ ] |
| `src/Common.Cosmos/Retry/CosmosRetryPolicyLoggerExtensions.cs` | Source | [ ] | [ ] |

### Aqueduct (SignalR Orleans Integration)

#### Aqueduct.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Aqueduct.Abstractions/Aqueduct.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/Aqueduct.Abstractions/AqueductOptions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/IAqueductGrainFactory.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/IAqueductNotifier.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Grains/ISignalRClientGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Grains/ISignalRGroupGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Grains/ISignalRServerDirectoryGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Keys/SignalRClientKey.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Keys/SignalRServerDirectoryKey.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Messages/AllMessage.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Abstractions/Messages/ServerMessage.cs` | Source | [ ] | [ ] |

#### Aqueduct.Grains

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Aqueduct.Grains/Aqueduct.Grains.csproj` | Project | [ ] | [ ] |
| `src/Aqueduct.Grains/AqueductGrainFactory.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/AqueductGrainFactoryLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/AqueductGrainsRegistrations.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/AqueductSiloOptions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Diagnostics/AqueductMetrics.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRClientGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRClientGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRGroupGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRGroupGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRServerDirectoryGrain.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/SignalRServerDirectoryGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/State/SignalRClientState.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/State/SignalRGroupState.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/State/SignalRServerDirectoryState.cs` | Source | [ ] | [ ] |
| `src/Aqueduct.Grains/Grains/State/SignalRServerInfo.cs` | Source | [ ] | [ ] |

#### Aqueduct (ASP.NET Integration)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Aqueduct/Aqueduct.csproj` | Project | [ ] | [ ] |
| `src/Aqueduct/AqueductGrainFactory.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductGrainFactoryLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductHubLifetimeManager.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductHubLifetimeManagerLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductNotifier.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductNotifierLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/Aqueduct/AqueductRegistrations.cs` | Source | [ ] | [ ] |

### EventSourcing Core

#### EventSourcing.Aggregates.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Aggregates.Abstractions/EventSourcing.Aggregates.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/AggregateErrorCodes.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/AggregateServiceAttribute.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/IAggregateGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/IBrookEventConverter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/ICommandHandler.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/IEventTypeRegistry.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/IGenericAggregateGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/IRootCommandHandler.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/ISnapshotTypeRegistry.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Abstractions/OperationResult.cs` | Source | [ ] | [ ] |

#### EventSourcing.Aggregates

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Aggregates/EventSourcing.Aggregates.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Aggregates/AggregateGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/AggregateGrainFactoryLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/AggregateGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/AggregateRegistrations.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/BrookEventConverter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/DelegateCommandHandler.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/Diagnostics/AggregateMetrics.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/EventTypeRegistry.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/GenericAggregateGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/RootCommandHandler.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/RootCommandHandlerLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates/SnapshotTypeRegistry.cs` | Source | [ ] | [ ] |

#### EventSourcing.Aggregates.Api

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Aggregates.Api/EventSourcing.Aggregates.Api.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Api/AggregateControllerBase.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Api/AggregateControllerLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Api/AggregateServiceBase.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Api/AggregateServiceLoggerExtensions.cs` | Source | [ ] | [ ] |

#### EventSourcing.Aggregates.Generators

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Aggregates.Generators/EventSourcing.Aggregates.Generators.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Generators/AggregateServiceGenerator.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Generators/Models/AggregateServiceInfo.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Generators/Models/CommandHandlerInfo.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Aggregates.Generators/Models/CommandInfo.cs` | Source | [ ] | [ ] |

### EventSourcing Brooks (Event Streams)

#### EventSourcing.Brooks.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Brooks.Abstractions/EventSourcing.Brooks.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookEvent.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookNameHelper.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookPosition.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameHelper.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameAttribute.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameHelper.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Cursor/IBrookCursorGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Factory/IBrookGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Reader/IBrookAsyncReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Reader/IBrookReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Storage/BrookStorageProviderExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageProvider.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Streaming/BrookCursorMovedEvent.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Streaming/BrookProviderOptions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Streaming/IStreamIdFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/Writer/IBrookWriterGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Abstractions/suggested-test-cases.md` | Doc | [ ] | [ ] |

#### EventSourcing.Brooks

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Brooks/EventSourcing.Brooks.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Brooks/EventSourcingOrleansStreamNames.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/EventSourcingRegistrations.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/StreamIdFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Cursor/BrookCursorGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Cursor/BrookCursorGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Diagnostics/BrookMetrics.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Factory/BrookGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Factory/IInternalBrookGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookAsyncReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookReaderGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookReaderOptions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookSliceReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/BrookSliceReaderGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Reader/IBrookSliceReaderGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Writer/BrookWriterGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/Writer/BrookWriterGrainLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks/suggested-test-cases.md` | Doc | [ ] | [ ] |

#### EventSourcing.Brooks.Cosmos

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Brooks.Cosmos/EventSourcing.Brooks.Cosmos.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/BrookStorageOptions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/BrookStorageProvider.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/BrookStorageProviderRegistrations.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/IBrookRecoveryService.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/ICosmosRepository.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/IEventBrookReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/IEventBrookWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/OptimisticConcurrencyException.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Batching/BatchSizeEstimator.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Batching/IBatchSizeEstimator.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Brooks/BrookRecoveryService.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Brooks/BrookRecoveryServiceLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Brooks/EventBrookReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Brooks/EventBrookReaderLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Brooks/EventBrookWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/BlobDistributedLock.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/BlobDistributedLockManager.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/BlobDistributedLockManagerLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/BlobLeaseClientAdapter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/BlobLeaseClientFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/IBlobLeaseClient.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/IBlobLeaseClientFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/IDistributedLock.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/IDistributedLockManager.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Locking/LockMetrics.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Mapping/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/Storage/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Brooks.Cosmos/suggested-test-cases.md` | Doc | [ ] | [ ] |

### EventSourcing Reducers

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Reducers.Abstractions/EventSourcing.Reducers.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers.Abstractions/IEventReducer.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers.Abstractions/IRootReducer.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers/EventSourcing.Reducers.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Reducers/DelegateEventReducer.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers/DelegateReducerLoggerExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers/ReducerRegistrations.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers/RootReducer.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Reducers/RootReducerLoggerExtensions.cs` | Source | [ ] | [ ] |

### EventSourcing Serialization

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Serialization.Abstractions/EventSourcing.Serialization.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/ISerializationProvider.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/ISerializationReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/ISerializationWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/IAsyncSerializationReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/IAsyncSerializationWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Abstractions/SerializationStorageProviderExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Json/EventSourcing.Serialization.Json.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Serialization.Json/JsonSerializationProvider.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Serialization.Json/ServiceRegistration.cs` | Source | [ ] | [ ] |

### EventSourcing Snapshots

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.Snapshots.Abstractions/EventSourcing.Snapshots.Abstractions.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotCacheGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotGrainFactory.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotPersisterGrain.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStateConverter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageProvider.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageReader.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageWriter.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotEnvelope.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotRetentionOptions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotStorageProviderExtensions.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots/EventSourcing.Snapshots.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Snapshots/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Cosmos/EventSourcing.Snapshots.Cosmos.csproj` | Project | [ ] | [ ] |
| `src/EventSourcing.Snapshots.Cosmos/*.cs` | Source | [ ] | [ ] |

### EventSourcing UxProjections

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/EventSourcing.UxProjections.Abstractions/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.UxProjections/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.UxProjections.Api/*.cs` | Source | [ ] | [ ] |
| `src/EventSourcing.UxProjections.Api.Generators/*.cs` | Source | [ ] | [ ] |

### Inlet (Projection Subscriptions)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Inlet.Abstractions/*.cs` | Source | [ ] | [ ] |
| `src/Inlet/*.cs` | Source | [ ] | [ ] |
| `src/Inlet.Orleans/*.cs` | Source | [ ] | [ ] |
| `src/Inlet.Orleans.SignalR/*.cs` | Source | [ ] | [ ] |
| `src/Inlet.Blazor.Server/*.cs` | Source | [ ] | [ ] |
| `src/Inlet.Blazor.WebAssembly/*.cs` | Source | [ ] | [ ] |
| `src/Inlet.Projection.Abstractions/*.cs` | Source | [ ] | [ ] |

### Reservoir (Blazor State Management)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `src/Reservoir.Abstractions/*.cs` | Source | [ ] | [ ] |
| `src/Reservoir/*.cs` | Source | [ ] | [ ] |
| `src/Reservoir.Blazor/*.cs` | Source | [ ] | [ ] |

---

## Test Projects (tests/)

*Note: Test projects follow the pattern `{SourceProject}.L0Tests`, `{SourceProject}.L1Tests`, `{SourceProject}.L2Tests`*

### Architecture Tests

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `tests/Architecture.L0Tests/Architecture.L0Tests.csproj` | Project | [ ] | [ ] |
| `tests/Architecture.L0Tests/AbstractionsLayeringTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/AccessControlArchitectureTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/ArchitectureTestBase.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/CSharpArchitectureTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/LoggingArchitectureTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/NamingConventionTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/OrleansGrainArchitectureTests.cs` | Test | [ ] | [ ] |
| `tests/Architecture.L0Tests/ServiceRegistrationArchitectureTests.cs` | Test | [ ] | [ ] |

### Testing Utilities

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `tests/Testing.Utilities/Testing.Utilities.csproj` | Project | [ ] | [ ] |
| `tests/Testing.Utilities/Mocks/GrainContextMockBuilder.cs` | Source | [ ] | [ ] |
| `tests/Testing.Utilities/Orleans/*.cs` | Source | [ ] | [ ] |
| `tests/Testing.Utilities/Storage/InMemoryBrookStorage.cs` | Source | [ ] | [ ] |

### L0 Test Projects (Abbreviated - ~35 projects)

*Each source project has a corresponding `.L0Tests` project with unit tests*

| Project | Pass 1 | Pass 2 |
|---------|--------|--------|
| `Aqueduct.Abstractions.L0Tests` | [ ] | [ ] |
| `Aqueduct.Grains.L0Tests` | [ ] | [ ] |
| `Aqueduct.L0Tests` | [ ] | [ ] |
| `Common.Abstractions.L0Tests` | [ ] | [ ] |
| `Common.Cosmos.L0Tests` | [ ] | [ ] |
| `EventSourcing.Aggregates.Abstractions.L0Tests` | [ ] | [ ] |
| `EventSourcing.Aggregates.Api.L0Tests` | [ ] | [ ] |
| `EventSourcing.Aggregates.Generators.L0Tests` | [ ] | [ ] |
| `EventSourcing.Aggregates.L0Tests` | [ ] | [ ] |
| `EventSourcing.Brooks.Abstractions.L0Tests` | [ ] | [ ] |
| `EventSourcing.Brooks.Cosmos.L0Tests` | [ ] | [ ] |
| `EventSourcing.Brooks.L0Tests` | [ ] | [ ] |
| `EventSourcing.Reducers.Abstractions.L0Tests` | [ ] | [ ] |
| `EventSourcing.Reducers.L0Tests` | [ ] | [ ] |
| `EventSourcing.Serialization.Json.L0Tests` | [ ] | [ ] |
| `EventSourcing.Serialization.L0Tests` | [ ] | [ ] |
| `EventSourcing.Snapshots.Abstractions.L0Tests` | [ ] | [ ] |
| `EventSourcing.Snapshots.Cosmos.L0Tests` | [ ] | [ ] |
| `EventSourcing.Snapshots.L0Tests` | [ ] | [ ] |
| `EventSourcing.UxProjections.Abstractions.L0Tests` | [ ] | [ ] |
| `EventSourcing.UxProjections.Api.Generators.L0Tests` | [ ] | [ ] |
| `EventSourcing.UxProjections.Api.L0Tests` | [ ] | [ ] |
| `EventSourcing.UxProjections.L0Tests` | [ ] | [ ] |
| `Inlet.Abstractions.L0Tests` | [ ] | [ ] |
| `Inlet.Blazor.Server.L0Tests` | [ ] | [ ] |
| `Inlet.Blazor.WebAssembly.L0Tests` | [ ] | [ ] |
| `Inlet.L0Tests` | [ ] | [ ] |
| `Inlet.Orleans.L0Tests` | [ ] | [ ] |
| `Inlet.Orleans.SignalR.L0Tests` | [ ] | [ ] |
| `Reservoir.Abstractions.L0Tests` | [ ] | [ ] |
| `Reservoir.Blazor.L0Tests` | [ ] | [ ] |
| `Reservoir.L0Tests` | [ ] | [ ] |

### L2 Test Projects (Integration)

| Project | Pass 1 | Pass 2 |
|---------|--------|--------|
| `Aqueduct.L2Tests` | [ ] | [ ] |
| `Aqueduct.L2Tests.AppHost` | [ ] | [ ] |

---

## Sample Projects (samples/)

### Cascade (Chat Application Sample)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Directory.Build.props` | MSBuild | [ ] | [ ] |
| `samples/Cascade/docs/event-sourcing-flow.md` | Doc | [ ] | [ ] |

#### Cascade.AppHost

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.AppHost/Cascade.AppHost.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.AppHost/Program.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.AppHost/Properties/launchSettings.json` | Config | [ ] | [ ] |
| `samples/Cascade/Cascade.AppHost/appsettings*.json` | Config | [ ] | [ ] |

#### Cascade.Client (Blazor WebAssembly)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Client/Cascade.Client.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Program.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/App.razor` | Razor | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/MainLayout.razor` | Razor | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/_Imports.razor` | Razor | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Pages/*.razor` | Razor | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Components/**/*.razor` | Razor | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Cart/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Chat/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Client/Services/*.cs` | Source | [ ] | [ ] |

#### Cascade.Contracts

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Contracts/Cascade.Contracts.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Contracts/Api/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Contracts/Projections/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Contracts/Storage/*.cs` | Source | [ ] | [ ] |

#### Cascade.Domain (Event Sourcing Domain)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Domain/Cascade.Domain.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain/CascadeRegistrations.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain/Channel/**/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain/Conversation/**/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain/User/**/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain/Projections/**/*.cs` | Source | [ ] | [ ] |

#### Cascade.Domain.L0Tests

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Domain.L0Tests/Cascade.Domain.L0Tests.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Domain.L0Tests/**/*Tests.cs` | Test | [ ] | [ ] |

#### Cascade.Grains.Abstractions

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Grains.Abstractions/Cascade.Grains.Abstractions.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Grains.Abstractions/*.cs` | Source | [ ] | [ ] |

#### Cascade.L0Tests

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.L0Tests/Cascade.L0Tests.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.L0Tests/**/*.cs` | Test | [ ] | [ ] |

#### Cascade.L2Tests

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.L2Tests/Cascade.L2Tests.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.L2Tests/*.cs` | Test | [ ] | [ ] |

#### Cascade.Server

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Server/Cascade.Server.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Server/Program.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Server/Hubs/MessageHub.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Server/Services/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Server/appsettings*.json` | Config | [ ] | [ ] |

#### Cascade.Silo

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Cascade/Cascade.Silo/Cascade.Silo.csproj` | Project | [ ] | [ ] |
| `samples/Cascade/Cascade.Silo/Program.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Silo/Grains/*.cs` | Source | [ ] | [ ] |
| `samples/Cascade/Cascade.Silo/appsettings*.json` | Config | [ ] | [ ] |

### Crescent (Counter Sample)

| File | Type | Pass 1 | Pass 2 |
|------|------|--------|--------|
| `samples/Crescent/Directory.Build.props` | MSBuild | [ ] | [ ] |
| `samples/Crescent/Crescent.AppHost/Crescent.AppHost.csproj` | Project | [ ] | [ ] |
| `samples/Crescent/Crescent.AppHost/Program.cs` | Source | [ ] | [ ] |
| `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj` | Project | [ ] | [ ] |
| `samples/Crescent/Crescent.L2Tests/*.cs` | Test | [ ] | [ ] |
| `samples/Crescent/Crescent.L2Tests/Domain/**/*.cs` | Source | [ ] | [ ] |

---

## Review Progress Tracking

### Pass 1 Progress

- **Started:** 2026-01-16
- **Current Phase:** ✅ Complete (Key Files)
- **Completed:** 32 critical files reviewed in detail
- **Approach:** Prioritized review of pattern-defining files

### Pass 2 Progress

- **Started:** 2026-01-16
- **Current Phase:** ✅ Complete
- **Completed:** Holistic assessment with cross-cutting analysis
- **Artifacts:** Architecture diagrams, pattern inventory, recommendations
