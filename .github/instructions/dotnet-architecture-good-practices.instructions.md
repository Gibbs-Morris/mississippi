---
description: "DDD and .NET architecture guidelines"
applyTo: '**/*.cs,**/*.csproj,**/Program.cs,**/*.razor'
---

# DDD and Architecture Checklist

Governing thought: Start every domain change with explicit DDD/SOLID analysis, keep logic in the right layer, and verify tests/observability before shipping.

> Drift check: When citing scripts or configs (build/test/logging), open the referenced files first; they remain authoritative.

## Rules (RFC 2119)

- Domain-sensitive work **MUST** start with a written analysis listing bounded context, aggregates/value objects/services/events, applicable patterns, and security/compliance impacts. Why: Prevents ad-hoc design.
- Before coding, agents **MUST** plan which aggregates/value objects/domain services/events and tests will change. Why: Aligns ubiquitous language and verification.
- Domain logic **MUST** stay inside aggregates/value objects/domain services; application services **MUST** remain orchestration; infrastructure concerns **MUST** stay isolated per service-registration guidance. Why: Preserves clean layering.
- Test strategy **MUST** follow testing instructions (PascalCase test names, L0-first, coverage >=80% overall/target 95-100% where feasible, 100% on touched code, proportionate mutation testing as an additional signal). Why: Ensures consistent verification.
- Financial rules **MUST** use decimal-based value objects with explicit rounding and recorded domain events. Why: Protects audit/compliance.
- After implementation, agents **MUST** confirm SOLID adherence, event publication, security boundaries, and documentation/tasks before marking done. Why: Enforces exit criteria.

## Scope and Audience

Engineers modifying domain/application/infrastructure/UI shells where DDD or SOLID choices matter.

## References

- C#: `.github/instructions/csharp.instructions.md`
- Service registration: `.github/instructions/service-registration.instructions.md`
- Testing/mutation: `.github/instructions/testing.instructions.md`
- Logging: `.github/instructions/logging-rules.instructions.md`
