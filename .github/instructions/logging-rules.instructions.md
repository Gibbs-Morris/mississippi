---
applyTo: '**/*.cs'
---

# Logging (LoggerExtensions) Standard

Governing thought: All logging goes through `[LoggerMessage]` LoggerExtensions for zero-allocation performance and consistent observability.

> Drift check: Open referenced LoggerExtensions files or logging configs before editing; scripts/configs remain authoritative for levels and providers.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [logging contracts](../../src/AGENTS.md#logging), [root engineering rules](../../AGENTS.md#engineering) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For adding or converting logging calls, contributors **MUST** follow [add-dotnet-source-generated-logging](../../.agents/skills/add-dotnet-source-generated-logging/SKILL.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All C# contributors emitting logs (services, grains, libraries).
