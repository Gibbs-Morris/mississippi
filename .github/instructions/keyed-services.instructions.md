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

## At-a-Glance Quick-Start

### Library Side

```csharp
// Use module-owned keys from Brooks storage defaults
public BlobDistributedLockManager(
    [FromKeyedServices(BrookCosmosDefaults.BlobLockingServiceKey)]
    BlobServiceClient blobServiceClient,
    ILogger<BlobDistributedLockManager> logger) { }

// Document in registration comments
// Caller must register a keyed BlobServiceClient with BrookCosmosDefaults.BlobLockingServiceKey
services.AddSingleton<IDistributedLockManager, BlobDistributedLockManager>();
```

### Host Side (Aspire)

```csharp
// Register with Aspire key
builder.AddKeyedAzureBlobServiceClient("blobs");

// Forward to library key
builder.Services.AddKeyedSingleton(
    BrookCosmosDefaults.BlobLockingServiceKey,
    (sp, _) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

// If host also needs unkeyed for its own services
builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));
```

## Core Principles

- One client type, many instances: keyed services enable coexistence.
- Keys are defined with module ownership alongside the consuming storage provider.
- Library keys are stable contracts; host keys are deployment-specific.
- Explicit forwarding makes the DI graph auditable.

## Module-Owned Key Reference

| Key | Value | Purpose |
|-----|-------|---------|
| `BrookCosmosDefaults.CosmosContainerServiceKey` | `"mississippi-cosmos-brooks"` | Cosmos container for event streams |
| `SnapshotCosmosDefaults.CosmosContainerServiceKey` | `"mississippi-cosmos-snapshots"` | Cosmos container for snapshots |
| `BrookCosmosDefaults.BlobLockingServiceKey` | `"mississippi-blob-locking"` | Blob storage for distributed locking |

See also module-owned storage/container defaults (for example `BrookCosmosDefaults.ContainerId`, `SnapshotCosmosDefaults.ContainerId`).

## References

- Service registration: `.github/instructions/service-registration.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
