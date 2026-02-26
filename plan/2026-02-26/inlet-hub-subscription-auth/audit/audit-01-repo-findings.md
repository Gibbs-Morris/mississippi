# 01 — Repository Findings

## Search Terms Used

- `GenerateAuthorizationAttribute`, `GenerateAllowAnonymousAttribute`, `GeneratedApiAuthorizationConvention`
- `InletHub`, `SubscribeAsync`, `MapInletHub`, `IInletHubClient`
- `IProjectionBrookRegistry`, `ProjectionBrookRegistry`, `ScanProjectionAssemblies`
- `GeneratedApiAuthorizationMode`, `GeneratedApiAuthorizationOptions`, `InletServerOptions`
- `ProjectionEndpointsGenerator`, `GeneratedApiAuthorizationAnalysis`, `GeneratedApiAuthorizationModel`
- `ProjectionPathAttribute`, `BrookNameAttribute`
- `HubException`, `InletSignalRActionEffect`

## Areas Inspected

- `src/Inlet.Gateway/` — Hub, registrations, MVC convention, auth options
- `src/Inlet.Gateway.Abstractions/` — Hub constants, notifier interfaces
- `src/Inlet.Runtime/` — Silo registrations, projection brook registry
- `src/Inlet.Runtime.Abstractions/` — `IProjectionBrookRegistry` interface
- `src/Inlet.Abstractions/` — `ProjectionPathAttribute`
- `src/Inlet.Generators.Abstractions/` — `GenerateAuthorizationAttribute`, `GenerateAllowAnonymousAttribute`, `GenerateProjectionEndpointsAttribute`
- `src/Inlet.Generators.Core/Analysis/` — `GeneratedApiAuthorizationAnalysis`, `GeneratedApiAuthorizationModel`
- `src/Inlet.Gateway.Generators/` — `ProjectionEndpointsGenerator`
- `src/Inlet.Client/ActionEffects/` — `InletSignalRActionEffect`
- `samples/Spring/Spring.Domain/` — All projection and command files
- `samples/Spring/Spring.Server/` — `Program.cs` registration
- `tests/Inlet.Gateway.L0Tests/` — Existing convention tests

---

## Finding 1: InletHub Has Zero Authentication or Authorization

**Evidence:**
- `src/Inlet.Gateway/InletHub.cs` (lines 31–125): Class has NO `[Authorize]` attribute. `SubscribeAsync(path, entityId)` validates non-null inputs but performs no auth check. Any connected client can subscribe to any projection path.
- `src/Inlet.Gateway/InletServerRegistrations.cs` (lines 92–103): `MapInletHub()` returns `endpoints.MapHub<InletHub>(pattern)` with no `.RequireAuthorization()` chaining.

**Second Source:**
- `tests/Inlet.Gateway.L0Tests/` — no tests for hub-level auth; existing auth convention tests (`GeneratedApiAuthorizationConventionTests.cs`) only test MVC controllers.

**Implication:**
- This is the core gap. Two layers are needed: (1) hub-level auth (connection requires authentication), (2) per-subscription auth (individual `SubscribeAsync` calls checked against projection auth metadata).

---

## Finding 2: GeneratedApiAuthorizationConvention Is MVC-Only

**Evidence:**
- `src/Inlet.Gateway/GeneratedApiAuthorizationConvention.cs` (lines 17–158): Implements `IApplicationModelConvention` — MVC-only. Checks for `[GeneratedCode]` from `AggregateControllerGenerator`, `ProjectionEndpointsGenerator`, `SagaControllerGenerator`. Applies `AuthorizeFilter` to generated controllers when mode is `RequireAuthorizationForAllGeneratedEndpoints`.
- `src/Inlet.Gateway/GeneratedApiAuthorizationMvcOptionsSetup.cs` (lines 1–45): Wired via `IConfigureOptions<MvcOptions>` — explicitly MVC-scoped.

**Second Source:**
- `src/Inlet.Gateway/InletServerRegistrations.cs` (line 53): `TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, GeneratedApiAuthorizationMvcOptionsSetup>())` — confirms MVC-only wiring.

**Implication:**
- The convention pattern cannot be extended to cover SignalR hubs. A separate mechanism is needed for hub auth — either hub endpoint configuration (`RequireAuthorization()`) or hub-internal auth checks.

---

