---
id: spring-auth-proof-mode
title: Spring Auth-Proof Mode
sidebar_label: Auth-Proof Mode
sidebar_position: 7
description: Dev-only, opt-in generated endpoint authorization proof flow for the Spring sample, including 200/401/403 verification.
---

# Spring Auth-Proof Mode

## Overview

Focus: Public API / Developer Experience.

Spring includes a dedicated auth-proof flow for generated endpoints so you can verify authentication and authorization behavior in local development with explicit `200`, `401`, and `403` outcomes.

> Warning: Auth-proof mode is a local development aid that uses header-based identity emulation. It is not a production authentication design.

## Enable Auth-Proof Mode

Set `Spring__AuthProofMode=true` before launching `Spring.AppHost`.

When enabled, AppHost sets `SpringAuth__Enabled=true` for `spring-server`. When not set (or set to `false`), Spring keeps auth disabled by default and existing sample behavior remains unchanged.

## Identity Override Headers

When local dev auth is enabled, `Spring.Server` reads these headers:

| Header | Purpose | Example |
| --- | --- | --- |
| `X-Spring-Anonymous` | Forces no authenticated principal for the request | `true` |
| `X-Spring-User` | Overrides user identifier | `auth-proof-user` |
| `X-Spring-Roles` | Comma-separated roles (`none` means empty role set) | `auth-proof-operator` |
| `X-Spring-Claims` | Claim pairs (`type=value`) separated by `,` or `;` | `spring.permission=auth-proof` |

Header precedence is `request header` over configured defaults.

## Generated Endpoint Matrix

Spring auth-proof mode demonstrates generated authorization behavior across all three endpoint categories:

| Category | Endpoint | Authorization metadata | Identity example | Expected status |
| --- | --- | --- | --- | --- |
| Command | `POST /api/aggregates/auth-proof/{id}/authenticated` | `[GenerateAuthorization]` | `X-Spring-Anonymous: true` | `401` |
| Command | `POST /api/aggregates/auth-proof/{id}/role` | `[GenerateAuthorization(Roles = "auth-proof-operator")]` | `X-Spring-Roles: none` | `403` |
| Command | `POST /api/aggregates/auth-proof/{id}/role` | `[GenerateAuthorization(Roles = "auth-proof-operator")]` | `X-Spring-Roles: auth-proof-operator` | `200` |
| Saga | `GET /api/sagas/auth-proof/{sagaId}/status` | `[GenerateAuthorization(Roles = "auth-proof-operator")]` | `X-Spring-Anonymous: true` | `401` |
| Saga | `GET /api/sagas/auth-proof/{sagaId}/status` | `[GenerateAuthorization(Roles = "auth-proof-operator")]` | `X-Spring-Roles: none` | `403` |
| Saga | `GET /api/sagas/auth-proof/{sagaId}/status` | `[GenerateAuthorization(Roles = "auth-proof-operator")]` | `X-Spring-Roles: auth-proof-operator` | `200` |
| Projection | `GET /api/projections/auth-proof/{id}` | `[GenerateAuthorization(Policy = "spring.auth-proof.claim")]` | `X-Spring-Anonymous: true` | `401` |
| Projection | `GET /api/projections/auth-proof/{id}` | `[GenerateAuthorization(Policy = "spring.auth-proof.claim")]` | no matching claim | `403` |
| Projection | `GET /api/projections/auth-proof/{id}` | `[GenerateAuthorization(Policy = "spring.auth-proof.claim")]` | `X-Spring-Claims: spring.permission=auth-proof` | `200` |

## Verify with L2 Tests

Run the auth-proof matrix tests:

```powershell
dotnet test ./samples/Spring/Spring.L2Tests/Spring.L2Tests.csproj -c Release --filter "FullyQualifiedName~AuthProofAuthorizationIntegrationTests"
```

These tests validate all required scenarios across command, saga, and projection endpoints:

- anonymous identity -> `401`
- authenticated identity missing required role/claim -> `403`
- authenticated identity with required role/claim -> `200`

## Troubleshooting

| Symptom | Likely cause | Check |
| --- | --- | --- |
| `401 Unauthorized` | No authenticated principal was established | Ensure `X-Spring-Anonymous` is absent or `false` and `SpringAuth` is enabled |
| `403 Forbidden` | Principal authenticated but missing required role/claim | Verify `X-Spring-Roles` and `X-Spring-Claims` match endpoint requirements |
| Auth behavior appears unchanged | AppHost auth-proof mode is not enabled | Confirm `Spring__AuthProofMode=true` before startup |

## See Also

- [Host Applications](./host-applications.md) — Spring host wiring and generated registration overview
- [Overview](./index.md) — Spring sample architecture and learning path
- [MCP in VS Code](./mcp-server-vscode-testing.md) — Testing generated MCP tools locally
