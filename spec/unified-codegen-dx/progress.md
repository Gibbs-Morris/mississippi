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

1. ~~Run markdownlint on spec files~~ ✓
2. ~~Commit spec folder~~ ✓ (commit c91cbca)
3. ~~Present decisions to user for approval~~ ✓

### Commit

```text
spec(unified-codegen-dx): address user feedback on Orleans isolation

- Corrected call-chain diagrams: SignalR is notification-only, HTTP fetches data
- Added Three-Layer Architecture section showing WASM/ASP.NET/Orleans boundaries
- Expanded RFC with Critical Constraint: Orleans Isolation section
- Added generated project approach to implementation plan
- Updated handoff with isolation diagrams and checklist
- All markdownlint checks pass

Commit: c91cbca
```

## 2026-01-17 (continued - decisions confirmed)

### User Decisions

User confirmed all remaining decisions:

1. **Attribute model**: Keep **separate** for aggregates and projections
   - Rationale: SRP; framework should be pluggable (different projection backends)

2. **DI registration**: Use **compile-time** source generators
   - Rationale: No runtime reflection cost; better AOT support

3. **Client action generation**: **Opt-in** via `[GenerateClientAction]`
   - Rationale: Explicit control; future RBAC properties

4. **Client DTO generation**: **Opt-in** via `[GenerateClientDto]`
   - Same rationale as client actions

5. **Future extensibility**: Reserve **RBAC properties** on attributes
   - Rationale: Attributes will carry authorization metadata (roles, permissions,
     policies) for generated endpoints and actions in future versions

### Documents Updated

1. **rfc.md**:
   - Added "Decisions (Confirmed)" table at top
   - Added "Attribute Design: SRP and RBAC Extensibility" section
   - Added attribute hierarchy diagram (current vs future RBAC)
   - Added opt-in model explanation
   - Reserved RBAC properties in attribute schema
   - Updated Goals to include pluggable architecture and RBAC extensibility

2. **implementation-plan.md**:
   - Updated Phase 4 to use opt-in `[GenerateClientDto]` attribute
   - Added attribute definition with reserved RBAC properties
   - Added migration step to add `[GenerateClientDto]` to projections

3. **handoff.md**:
   - Added "Decisions (Confirmed)" table at top
   - Updated target state to reflect opt-in model
   - Updated Phase 3 to note compile-time approach
   - Updated Phase 4 checklist for opt-in attribute workflow

4. **README.md**:
   - Changed status to "Decisions Made → Ready for Implementation"
   - Added "Decisions (Confirmed)" table
   - Added "Pluggable Architecture" section
   - Updated next steps for Phase 1

### Status

**Ready for Implementation** — All major decisions confirmed. Spec package
complete. Next action: Begin Phase 1 (enable existing generators in Cascade).

## 2026-01-17 (Architect Review & POC Validation)

### Skeptical Architect Review

Performed principal architect review of all claims. Identified potential concern
with cross-project generation pattern (generators emit to current compilation only).

### User Challenge

User correctly challenged: "But how does the dual approach work when for the
server side we need the Orleans SDK to generate the serialization bit via
source gen, which means we can't source gen the server side or am I wrong?"

### Key Insight

Both generators run independently:

- **Orleans generator**: Produces serialization code for original type (stays in Domain)
- **Mississippi generator**: Creates SEPARATE DTO type (Orleans-free)

The concern was not that generators can't coexist, but whether the generator
could read types from a **referenced assembly** (not just source files).

### Proposed Solution

Use `PrivateAssets="all"` pattern:

- `Contracts.Generated` references `Domain` with `PrivateAssets="all"`
- Generator runs in `Contracts.Generated` context
- Generator reads Domain types via `compilation.References`
- `PrivateAssets` prevents Orleans from flowing transitively to downstream projects

### POC Validation

Built proof-of-concept in `.scratchpad/poc-cross-project-gen/`:

**Structure:**

- `Source.Domain/` — Orleans SDK, `[GenerateSerializer]`, `[GenerateClientDto]` marker
- `Source.Generator/` — IIncrementalGenerator scanning referenced assemblies
- `Target.Contracts/` — `PrivateAssets="all"` reference to Domain
- `Target.Client/` — References only Contracts

**Commands:**

```powershell
dotnet build poc.sln -c Release
dotnet run --project Target.Client -c Release
Get-ChildItem Target.Client\bin\Release\net9.0\*.dll | Select-Object Name
```

**Results:**

- ✅ Build succeeded
- ✅ Generator found types in referenced assembly
- ✅ Generated DTO has Orleans attributes stripped
- ✅ Client output contains **zero Orleans DLLs**
- ✅ Orleans generator still produces `Codec_ChannelProjection` in Domain

**Dual Generator Output:**

- `Source.Domain/obj/.../Source.Domain.orleans.g.cs` — Orleans serialization
- `Target.Contracts/obj/.../ChannelProjectionDto.g.cs` — Mississippi DTO

**Client Output DLLs:**

```text
Target.Client.dll
Target.Contracts.dll
(NO Orleans DLLs)
```

### Documents Updated

1. **architect-review.md** — Changed from CONDITIONAL to ✅ APPROVED
2. **verification.md** — Updated C11 claim to VALIDATED with POC evidence
3. **implementation-plan.md** — Updated Phase 4 with validated pattern
4. **README.md** — Updated status to APPROVED

### Status

**✅ Fully Approved for Implementation** — All phases (1-5) validated. Cross-
project generation pattern proven via POC. Next action: Begin Phase 1.

## 2026-01-18 (Naming Integration)

### Project Naming Integration

Integrated `.scratchpad/project-naming/` work into unified codegen spec.

**Files reviewed:**

- `00-boundary-rules.md` — Strict WASM/ASP.NET/Orleans separation rules
- `03-boundary-violations.md` — 7 current violations in codebase
- `04-target-architecture.md` — Project taxonomy with runtime suffixes
- `05-migration-plan.md` — Phase 0-3 migration strategy
- `06-project-names.md` — Complete project/attribute name mappings

### Attribute Naming Convention Adopted

Resolved the architect review's advisory on attribute naming:

| Pattern | Examples | Purpose |
|---------|----------|---------|
| `Generate*` | `[GenerateAggregateService]`, `[GenerateClientDto]` | Triggers source generation |
| `Define*` | `[DefineProjectionPath]`, `[DefineBrookName]` | Assigns identity/metadata |

**Rationale:**

- `Generate*` aligns with Orleans `[GenerateSerializer]` style
- `Define*` follows Orleans identity marker style (`[Alias]`, `[Id]`)

### Phase 0 Added

New Phase 0 (attribute naming alignment) added before Phase 1:

1. Rename `AggregateServiceAttribute` → `GenerateAggregateServiceAttribute`
2. Rename `UxProjectionAttribute` → `GenerateProjectionApiAttribute`
3. Rename `ProjectionPathAttribute` → `DefineProjectionPathAttribute`
4. Update generators to use new attribute names
5. Update all usages in Mississippi source and samples
6. Build and run tests to verify rename is complete

**Note:** Clean rename approach (no backward compatibility shims) since this is
pre-production.

**Ready for Implementation** — Phase 0 (attribute naming) should be done first,
then Phases 1-5 as previously planned.