## Finding 3: IProjectionBrookRegistry Is the Pattern Precedent

**Evidence:**
- `src/Inlet.Runtime.Abstractions/IProjectionBrookRegistry.cs`: Interface with `Register(path, brookName)`, `TryGetBrookName(path, out brookName)`, `GetAllPaths()`, `GetBrookName(path)`.
- `src/Inlet.Runtime/ProjectionBrookRegistry.cs`: Implementation uses `ConcurrentDictionary<string, string>`.

**Second Source:**
- `src/Inlet.Runtime/InletSiloRegistrations.cs` (lines 73–99): `ScanProjectionAssemblies()` iterates assembly types, reads `[ProjectionPath]` + `[BrookName]`, calls `registry.Register()`. Result replaces singleton `IProjectionBrookRegistry`.

**Implication:**
- A parallel `IProjectionAuthorizationRegistry` following the exact same pattern (interface in Abstractions, impl in Runtime, populated during `ScanProjectionAssemblies`) is the natural and consistent approach.

---

## Finding 4: GenerateAuthorizationAttribute Is Available at Runtime

**Evidence:**
- `src/Inlet.Generators.Abstractions/GenerateAuthorizationAttribute.cs`: `[AttributeUsage(AttributeTargets.Class)]` with `Policy`, `Roles`, `AuthenticationSchemes` properties. Targets `netstandard2.0`.
- `samples/Spring/Spring.Domain/Spring.Domain.csproj` (line 20): `<ProjectReference Include="...\Inlet.Generators.Abstractions.csproj" />` — domain assemblies reference this project. The attribute is available in the compiled assembly at runtime.

**Second Source:**
- `src/Inlet.Generators.Abstractions/GenerateAllowAnonymousAttribute.cs`: Same pattern — netstandard2.0, no dependencies, attribute-only.
- `src/Inlet.Generators.Abstractions/Inlet.Generators.Abstractions.csproj`: Zero dependencies, `netstandard2.0` — a pure contracts package.

**Implication:**
- `ScanProjectionAssemblies()` can read `[GenerateAuthorization]` and `[GenerateAllowAnonymous]` via reflection at runtime from the scanned assemblies. However, `Inlet.Runtime` does NOT currently reference `Inlet.Generators.Abstractions`. Either: (a) add that reference, (b) use reflection by attribute name, or (c) have the scan logic live in `Inlet.Gateway` which already has indirect access.

---

## Finding 5: Inlet.Runtime Does NOT Reference Inlet.Generators.Abstractions

**Evidence:**
- `src/Inlet.Runtime/Inlet.Runtime.csproj`: References `Aqueduct.Abstractions`, `DomainModeling.Runtime`, `Brooks.Abstractions`, `Inlet.Abstractions`, `Inlet.Runtime.Abstractions`, `Inlet.Gateway.Abstractions`. Notably absent: `Inlet.Generators.Abstractions`.

**Second Source:**
- `src/Inlet.Generators.Abstractions/Inlet.Generators.Abstractions.csproj`: `netstandard2.0`, zero dependencies — adding it as a reference would not introduce any transitive dependency burden.

**Implication:**
- Three options: (1) Add `Inlet.Generators.Abstractions` as a reference to `Inlet.Runtime` — safe since it's dependency-free netstandard2.0. (2) Scan by attribute name via reflection (fragile). (3) Move scanning to `Inlet.Gateway` which is the gateway layer and more architecturally appropriate for auth concerns. Option (1) is simplest and follows the existing pattern where `Inlet.Runtime` already references `Inlet.Abstractions` (which contains `ProjectionPathAttribute`).

---

## Finding 6: ScanProjectionAssemblies Is the Natural Extension Point

**Evidence:**
- `src/Inlet.Runtime/InletSiloRegistrations.cs` (lines 73–99): Already iterates exported types, reads `[ProjectionPath]`, `[BrookName]`. Adding reads for `[GenerateAuthorization]` and `[GenerateAllowAnonymous]` in the same loop is trivial.

**Second Source:**
- `samples/Spring/Spring.Server/Program.cs` (line 121): `builder.Services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly)` — single call, all metadata scanned. Users don't need to change their registration.

**Implication:**
- Zero API change for consumers. The assembly scan populates both the brook registry and the auth registry simultaneously. Domain developers keep writing `[GenerateAuthorization]` on their projection classes; the framework picks it up automatically.

