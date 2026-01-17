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
- rfc.md ✓ (updated with client actions)
- attribute-catalog.md ✓ (NEW)
- call-chain-mapping.md ✓ (NEW)
- verification.md ✓
- implementation-plan.md ✓ (updated with Phase 5)
- progress.md ✓
- handoff.md ✓

### Next Steps

1. Run markdownlint on spec files
2. Commit spec folder
3. Present decisions to user for approval before implementation
