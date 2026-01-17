# Progress Log

## 2026-01-17

### Initial Analysis

- Reviewed existing generators: `AggregateServiceGenerator`, `ProjectionApiGenerator`
- Examined Cascade sample structure: Domain, Contracts, Client, Server, Silo
- Identified manual DTO duplication pattern in Cascade.Contracts
- Analyzed attribute model: `[AggregateService]`, `[UxProjection]`, `[ProjectionPath]`

### Key Findings

1. **AggregateServiceGenerator** (in `EventSourcing.Aggregates.Generators`):
   - Scans `[AggregateService]` attribute on aggregate types
   - Discovers `CommandHandler<TCommand, TAggregate>` implementations
   - Generates: `I{Name}Service`, `{Name}Service`, `{Name}Controller`
   - Uses route from attribute for API paths

2. **ProjectionApiGenerator** (in `EventSourcing.UxProjections.Api.Generators`):
   - Scans `[UxProjection]` attribute on projection types
   - Generates: `{Name}Dto`, `{Name}MappingExtensions.ToDto()`, `{Name}Controller`
   - Strips Orleans `[Id]`/`[GenerateSerializer]` from DTOs
   - Route comes from `[ProjectionPath]` attribute

3. **Current Cascade Structure**:
   - `Cascade.Domain` - Orleans projections with `[Id]`, `[GenerateSerializer]`
   - `Cascade.Contracts` - Manual WASM-safe DTOs with `[ProjectionPath]`
   - `Cascade.Client` - References Contracts, not Domain
   - `Cascade.Server` - References both Domain and Contracts

4. **Dependency Flow**:

   ```text
   Domain (Orleans) → Server ← Contracts (WASM-safe) ← Client (WASM)
   ```

### Verification Phase

- Confirmed only `UserAggregate` has `[AggregateService]` (grep verified)
- Confirmed `Cascade.Server` uses manual endpoints, not generated services
- Confirmed generated DTOs exist but client uses manual Contracts DTOs
- Confirmed 9 DTOs in Contracts duplicate Domain projections
- Confirmed 332 lines / 80+ Add* calls in CascadeRegistrations.cs

### RFC Drafted

Created RFC with three options:

- Option A: Multi-output generator with Contracts project output
- Option B: Shared abstractions with type derivation
- **Option C (Recommended)**: Unified attribute + analyzer-linked generation

### Expanded Scope

Added new requirements from user:

- Complete attribute inventory with SRP analysis
- Full call-chain mapping (UX → Cosmos → Client state)
- Client-side action generation with `[GenerateClientAction]`

### New Documents Created

- `attribute-catalog.md` — complete inventory of all 6 Mississippi attributes
- `call-chain-mapping.md` — write path, read path, update path with code traces

### Implementation Plan Updated

Five-phase approach:

1. Enable existing generators in Cascade
2. Verify projection DTO generation
3. Create DI registration generator
4. Create client DTO generator
5. Create client action generator (NEW)

### Spec Files Complete

- README.md ✓
- learned.md ✓
- rfc.md ✓
- verification.md ✓
- implementation-plan.md ✓
- attribute-catalog.md ✓
- call-chain-mapping.md ✓
- progress.md ✓
- handoff.md ✓

### Commit

```text
spec(unified-codegen-dx): initial spec package

- RFC with three options (recommend Option C)
- Implementation plan with 5 phases
- Attribute catalog (6 attributes)
- Call-chain mapping (write/read/update paths)
- Client action generation design

Commit: 65ad767
```

## 2026-01-17 (continued)

### User Feedback

User clarified key architectural points:

1. **SignalR vs HTTP**: "Projection read is done via HTTP not SignalR, but we
   get a notification on SignalR the version has updated - this is key."

2. **Inlet Pattern**: "Inlet has a bunch of effects/actions/store to manage
   server side state."

3. **Generated Project Preference**: "I think I like the generated project
   approach."

4. **Critical Concern**: "Biggest concern is the Orleans attributes leaking
   into client code and HTTP code, as WASM/ASP.NET/Orleans are 3 different
   things running in different pods/envs."

### Verification of User Feedback

Traced code to verify SignalR/HTTP pattern:

- `InletSignalREffect.cs:357-381` — `OnProjectionUpdatedAsync(path, entityId, newVersion)` receives SignalR notification, then calls `FetchAtVersionAsync()` via HTTP
- `AutoProjectionFetcher.cs:76-101` — Uses `HttpClient` to GET `/api/projections/{path}/{entityId}/at/{version}`

**CONFIRMED**: SignalR is notification-only (lightweight); HTTP fetches actual data.

### Documents Updated

1. **call-chain-mapping.md**:
   - Added "Three-Layer Architecture" diagram
   - Added "Key Insight: SignalR vs HTTP" section
   - Corrected Read Path and Update Path diagrams to show HTTP for data fetch
   - Added "Notification vs Data Separation" explanation

2. **rfc.md**:
   - Expanded Goals to emphasize Orleans isolation across three deployment targets
   - Added "Critical Constraint: Three-Layer Isolation" section
   - Added "Deployment Boundary Model" diagram
   - Added "How Generators Enforce Isolation" with concrete rules
   - Added "Build-Time Validation" diagnostics

3. **implementation-plan.md**:
   - Added user preference note for generated project approach
   - Added "Generated Project Architecture" section with file layout
   - Added project reference diagram showing Orleans-free zone
   - Added "Key Isolation Mechanisms" (AnalyzerReference, attribute stripping)

4. **learned.md**:
   - Added "SignalR vs HTTP Pattern" verified section with code evidence
   - Added "Three-Layer Deployment Model" verified section

### Next Steps

1. Run markdownlint on spec files
2. Commit spec folder
3. Present decisions to user for approval before implementation
