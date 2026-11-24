---
applyTo: '**/*.cs'
---

# Keyed Services for Storage Providers

Governing thought: Use keyed DI services for storage clients so multiple instances (Cosmos, Blob, Redis, etc.) can coexist in a single host for different purposes.

> Drift check: Review module-owned `*Defaults` types and Aspire registration patterns before adding new keyed services.

## Rules (RFC 2119)

- Library code that consumes cloud clients (BlobServiceClient, CosmosClient, Container, etc.) **MUST** use `[FromKeyedServices(<ModuleDefaults>.XxxServiceKey)]` on constructor parameters rather than expecting an unkeyed registration. Why: Enterprise apps require multiple storage accounts for different purposes (locking, state, uploads, archival).
- Service keys **MUST** be module-owned in the package that defines the storage contract (for example `BrookCosmosDefaults`, `SnapshotCosmosDefaults`) and **MUST NOT** be centralized in a cross-module defaults hub. Why: Keeps ownership explicit and avoids accidental coupling.
- Key constants **MUST** follow the pattern `"mississippi-{client-type}-{feature}"` (e.g., `"mississippi-cosmos-brooks"`, `"mississippi-blob-locking"`). Why: Provides unique, discoverable identifiers.
- Registration documentation **MUST** comment which keyed services the library expects callers to provide. Why: Clarifies the DI contract.
- Host applications **MUST** forward from their registration key (e.g., Aspire's `"cosmos"`, `"blobs"`) to the library's expected key using `AddKeyedSingleton`. Why: Decouples host naming from library requirements.
- When a host needs both keyed (for library) and unkeyed (for its own services), it **MUST** explicitly forward using `AddSingleton(sp => sp.GetRequiredKeyedService<T>("key"))`. Why: Makes DI resolution explicit.

## Scope and Audience

Library authors and host developers integrating Mississippi with cloud storage or external services.

## Registration workflow

Use [register-dotnet-services](../../.agents/skills/register-dotnet-services/SKILL.md) with the [local source bindings](../agent-guidance/service-registration-bindings.md).
If discovery is unavailable or applicability is unclear, read both files directly; the Rules above remain effective independently of skill activation.

## References

- Service registration: `.github/instructions/service-registration.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
