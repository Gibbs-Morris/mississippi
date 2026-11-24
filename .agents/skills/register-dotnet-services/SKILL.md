---
name: register-dotnet-services
description: Implement or assess .NET dependency-injection registration for a feature, including registration hierarchy, options overloads and validation, deferred initialization, and keyed external-client forwarding. Use when adding or changing service wiring or its configuration contract. Not for ordinary feature logic, logging conversion, deployment provisioning, or general architecture review.
---

# Register .NET services

Make a feature's service graph composable and verify its configuration and client
identity. Use the consuming project's policies and current sources for its public
surface, lifetimes, defaults, key ownership, and validation requirements.

## Establish the registration contract

Identify the feature boundary, callers, supported host, and whether the request is
assessment-only or authorizes edits. Read applicable instructions and local
bindings, current parent/child registrations, options and validators, consuming
constructors, and host configuration. Inspect builder attachment and generated
registration paths when present; do not infer an overload or lifetime from a name
or copied example. Report unavailable evidence and ask only for missing decisions
that change the graph or authority.

For assessment-only, inspect and explain the supplied graph without editing,
running initialization, provisioning resources, or publishing. Registration work
does not authorize changes to unrelated domain behavior, packages, or policy.

## Compose the graph and options

- Put public entrypoints at the project's approved feature boundaries. Compose
  child registrations at their intended visibility instead of copying service
  lists into parents. Follow local naming, XML documentation, registration order,
  and builder lifecycle requirements; retain generated paths when applicable.
- Preserve intentional lifetimes, overrides, and aliases. Decide from the contract
  whether two interfaces share an instance; separate registrations do not establish
  shared identity. Avoid building a temporary provider during registration.
- Use options in consumers. Offer the configuration forms required by the project,
  such as an action, configuration section, or explicit parameters, through a
  common registration path. Verify defaults, binding precedence, required values,
  and validation timing; validate options on startup when required. Do not assume
  a nested builder callback takes an options type.
- Keep service registration synchronous. Store client creation in factories and
  defer asynchronous resource setup to a hosted service or the host's lifecycle
  participant. Preserve cancellation and failure handling; do not start network
  work or block on a task while building the graph.

## Forward keyed clients

Read each consuming constructor and module-owned defaults to identify the exact
service type and key. Keep library keys separate from deployment names; preserve
existing key ownership and naming rules instead of inventing a shared key hub.
Document the keyed services callers provide in the registration contract.

Register forwarding factories from each host-owned keyed client to the matching
library key. Resolve the intended keyed source inside those factories, preserving
its lifetime and instance identity. Add an unkeyed alias only when the host needs
one, and forward it explicitly to the intended keyed instance. Do not replace all
keys with one unkeyed client or construct another client merely to create an alias.
Use keyed constructor injection where supported; keep runtime resolution in the
approved factory seam rather than injecting a provider into ordinary consumers.

## Verify the observable contract

Use the project's checks and meaningful tests for the changed graph. Resolve
representative consumers and assert exact client identity, distinct instances for
independent keys, intended lifetimes and aliases, configured/default options, and
invalid-option or missing-key failures at the required boundary. Confirm that
registration performs no asynchronous setup and that authorized startup performs
it with cancellation and failure propagation. Check hierarchy and overload routing
against the actual implementation, not just matching method names or prose.

Report changed boundaries, configuration forms, required keys, deferred work,
checks actually executed, and remaining evidence gaps. Compilation or a successful
provider build alone cannot prove startup behavior or external-resource readiness.
