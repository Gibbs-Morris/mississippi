---
applyTo: 'tests/**/*.cs'
---

# Allure Test Naming

Governing thought: Use consistent Allure attributes to map tests to architecture/backlog with stable, human-friendly names.

> Drift check: Verify Allure.Xunit attribute behavior in release notes if packages change.

## Rules (RFC 2119)

- Each test/class **MUST** have exactly one `[AllureParentSuite]`, `[AllureSuite]`, and `[AllureSubSuite]` representing bounded context, component, and capability. Why: Keeps reports navigable.
- Names **MUST** be stable Title Case domain terms without timestamps/IDs; `[AllureId]` values **MUST** be unique/stable. Why: Enables history and trend analysis.
- Backlog axis (`[AllureEpic]`, `[AllureFeature]`, `[AllureStory]`) **MAY** be omitted; when used, a single Epic **SHOULD** be set and Feature/Story **SHOULD** be singular (multiples only when documented). Why: Prevents fragmentation.
- Use either xUnit `DisplayName` or `[AllureName]` (not both with conflicting values); `[AllureName]` **MUST** align with the display name. Why: Single source of truth.
- Tags **MUST** follow `key:value`; `[AllureOwner]` **MUST** be a team/alias, not an individual. Why: Supports filtering and durable ownership.
- Step descriptions **MUST** be verb phrases in domain language; parameters containing sensitive data **MUST** be masked/omitted. Why: Readable and safe reports.
- Tests spanning multiple bounded contexts **MUST** be split so each covers one slice. Why: Clear ownership and diagnostics.

## Scope and Audience

Test authors using Allure.Xunit under `tests/`.
Allure is only used with L0Tests and L1Tests.

## At-a-Glance Quick-Start

- Set ParentSuite = bounded context; Suite = component; SubSuite = capability.
- Optional backlog: Epic/Feature/Story when using PM tooling.
- Prefer `DisplayName`; add `[AllureName]` only when needed.
- Use `AllureTag("priority:high")`, `AllureOwner("team-xyz")`, stable `[AllureId]`.

## Core Principles

- Architecture axis is required; backlog axis is optional.
- Names must be stable, domain-driven, and unique.
- Metadata should aid filtering without duplication.
