---
applyTo: '**/Aspire*/**/*.cs'
---

# Aspire Integration Testing

Governing thought: Use the preview Cosmos emulator with HTTP mode and SDK workarounds to avoid known connectivity issues.

> Drift check: Check [Aspire Cosmos issues](https://github.com/dotnet/aspire/issues?q=cosmos+emulator) and [Cosmos SDK issues](https://github.com/Azure/azure-cosmos-dotnet-v3/issues) for updates before changing emulator configuration.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [Aspire integration contracts](../../src/AGENTS.md#aspire-integration) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For authoring or extending an owned Cosmos emulator integration-test fixture, contributors **MUST** follow [author-cosmos-integration-tests](../../.agents/skills/author-cosmos-integration-tests/SKILL.md) with [its local binding](../agent-guidance/cosmos-integration-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Developers building Aspire-based integration tests with Azure emulators.