---

## Finding 7: GeneratedApiAuthorizationOptions Already Has All Config Fields

**Evidence:**
- `src/Inlet.Gateway/GeneratedApiAuthorizationOptions.cs`: `Mode`, `DefaultPolicy`, `DefaultRoles`, `DefaultAuthenticationSchemes`, `AllowAnonymousOptOut`. These same values drive the MVC convention.

**Second Source:**
- `src/Inlet.Gateway/GeneratedApiAuthorizationConvention.cs` (lines 140–158): `CreateDefaultAuthorizeFilter()` reads `DefaultPolicy`, `DefaultRoles`, `DefaultAuthenticationSchemes` from options to build `AuthorizeAttribute`.

**Implication:**
- The same options should drive hub auth behavior. No new configuration surface needed. When `Mode == RequireAuthorizationForAllGeneratedEndpoints`: (1) `MapInletHub()` chains `.RequireAuthorization()` with the default policy/roles/schemes, (2) per-projection auth checks in `SubscribeAsync` enforce projection-specific overrides, (3) `AllowAnonymousOptOut` controls whether `[GenerateAllowAnonymous]` projections are exempt.

---

## Finding 8: Client Gracefully Handles Subscribe Errors

**Evidence:**
- `src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs` (lines ~270–310): `HandleSubscribeAsync` wraps `HubConnection.InvokeAsync<string>("SubscribeAsync", path, entityId, cancellationToken)` in try/catch. Catches `Exception`, yields `ProjectionActionFactory.CreateError(projectionType, entityId, ex)` to the Redux store.

**Second Source:**
- `src/Inlet.Gateway.Abstractions/InletHubConstants.cs` (line 28): `ProjectionUpdatedMethod = "ProjectionUpdated"` — push notification only, no payload. Combined with the error handling, the client pattern is: subscribe → if error, dispatch error action → UI handles.

**Implication:**
- Throwing `HubException` from `InletHub.SubscribeAsync()` on auth failure is the correct approach. The client will catch it and dispatch an error action. No client code changes needed.

---

## Finding 9: GeneratedApiAuthorizationAnalysis Extracts All Auth Metadata

**Evidence:**
- `src/Inlet.Generators.Core/Analysis/GeneratedApiAuthorizationAnalysis.cs` (lines 50–107): `Analyze(typeSymbol, ...)` resolves `[GenerateAuthorization]` → `GeneratedApiAuthorizationModel` with Policy, Roles, AuthenticationSchemes, HasAuthorize, HasAllowAnonymous.

**Second Source:**
- `src/Inlet.Generators.Core/Analysis/GeneratedApiAuthorizationModel.cs` (lines 1–80): Immutable model. `HasAnyAuthorizationMetadata` = `HasAuthorize || HasAllowAnonymous`.
- `src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs` (line 243): `AppendAuthorizationAttributes(sb, projection.Authorization)` — same model used in code generation.

**Implication:**
- At generator time, auth metadata is fully resolved. At runtime, we need an equivalent model for the auth registry — but simpler since we only need Policy/Roles/AuthenticationSchemes/AllowAnonymous per path, not diagnostics.

---

## Finding 10: Spring.Domain Demonstrates the Full DevEx Pattern

**Evidence (5 projection files):**
- `BankAccountBalanceProjection`: `[GenerateProjectionEndpoints]` — no auth.
- `BankAccountLedgerProjection`: `[GenerateProjectionEndpoints]` — no auth.
- `FlaggedTransactionsProjection`: `[GenerateProjectionEndpoints]` — no auth.
- `MoneyTransferStatusProjection`: `[GenerateProjectionEndpoints]` — no auth.
- `AuthProofProjection`: `[GenerateProjectionEndpoints]`, `[GenerateAuthorization(Policy = "spring.auth-proof.claim")]` — auth via attribute.

**Second Source:**
- `samples/Spring/Spring.Server/Program.cs` (lines 95–120): When auth enabled, configures `Mode = RequireAuthorizationForAllGeneratedEndpoints`, `DefaultPolicy = "spring.generated-api"`, `AllowAnonymousOptOut = true`.

**Implication:**
- After implementation: `AuthProofProjection` subscription would require policy `"spring.auth-proof.claim"`. The other 4 projections, with force mode on, would require the default policy `"spring.generated-api"`. No domain code changes. This is the pit-of-success: auth works for both explicit and global modes without domain awareness of SignalR.

