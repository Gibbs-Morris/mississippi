---
applyTo: '**/*.cs'
---

# Service Registration Pattern

Governing thought: Use hierarchical `ServiceRegistration` extension methods with options-based overloads and synchronous registration; defer async work to hosted services or Orleans lifecycle participants.

> Drift check: Review DI settings in `Directory.Build.props` and any referenced scripts/config before editing registration code.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [registration and keyed-service contracts](../../src/AGENTS.md#registration-and-keyed-services), [root engineering rules](../../AGENTS.md#engineering) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For registration work, contributors **MUST** follow [register-dotnet-services](../../.agents/skills/register-dotnet-services/SKILL.md) with [its local binding](../agent-guidance/service-registration-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Developers adding or modifying DI registration in Mississippi/Samples, including Orleans integrations.
