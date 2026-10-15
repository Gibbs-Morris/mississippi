# Cosmos integration-test local binding

Read this binding with [author-cosmos-integration-tests](../../.agents/skills/author-cosmos-integration-tests/SKILL.md)
when authoring Mississippi Cosmos emulator fixtures. The [Aspire rules](../instructions/aspire.instructions.md#rules-rfc-2119)
retain their scope and authority. If discovery is unavailable, read both linked
files directly; this binding does not replace instruction loading or quality gates.

## Inspect current sources

| Concern | Current source and boundary |
| --- | --- |
| Owned vNext emulator | [Spring AppHost](../../samples/Spring/Spring.AppHost/Program.cs) calls `RunAsEmulator`, `WithDataExplorer`, and `WithoutHttpsCertificate`, creates `spring-db`, and makes the runtime wait for Cosmos. |
| Client transport | [Spring runtime](../../samples/Spring/Spring.Runtime/Program.cs) configures `ConnectionMode.Gateway` and `LimitToEndpoint = true`; its Aspire account client is forwarded to the local `spring-cosmos` key. |
| Startup and teardown | [SpringApplicationFixture](../../samples/Spring/Spring.TestHarness/SpringApplicationFixture.cs) uses xUnit `IAsyncLifetime`, one three-minute startup budget, owned async disposal, partial-startup cleanup, and resource logs. |
| Consuming tests | [Spring L2 collection](../../samples/Spring/Spring.L2Tests/SpringApiCollectionDefinition.cs) shares that fixture; [BankAccountIntegrationTests](../../samples/Spring/Spring.L2Tests/BankAccountIntegrationTests.cs) exercises storage-backed API behavior. This is the existing reference, replacing the absent Crescent Aspire test path. |
| Event document | [EventDocument](../../src/Brooks.Runtime.Storage.Cosmos/Storage/EventDocument.cs) uses Newtonsoft `JsonProperty("id")` and `JsonProperty("brookPartitionKey")`; [provider registration](../../src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs) requires `/brookPartitionKey` and refuses to delete mismatched existing containers. |
| Snapshot document | [SnapshotDocument](../../src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotDocument.cs) uses Newtonsoft `id` and `snapshotPartitionKey`; inspect its provider/initializer before binding a snapshot test. |
| Placement and runner | [Spring test map](../../samples/Spring/TESTING.md), [testing rules](../instructions/testing.instructions.md), and [shared props](../../Directory.Build.props) define L2 infrastructure, shared TestHarness, xUnit v3/MTP, deterministic tests, and warnings-as-errors. Browser journeys belong in L3. |

These are source bindings, not a claim that a new direct Cosmos fixture already
exists or that the reference passed on this revision. Preserve business/runtime
behavior when authoring tests. Existing AppHost experimental `#pragma` directives
are not permission to introduce new suppressions; approval rules still apply.

## Version and serialization boundary

At the extraction base, [global.json](../../global.json) selects SDK `10.0.401`
with roll-forward disabled. [Central packages](../../Directory.Packages.props)
configure Cosmos `3.62.1` and Aspire `13.5.3`; [Spring AppHost project](../../samples/Spring/Spring.AppHost/Spring.AppHost.csproj)
also selects Aspire AppHost SDK `13.5.3` and the CLI bundle. Recheck manifests,
resolved lockfiles, available tools, and actual APIs before authoring.

The runtime registers a Cosmos client without a custom document serializer in
that registration. The storage document attributes above use Newtonsoft. Its
separate `AddJsonSerialization()` call is the event payload serialization seam,
not evidence that Cosmos honors System.Text.Json document attributes. Inspect
the actual client serializer and any nearer overrides; keep the local document
rule and report a conflict rather than silently replacing the serializer.

At the 4 October synchronization base `c8da151e607bcc8f3b253519317a8e7418d26261`,
central packages select Cosmos `3.63.2` and Aspire hosting integrations `13.6.0`.
The AppHost SDK declaration remains `13.5.3`; this is a separate input from the
hosting package versions. SDK `10.0.401` remains selected. The inspected Spring
AppHost uses `RunAsEmulator()`, keeps `WithDataExplorer()` explicit because Data
Explorer is opt-in, and calls `WithoutHttpsCertificate()` under its existing
`ASPIRECERTIFICATES001` diagnostic scope. Aspire 13.6 makes the Linux vNext
emulator the default and deprecates `RunAsPreviewEmulator()`; see the
[13.6 migration notes](https://aspire.dev/whats-new/aspire-13-6/#breaking-changes).
This source refresh does not migrate packages or authorize new suppressions.

## Validation and operational boundary

Follow [verify-change and local checks](../instructions/testing.instructions.md#change-verification)
for required completion gates. The root [Spring command](../../test-spring.ps1)
and [README](../../README.md#validate-spring-after-a-change) define prerequisites
and evidence:

- `pwsh ./test-spring.ps1 -Doctor` checks selected SDK and Docker Linux access; READY does not execute tests or prove emulator startup.
- `pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full` builds the selected Spring API/infrastructure tests with locked restore, then executes them against their owned AppHost.
- Inspect the emitted SUMMARY, nonempty selected/passing counts, TRX, build/test/resource logs, and cleanup result. Missing, skipped, or empty tests cannot establish PASS.

Do not run browser suites merely because an L2 fixture uses Aspire. Separate
worktrees protect mutable build state; the fixture owns its containers. Do not
use process-name cleanup or Docker prune. A new direct storage fixture may need
its own locally supported selector and evidence contract; Spring API results do
not prove assertions that were never included.

## Retained issue-reference limitation

The extraction originally retained the old issue links. On September 26, 2026,
[Aspire #7882](https://github.com/microsoft/aspire/issues/7882) described dashboard
resource-graph configuration, so it does not substantiate the rule's health-check
rationale. The [SDK #5364](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/5364)
page was unavailable during extraction; its current status was not verified.
Use inspected local configuration as evidence of configured behavior and check
current primary sources before changing emulator policy. The 4 October synchronization retains the five current-main rules and their refreshed citations; the historical issue observations do not prove a current SDK defect.