---

## Finding 11: InletHub.SubscribeAsync Has Access to Context.User

**Evidence:**
- `src/Inlet.Gateway/InletHub.cs` (line 31): `Hub<IInletHubClient>` — inherits from `Hub` which exposes `Context.User` (the `ClaimsPrincipal`).
- Standard ASP.NET Core SignalR behavior: `Context.User` is populated from the connection's authentication state.

**Second Source:**
- Hub also has `Context.ConnectionId` (already used in lines 59, 72, 97, 111) — confirms standard Hub lifecycle access. `IAuthorizationService` is available via DI injection into the hub constructor.

**Implication:**
- `InletHub` can accept `IAuthorizationService` via constructor injection and evaluate per-projection authorization policies against `Context.User` in `SubscribeAsync`.

---

## Finding 12: MapInletHub Can Conditionally Chain RequireAuthorization

**Evidence:**
- `src/Inlet.Gateway/InletServerRegistrations.cs` (lines 92–103): Returns `HubEndpointConventionBuilder` which supports `.RequireAuthorization()` chaining.

**Second Source:**
- ASP.NET Core documentation: `HubEndpointConventionBuilder` inherits endpoint conventions including `RequireAuthorization(policy)` and `AllowAnonymous()`.

**Implication:**
- `MapInletHub()` can conditionally call `.RequireAuthorization()` based on `InletServerOptions.GeneratedApiAuthorization.Mode`. However, this requires access to `IOptions<InletServerOptions>` which isn't available in the extension method (it takes `IEndpointRouteBuilder`). Options: (1) Resolve from `IEndpointRouteBuilder.ServiceProvider`, (2) Accept options as parameter, (3) Use a separate extension method.

---

## Finding 13: Architecture and Layer Placement

**Evidence (project references):**
- `Inlet.Runtime` → `Inlet.Abstractions`, `Inlet.Runtime.Abstractions`, `Inlet.Gateway.Abstractions` — silo/grain layer
- `Inlet.Gateway` → `Inlet.Runtime`, `Inlet.Gateway.Abstractions`, `Aqueduct.Gateway` — ASP.NET gateway layer
- `Inlet.Runtime.Abstractions` has the `IProjectionBrookRegistry` interface
- `Inlet.Runtime` has `ProjectionBrookRegistry` implementation and `ScanProjectionAssemblies`

**Second Source:**
- `src/Inlet.Gateway/InletHub.cs` — gateway layer, where auth checks belong
- `src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs` — runtime layer, no auth (correct — auth belongs at edge)

**Implication:**
- Auth registry interface → `Inlet.Runtime.Abstractions` (parallels `IProjectionBrookRegistry`)
- Auth registry implementation + scanning → `Inlet.Runtime` (parallels `ProjectionBrookRegistry`)
- Hub auth checks → `Inlet.Gateway` (where `InletHub` and `MapInletHub` live)
- This follows the existing layering exactly.

---

## CoV Summary

| Claim | Evidence | Confidence |
|-------|----------|------------|
| Hub has no auth | `InletHub.cs` has no `[Authorize]`, `MapInletHub` has no `.RequireAuthorization()` | High |
| MVC convention doesn't cover hubs | `IApplicationModelConvention` is MVC-only; wired via `MvcOptions` | High |
| `IProjectionBrookRegistry` pattern is appropriate precedent | Interface in Abstractions, impl in Runtime, populated by scan | High |
| `[GenerateAuthorization]` attribute is available at runtime in domain assemblies | Spring.Domain.csproj references Inlet.Generators.Abstractions | High |
| `Inlet.Runtime` doesn't reference `Inlet.Generators.Abstractions` | Verified in csproj — no reference | High |
| Adding that reference is safe | Inlet.Generators.Abstractions is netstandard2.0, zero deps | High |
| Client handles subscribe errors gracefully | `InletSignalRActionEffect` catches Exception, yields error action | High |
| `MapInletHub` can chain `.RequireAuthorization()` | Returns `HubEndpointConventionBuilder` — standard ASP.NET Core | High |
| Same `GeneratedApiAuthorizationOptions` can drive hub auth | Contains Mode, DefaultPolicy, DefaultRoles, etc. | High |
| No domain code changes needed | Attribute already on projection types; scanning at startup picks it up | High |
