---
applyTo: '**/*.cs'
---

# Keyed Services for Storage Providers

Governing thought: Use keyed DI services for storage clients so multiple instances (Cosmos, Blob, Redis, etc.) can coexist in a single host for different purposes.

> Drift check: Review module-owned `*Defaults` types and Aspire registration patterns before adding new keyed services.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [registration and keyed-service contracts](../../src/AGENTS.md#registration-and-keyed-services) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For registration work, contributors **MUST** follow [register-dotnet-services](../../.agents/skills/register-dotnet-services/SKILL.md) with [its local binding](../agent-guidance/service-registration-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Library authors and host developers integrating Mississippi with cloud storage or external services.
