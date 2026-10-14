---
applyTo: '**'
---

# Metadata and Host Composition

Governing thought: Within the activation scope below, put host composition in registrations, runtime policy in options, and immutable type facts or one static capability contract in attributes.

> Drift check: Read the operative declaration, generator or runtime reader, tests, and host call before changing a contract; the [Proposed ADR](../../docs/Docusaurus/docs/adr/0002-metadata-and-host-composition.md) records the rationale.

These proposed rules apply to work whose approved issue or execution contract
explicitly adopts them, including authorized work in attribute programme #1127.
Global discovery of this guide does not establish repository-wide adoption.
Repository-wide activation requires actual ADR acceptance through the existing
lifecycle. Programme approval, publication, or merge alone does not establish
that acceptance. The rules below are limited to this activation scope.

API migration availability follows the source revision being built.
The audited coupling below describes revisions before each implementing change;
this guide alone does not change generator consumers or supply a replacement API.
The projection runtime/HTTP split is implemented in
[PR #1168](https://github.com/Gibbs-Morris/mississippi/pull/1168) for
[programme #1127](https://github.com/Gibbs-Morris/mississippi/issues/1127).
Other migrations remain planned where their implementing changes are absent.
Check declarations and operative consumers in the current checkout before use.

## Rules (RFC 2119)

### Registrations

- Service inclusion choices offered by a feature MUST be made explicitly through registration. Why: The composition root owns which services participate.
- Provider implementation choices offered by a feature MUST be made explicitly through registration. Why: The host owns its provider selection.
- Service lifetime choices offered by a feature MUST be made explicitly through registration. Why: The host owns service lifecycles.
- Host capability choices offered by a feature MUST be made explicitly through registration. Why: The host owns which capabilities it enables.
- Assembly discovery choices offered by a feature MUST be made explicitly through registration. Why: The host owns its discovery boundary.
- Registrations MUST be synchronous. Why: Container composition is not an asynchronous initialization boundary.
- Registration code MUST NOT create temporary service providers, including through BuildServiceProvider. Why: A second container can create duplicate singleton graphs outside the host's ownership.
- Network work and asynchronous initialization MUST be deferred to factories, hosted services, or supported lifecycle participants. Why: Registration cannot prove resource readiness.
- Feature registration MUST compose with earlier valid registrations. Why: Adding a second module should preserve the first module's graph.
- An identical feature registration request MUST be idempotent. Why: Repeated inclusion should not duplicate descriptors, adapters, or registry entries.
- Deliberate additive registrations MUST document their request identity and multiplicity. Why: Distinct handlers or effects are not duplicate requests.
- Generated registration helpers MUST require an explicit composition-root call. Why: An attribute supplies generation input rather than executing container mutations.
- Authors MUST retain the shipped RuntimeBuilder, ClientBuilder, and hierarchical Add entry points when extending composition. Why: This programme does not replace the established host facades.
- Authors MUST preserve each root builder's terminal attachment rules. Why: Idempotent feature inclusion does not permit repeated UseMississippi attachment.
- Features MUST offer options-configuring `Action<TOptions>` and IConfiguration forms at the appropriate existing boundary when they have bindable configurable options. Why: Those forms configure a real options contract rather than manufacture API ceremony.
- Non-bindable settings such as delegates MUST use an explicit callback or factory contract. Why: Configuration binding cannot construct executable dependencies.
- A required type identity MAY remain an explicit argument or generic type parameter. Why: A single required identity does not need an options object.
- Marker-only registrations MUST NOT acquire empty options or three overloads solely to match a blanket pattern. Why: The public surface should reflect actual choices.

### Options

- Host, deployment, and runtime tuning or defaults MUST use feature-owned {Feature}Options through the existing DI options pipeline. Why: Timeouts, retention defaults, transport settings, and access defaults belong to the host.
- Configurable options MUST document their defaults and configuration/callback ordering. Why: Consumers need the effective configuration contract.
- Options validation MUST run before the host serves work, using ValidateOnStart or an explicit validated startup lifecycle. Why: IValidateOptions supplies validation logic but does not by itself trigger eager validation.
- Ordinary service constructors MUST consume options through `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` rather than raw configuration primitives. Why: One pipeline owns binding and validation.
- Registration MUST NOT create hidden Configure branches. Why: Consumers should receive configuration through the same validated options pipeline.
- Registration MUST NOT create new-options copies. Why: Consumers should receive the same validated effective configuration.
- An options contract MUST state whether it supports named instances, reload, or a fixed startup snapshot. Why: Binding alone does not establish those capabilities.

### Attributes

- Attributes MUST describe immutable per-type identity, a workflow relationship, one generated capability, or one static contract. Why: The declaration travels with the type rather than the deployment.
- Every public attribute member MUST have a named operative consumer. Why: A getter or parsed value alone does not implement a public promise.
- Every public attribute member MUST document its default. Why: Authors need to know the meaning of an omitted setting.
- Every public attribute member MUST document its precedence. Why: Overlapping declarations and host options need a defined effective value.
- Every public attribute member MUST define its invalid-value behavior. Why: Unsupported inputs need an explicit failure contract.
- Every public attribute member MUST have tests for its operative behavior. Why: Acceptance by a parser does not prove the public promise is implemented.
- Every public attribute member MUST have reference documentation. Why: Consumers need a discoverable contract for each setting.
- Attributes MUST NOT contain environment secrets, deployment endpoints, provider selection, retry budgets, or runtime switches. Why: Those values vary by host or represent executable policy.
- One attribute MAY contain several cohesive settings such as Policy, Roles, and AuthenticationSchemes. Why: Separating each field would not separate responsibilities.
- One attribute MUST NOT select both runtime registration and transport exposure. Why: Runtime-only composition and exposed adapters are independent capabilities.
- HTTP routing and client feature identity MUST have separate metadata contracts. Why: A route change should not rename a client state slice.
- A static transport contract MAY be consumed by both its server and generated client. Why: Both sides need the same wire contract.
- A tunable type default MAY use an attribute only with a named host-options override and documented precedence. Why: Snapshot retention demonstrates a deliberate exception with host ownership.
- Generators MUST validate their raw metadata independently of attribute constructors. Why: Roslyn analysis does not execute those constructors.
- Invalid or unsupported metadata MUST fail with a diagnostic naming the declaration, member, or configuration to correct. Why: Accepted but ignored settings make a contract misleading.
- Descriptive metadata MUST NOT opt into a capability or substitute for runtime enforcement. Why: A tool hint, policy name, or description cannot register a tool or authorize a request.

### Precedence and Identity

- Each overlapping declaration/options contract MUST specify its own precedence. Why: There is no universal attributes-always-win rule.
- Tests MUST verify the documented precedence at every operative boundary. Why: An order written in a guide does not establish effective consumer behavior.
- Runtime safety policy MUST be able to constrain static declarations under its documented contract. Why: A host may restrict anonymous access or apply required defaults.
- Ordinary source/API refactors MUST preserve persisted brook, event, snapshot, and Orleans identities, including constructed generic identities. Why: Pre-1.0 API freedom does not make stored data disposable.
- Authors MUST reconcile applicable instruction conflicts. Why: Future work needs consistent responsibility and precedence requirements.
- Authors MUST link this canonical policy instead of copying competing decision rules. Why: One source keeps the responsibility model discoverable.

## Scope and Audience

Framework authors and agents working within the activation scope above and choosing metadata, configuration, generation, or
host composition. Existing implementations are evidence of current behavior;
planned changes remain tracked by their bounded issues.

## At-a-Glance Quick-Start

A parent builder callback can still take a nested builder; its options configuration follows the existing feature boundary. This rule does not replace builder callbacks with options callbacks.

Classify the fact first. Find its reader. Fill the consumer checklist. Verify
the host call and the effective contract before adding a member or overload.

## Decision Tree

Follow the first matching branch; split a proposed setting that answers several
questions before classifying it.

1. Request data or executable business behavior? Use a command, parameter,
   domain implementation, or policy service.
2. Which services, implementation, lifetime, host capability, or assemblies
   participate? Use explicit registration or a factory registered there.
3. Does the value vary by host, environment, or runtime policy? Use options.
   A type default with an explicit host override follows the exception below.
4. Is it an immutable type identity, workflow relationship, one generation
   choice, or static wire/access contract? Use an attribute with an operative
   consumer.
5. Is there no operative consumer or supported meaning? Reject or defer the
   setting; do not expose an accepted placeholder.

### Seven Classification Cases

| Fact being chosen | Branch and owner | Reason |
| --- | --- | --- |
| Host retention modulus override | 3: SnapshotRetentionOptions.StateTypeOverrides | Deployment policy; SnapshotRetention remains a type default under explicit precedence. |
| BrookName | 4: immutable identity attribute | Identifies the logical brook; intentional sharing is separate from address uniqueness. |
| Saga input type | 4: workflow relationship | `SagaInput<TInput>` is planned in #1131; the current input still resides on GenerateSagaEndpoints. |
| Connection factory | 2: registration factory | Selects and creates a host dependency; configuration values used by it remain options. |
| HTTP route segment | 4: static transport attribute | Server and generated client share a wire route; a deployment base URL follows branch 3. |
| Client feature key | 4: client identity attribute | ClientFeatureKey is planned in #1133; it is independent of HTTP routing. |
| Authorization default | 3: generated API authorization options | Host policy; per-type explicit access metadata follows branch 4. |

SnapshotRetention is an existing type-default exception, not a license to put
arbitrary runtime settings on attributes. Its operative override algorithm is
[SnapshotRetentionOptions.GetRetainModulus](../../src/Tributary.Abstractions/SnapshotRetentionOptions.cs).

## Good and Bad Choices

These are design examples, not runnable application snippets.

| Good choice | Bad choice | Consumer or correction |
| --- | --- | --- |
| Keep BrookName unchanged when renaming a record. | Derive a new persisted brook from its HTTP route. | Brook identity readers; preserve the stored value. |
| Configure a host retention override in SnapshotRetentionOptions. | Put a deployment retry budget on a domain attribute. | GetRetainModulus consumes the override; retries need options or policy code. |
| Call a generated Add helper explicitly from host composition. | Claim that applying a generation marker registers services. | The generated helper's caller owns inclusion. |
| Keep Policy/Roles/AuthenticationSchemes together. | Use tool ReadOnly metadata as proof of authorization. | Access is enforced at the real boundary; hints only describe behavior. |
| Add configuration overloads for a real options family. | Create empty options for a marker or a sole required type identity. | Existing registration hierarchy and builders remain the entry points. |
| Select runtime, HTTP, and MCP capabilities independently. | Use an endpoint marker to imply all three choices. | The migration status below identifies implemented and planned consumers. |

## Metadata Consumer Checklist

Record this worksheet before introducing or changing a public member:

| Check | Evidence to record |
| --- | --- |
| Owner and responsibility | Registration, options, or one attribute purpose; reason an existing contract cannot serve it. |
| Declaration and default | Exact member/type, null/empty meaning, literal or derived default, required inputs. |
| Operative consumer | Named method/generator, emitted member, runtime reader, and composition-root call; parsing alone is insufficient. |
| Precedence | Order of defaults, binding, callbacks, type metadata, operation metadata, and host safety constraints where applicable. |
| Invalid input | Supported values/shapes and source/runtime diagnostic naming the declaration or configuration. |
| Parity and independence | Source/reference discovery and server/client/runtime/HTTP/MCP consumers; unrelated capabilities remain absent. |
| Identity and migration | Stored name/alias/version impact, old-to-new source changes, and all affected consumers/docs. |
| Tests and reference | Non-default consumer behavior, omission/conflict/invalid cases, effective precedence, executable generated-consumer proof, and public reference page. |

For a documentation-only rule change, name the review checks and mark behavioral
proof as child work. Do not report this worksheet or attribute getter tests as
executed generator, host startup, or authorization evidence.

## Precedence Matrix

| Contract | Effective order or boundary | Status |
| --- | --- | --- |
| Snapshot retention modulus | Stable snapshot storage-name override, CLR full-name override, SnapshotRetention.Modulus, DefaultRetainModulus | Existing GetRetainModulus behavior; prefer stable keys. |
| Host options | Document the feature's actual defaults, configuration binding, callbacks, post-configuration, and validation order | No global callback-wins rule or implied reload. |
| Stored identity | Explicit persisted identity; transport routes and client keys never replace it | Preserve existing values and derived generic names during ordinary refactors. |
| HTTP/client wire route | One static route contract interpreted consistently by both consumers | Configured aggregate-route parity remains owned by #861 / PR #862. |
| Access metadata and defaults | Resolve group/operation metadata, anonymous opt-out and validated host policy for each operation at the enforcing boundary | Planned #1150, #1151 and #1154; these rules do not claim completed HTTP/SignalR/MCP parity. |
| Capability selection | Independent runtime/HTTP/MCP opt-in plus the corresponding explicit host registration/mapping | Projection runtime/HTTP separation is implemented in #1130; other marker migrations remain planned. Metadata presence does not attach a host. |

Missing named policies or schemes are configuration errors to diagnose at the
owning boundary; declaring their names does not register authentication or
authorization services.

## Consumer Migration Status

| Audited coupling | Contract and availability |
| --- | --- |
| GenerateAggregateEndpoints also selects runtime registration | GenerateAggregateRegistration in #1129; endpoint metadata retains HTTP ownership. |
| GenerateProjectionEndpoints also selects runtime registration before A03 | Implemented in #1130 / [PR #1168](https://github.com/Gibbs-Morris/mississippi/pull/1168): GenerateProjectionRegistration selects individual and composite silo helpers; GenerateProjectionEndpoints retains HTTP controller, response DTO, mappings, matching client DTO, and shared projection-feature inclusion in generated domain-client registration. Neither marker activates subscriptions. |
| Saga input stored on HTTP endpoint metadata | `SagaInput<TInput>` in #1131; GenerateSagaRegistration in #1132. |
| Endpoint metadata stores client FeatureKey | ClientFeatureKey in #1133. |
| GenerateCommand drives HTTP and MCP selection | POST-only GenerateCommandEndpoint in #1134/#1136; independent GenerateMcpTool in #1135. |
| GenerateAuthorization / GenerateAllowAnonymous live with generator markers | Neutral InletAuthorize / InletAllowAnonymous in #1144 with separate enforcing-boundary work. |
| Unread DTO controls and placeholder generation promises | Property contract, requiredness, GeneratedDtoIgnore and GeneratedDtoJsonName in #1139-#1142; removals in #1137/#1138. |

Projection consumers on revisions containing A03 can use
`GenerateProjectionRegistration`; older revisions retain the audited coupling.
Other replacements remain planned unless their implementing change is present
in the source revision being built. Stored identity values, aliases, and versions
stay outside these source renames. Existing alias-policy work belongs to #370, and explicit sample storage
versions belong to #718.

## Registration Procedure Ownership

[Service registration](service-registration.instructions.md) retains the current
registration procedure. [#799](https://github.com/Gibbs-Morris/mississippi/issues/799)
and [draft PR #804](https://github.com/Gibbs-Morris/mississippi/pull/804) own its
proposed skill extraction. This policy adds responsibility rules, not another
procedural skill. If that extraction lands, use its then-current linked skill
and bindings for implementation and recheck their agreement with this policy.

## References

- [Service registration hierarchy](service-registration.instructions.md)
- [Domain metadata and identity](domain-modeling.instructions.md)
- [Options and constructor conventions](csharp.instructions.md)
- [Storage identity](storage-type-naming.instructions.md)
- [Backwards compatibility and persisted data](backwards-compatibility.instructions.md)
- [Issue implementation programme](https://github.com/Gibbs-Morris/mississippi/issues/1127)
