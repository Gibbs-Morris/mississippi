# Review 13 — Synthesis

All 12 persona reviews have been consolidated. Each item is deduplicated, categorized (Must / Should / Could / Won't), and adjudicated with rationale and required plan edits.

---

## Must (blocking — plan updated before finalization)

### M1. HubException must NOT leak path/policy in production

- **Sources:** Review 10 §2 (Security), Review 09 §2 (DX), Review 01 §4 (Marketing)
- **Issue:** Error message "Access denied for projection '{path}'" reveals existence of paths and policies — OWASP information leak.
- **Decision:** Accept. Use generic message `"Subscription denied."` in all environments. Log details (path, policy, userId, connectionId) server-side at Warning level. Remove DX suggestion for environment-conditional detail — security wins over developer convenience here.
- **Required edits:** Update draft plan Phase 3 (SubscribeAsync) and Observability section. Add constant for generic denial message.

### M2. Extract auth check from SubscribeAsync into a private method

- **Sources:** Review 03 §2 (Principal Engineer), Review 04 §5 (Tech Architect)
- **Issue:** SubscribeAsync accumulates 5+ responsibilities with 6+ branches. Violates SRP.
- **Decision:** Accept. Extract to `private async Task AuthorizeSubscriptionAsync(string path)`. SubscribeAsync remains an orchestrator: validate → authorize → delegate.
- **Required edits:** Update draft plan Phase 3 work item 9 to show extracted method.

### M3. Handle `AuthorizationPolicy` construction from Roles/Schemes (not just named policies)

- **Sources:** Review 04 §4 (Tech Architect)
- **Issue:** `IAuthorizationService.AuthorizeAsync` needs an `AuthorizationPolicy` object, not just a string policy name. When Roles or AuthenticationSchemes are specified without a policy name, a policy must be built via `AuthorizationPolicyBuilder`.
- **Decision:** Accept. Use `AuthorizationPolicyBuilder` to construct policies from entry fields (Policy, Roles, AuthenticationSchemes). Mirror the approach in `GeneratedApiAuthorizationConvention.CreateDefaultAuthorizeFilter()`.
- **Required edits:** Update draft plan Phase 3 to describe policy construction. Add test cases for roles-only and schemes-only auth.

### M4. Update `[GenerateAuthorization]` and `MapInletHub` XML doc comments

- **Sources:** Review 09 §3 (DX), Review 09 §4 (DX), Review 11 §2 (Source Gen)
- **Issue:** Attribute doc says "generated HTTP APIs" — now also covers SignalR subscriptions. `MapInletHub` behavior changes silently when force mode is on.
- **Decision:** Accept. Update XML doc for both.
- **Required edits:** Add file changes for `GenerateAuthorizationAttribute.cs` doc update and `InletServerRegistrations.cs` doc update.

### M5. Add edge case tests

- **Sources:** Review 03 §4 (Principal Engineer)
- **Issue:** Missing edge cases: path in brook registry but not auth registry, empty/null policy string, multiple conflicting registrations, AllowAnonymousOptOut toggled scenarios.
- **Decision:** Accept. Add to test scenarios.
- **Required edits:** Expand test scenarios section in Phase 4.

---

## Should (high-value improvements — incorporated into plan)

### S1. Specify structured log field names in LoggerMessage entries

- **Sources:** Review 05 §1 (Platform), Review 10 §2 (Security)
- **Issue:** Log messages need explicit field names for log aggregation queries.
- **Decision:** Accept. Specify: `connectionId`, `path`, `entityId`, `userId`, `policyName`, `reason`.
- **Required edits:** Update Observability section with parameter names.

### S2. Document SignalR JWT token transport (query string gotcha)

- **Sources:** Review 02 §1 (Solution Engineering)
- **Issue:** Consumers using JWT bearer auth need `JwtBearerEvents.OnMessageReceived` to extract tokens from query strings for WebSocket connections.
- **Decision:** Accept as migration documentation task (not code). Add note to Rollout/Migration section.
- **Required edits:** Add migration note about JWT + SignalR token transport.

### S3. Acknowledge `Inlet.Runtime` → `Inlet.Generators.Abstractions` cross-reference

- **Sources:** Review 03 §1 (Principal Engineer), Review 01 §5 (Marketing)
- **Issue:** Crosses a conceptual boundary (generator attributes → runtime dependency). Package consumers get a transitive dependency.
- **Decision:** Accept the reference — it's pragmatic (zero-dep, netstandard2.0, just attributes). Add a doc comment to the csproj noting dual consumption. The alternative (moving attributes to `Inlet.Abstractions`) would be a broader refactor.
- **Required edits:** Add note to Phase 1 work item 1.

### S4. PR title uses `+semver: feature`

- **Sources:** Review 03 §5 (Principal Engineer)
- **Decision:** Accept. Pre-1.0, `+semver: feature` is appropriate.
- **Required edits:** Add PR title guidance to plan.

### S5. Document subscribe-time auth trade-off (claims may change post-subscribe)

- **Sources:** Review 06 §3 (Distributed Systems), Review 10 §4 (Security)
- **Issue:** User's claims could change after subscribe. Subscription continues until disconnect.
- **Decision:** Accept as known limitation. Document in plan. Same limitation as HTTP endpoints. Per-notification auth is prohibitively expensive.
- **Required edits:** Add to known limitations section.

### S6. Add an L2 test recommendation

- **Sources:** Review 02 §5 (Solution Engineering)
- **Issue:** No L2 integration test planned for end-to-end SignalR auth verification.
- **Decision:** Accept as follow-up — L2 test is valuable but should not block L0/L1 gate. Note in plan.
- **Required edits:** Add L2 test follow-up note to Testing Strategy.

---

## Could (nice-to-have — noted but not blocking)

### C1. Rename `ProjectionAuthorizationEntry` to `ProjectionAuthorizationMetadata`

- **Sources:** Review 01 §1 (Marketing)
- **Issue:** "Metadata" better describes declarative attribute data vs. executable policy.
- **Decision:** Accept rename. `ProjectionAuthorizationMetadata` is more precise and avoids confusion with `AuthorizationPolicy`.
- **Required edits:** Rename throughout plan.

### C2. Add auth denial message constant to `InletHubConstants`

- **Sources:** Review 01 §4 (Marketing)
- **Decision:** Accept. Add `SubscriptionDeniedMessage` constant.
- **Required edits:** Add to Phase 3 file changes.

### C3. Note `IHubFilter` as future extensibility path

- **Sources:** Review 02 §4 (Solution Engineering)
- **Decision:** Note as future option, not implemented now. Inline auth is simpler and sufficient.
- **Required edits:** Add brief note to evolution section.

### C4. Mention `ISubscriptionAuthorizationService` as evolution path for per-entity auth

- **Sources:** Review 04 §5 (Tech Architect), Review 10 §7 (Security)
- **Decision:** Note as future extensibility. Current SRP extraction (M2) naturally enables this.
- **Required edits:** Add evolution note.

### C5. Consider metrics counters in future iteration

- **Sources:** Review 05 §2 (Platform)
- **Decision:** Defer. Framework doesn't use `System.Diagnostics.Metrics` yet. Logging is sufficient for v1.
- **Required edits:** Note as follow-up.

### C6. Note auth service latency for cascading failure awareness

- **Sources:** Review 05 §3 (Platform)
- **Decision:** Note in failure modes table. Recommend timeout at policy provider level.
- **Required edits:** Add row to failure modes table.

---

## Won't (explicitly out of scope or already handled)

### W1. Per-transport granularity (HTTP-only vs. hub-only auth)

- **Sources:** Review 02 §3 (Solution Engineering)
- **Rationale:** Explicitly a non-goal. Pre-1.0 allows iteration if demand arises.

### W2. Rate limiting on SubscribeAsync

- **Sources:** Review 10 §3 (Security)
- **Rationale:** Cross-cutting concern applicable to all hub methods. Use ASP.NET Core rate limiting middleware. Out of scope.

### W3. Token refresh / forced disconnect on revocation

- **Sources:** Review 10 §4 (Security)
- **Rationale:** Known SignalR limitation. Applies to all SignalR applications. Document as known trade-off; not specific to this feature.

### W4. Diagnostics HTTP endpoint for registry inspection

- **Sources:** Review 05 §5 (Platform)
- **Rationale:** `GetAllPaths()` method enables building one later. Not blocking for initial implementation.

### W5. Auth check on UnsubscribeAsync

- **Sources:** Review 04 §6 (Tech Architect)
- **Rationale:** Subscription grain is keyed by connectionId — clients can only unsubscribe their own subscriptions. No auth needed.

### W6. Multi-silo registry consistency validation

- **Sources:** Review 06 §4 (Distributed Systems)
- **Rationale:** Same deployment requirement as `IProjectionBrookRegistry`. Deterministic given same assemblies.

### W7. Generator-emitted auth registry population (alternative to reflection)

- **Sources:** Review 11 §4 (Source Gen)
- **Rationale:** Future optimization. Runtime reflection is the established pattern and consistent with `IProjectionBrookRegistry`.

### W8. Cross-tenant per-entity isolation

- **Sources:** Review 10 §7 (Security)
- **Rationale:** Explicitly non-goal. Architecture supports future extension.

### W9. `[GenerateAllowAnonymous]` + opt-out=false behavior for subscriptions

- **Sources:** Review 07 (ES/CQRS) — verification only, no issues found.
- **Rationale:** Already covered by the plan. Consistent with MVC behavior.

---

## Summary of Plan Changes Required

| # | Change | Category | Sections Affected |
|---|--------|----------|-------------------|
| M1 | Generic denial message, no path/policy in HubException | Must | Phase 3, Observability, Test scenarios |
| M2 | Extract `AuthorizeSubscriptionAsync` private method | Must | Phase 3 work item 9 |
| M3 | `AuthorizationPolicyBuilder` for roles/schemes | Must | Phase 3, Test scenarios |
| M4 | Update XML docs for attribute + `MapInletHub` | Must | Phase 1 + Phase 2 file changes |
| M5 | Add edge case tests | Must | Phase 4 test scenarios |
| S1 | Specify log field names | Should | Observability |
| S2 | JWT + SignalR token transport migration note | Should | Rollout/Migration |
| S3 | Document cross-reference decision | Should | Phase 1 work item 1 |
| S4 | PR title `+semver: feature` | Should | Plan header |
| S5 | Subscribe-time auth known limitation | Should | Known Limitations (new section) |
| S6 | L2 test follow-up recommendation | Should | Testing Strategy |
| C1 | Rename Entry → Metadata | Could | Throughout |
| C2 | `InletHubConstants.SubscriptionDeniedMessage` | Could | Phase 3 file changes |
| C3 | Note `IHubFilter` extensibility | Could | Evolution |
| C4 | Note `ISubscriptionAuthorizationService` future | Could | Evolution |
| C5 | Metrics counters follow-up | Could | Observability |
| C6 | Auth service latency failure mode | Could | Failure Modes table |
