---
applyTo: '**/Aspire*/**/*.cs'
---

# Aspire Integration Testing

Governing thought: Use the preview Cosmos emulator with HTTP mode and SDK workarounds to avoid known connectivity issues.

> Drift check: Check [Aspire Cosmos issues](https://github.com/dotnet/aspire/issues?q=cosmos+emulator) and [Cosmos SDK issues](https://github.com/Azure/azure-cosmos-dotnet-v3/issues) for updates before changing emulator configuration.

## Rules (RFC 2119)

- Cosmos emulator **MUST** use `RunAsPreviewEmulator()` with `WithoutHttpsCertificate()`; legacy Linux emulator has unreliable health checks. Why: Preview emulator has proper HTTP `/ready` endpoint. See [Aspire #7882](https://github.com/dotnet/aspire/issues/7882).
- `CosmosClientOptions` **MUST** set `LimitToEndpoint = true` when connecting to any emulator. Why: SDK hangs trying to discover replicas on single-node emulator. See [SDK #5364](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/5364).
- `CosmosClientOptions` **SHOULD** use `ConnectionMode.Gateway` for emulator connections. Why: Gateway mode is more reliable than Direct TCP for local emulators.
- Cosmos document models **MUST** use `[Newtonsoft.Json.JsonProperty("id")]` not `System.Text.Json` attributes. Why: Cosmos SDK v3 uses Newtonsoft.Json by default; STJ attributes are ignored.
- Aspire test projects **SHOULD** use `IAsyncLifetime` fixture pattern to manage AppHost lifecycle. Why: Ensures proper startup/teardown and resource cleanup.

## Scope and Audience

Developers building Aspire-based integration tests with Azure emulators.

## Mandatory route

For authoring or extending an owned Cosmos emulator integration-test fixture, use
[author-cosmos-integration-tests](../../.agents/skills/author-cosmos-integration-tests/SKILL.md)
with the [local Cosmos binding](../agent-guidance/cosmos-integration-bindings.md).
Read both linked files directly if discovery is unavailable or applicability is
unclear. The five rules above remain effective independently of skill activation.

## References

- [Shared guardrails](shared-policies.instructions.md) and [testing guidance](testing.instructions.md)
- [Source bindings and validation](../agent-guidance/cosmos-integration-bindings.md)
