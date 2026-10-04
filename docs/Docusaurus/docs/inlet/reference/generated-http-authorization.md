---
id: generated-http-authorization
title: Generated HTTP Authorization Options
description: Reference generated MVC authorization defaults, controller selection, and explicit metadata precedence.
sidebar_position: 4
sidebar_label: Generated HTTP Authorization Options
---

# Generated HTTP Authorization Options

## Overview

`InletServerOptions.GeneratedApiAuthorization` configures Inlet's generated MVC authorization convention. Its defaults leave generated application models unchanged while preserving explicitly generated authorization metadata.

## Applies To

- `Mississippi.Inlet.Gateway.GeneratedApiAuthorizationOptions`
- `GeneratedApiAuthorizationMode`
- Generated HTTP controllers processed by `GeneratedApiAuthorizationConvention`

## Defaults

The [options](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/GeneratedApiAuthorizationOptions.cs) expose five settings:

- `Mode`: `Disabled` by default. The convention changes application models only for `RequireAuthorizationForAllGeneratedEndpoints`.
- `AllowAnonymousOptOut`: `true`; generated anonymous metadata is preserved in force mode.
- `DefaultPolicy`: null; optional policy name assigned to an added default authorization filter.
- `DefaultRoles`: null; optional roles string assigned to that filter.
- `DefaultAuthenticationSchemes`: null; optional authentication-schemes string assigned to that filter.

The convention ignores whitespace-only default strings and copies other values without trimming or parsing them itself. Assigning these options does not register the named policy or authentication schemes; the host owns that setup.

When force mode adds a filter and all three default strings are null or blank, its empty `AuthorizeAttribute` uses the host's configured ASP.NET Core default authorization policy. The platform default requires an authenticated user; null option values do not remove authorization.

## Which Controllers Are Selected

[`AddInletServer`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/InletServerRegistrations.cs) registers an MVC options setup that installs the convention using resolved server options.

The [convention](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/GeneratedApiAuthorizationConvention.cs) selects controllers declaring `GeneratedCodeAttribute` with exactly one of these ordinal tool names: `AggregateControllerGenerator`, `ProjectionEndpointsGenerator`, or `SagaControllerGenerator`.

It does not infer generated ownership from a controller's namespace or class name, and it does not apply this default filter to arbitrary handwritten controllers.

## Force Mode And Explicit Metadata

In force mode, the convention adds a default controller `AuthorizeFilter` only when the controller has no preserved anonymous metadata and neither the controller nor any action has `IAuthorizeData` attributes or an `AuthorizeFilter`. Other custom `IAuthorizationFilter` or `IAsyncAuthorizationFilter` implementations do not count in this detection.

An explicit authorization annotation on even one action suppresses this controller fallback. Other unannotated actions in that mixed controller do not receive individual default filters from the convention. Review each action's actual authorization metadata when using mixed controllers.

With `AllowAnonymousOptOut = true`, anonymous controller/action metadata remains. With it set to false, the convention removes anonymous attributes and anonymous filters from selected controllers and actions. Removing that metadata does not change the explicit-authorization test that controls fallback creation.

In MVC, preserved controller-level `[AllowAnonymous]` bypasses `[Authorize]` on that controller and its actions. An action annotation therefore does not protect an endpoint under an anonymous controller unless the host supplies a separate guard. See Microsoft's [MVC authorization precedence](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/simple#authorize-attribute).

`Disabled` skips these convention changes; it does not remove existing authorization or force every endpoint to be anonymous.

## Separate Authorization Boundaries

These settings are also consumed by Inlet's hub mapping and subscription logic, but those paths evaluate their own conditions. This page describes the MVC convention; HTTP defaults alone do not establish identical SignalR subscription or MCP authorization behavior.

The [convention tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Gateway.L0Tests/GeneratedApiAuthorizationConventionTests.cs) cover explicit-action fallback suppression and preserving/removing anonymous metadata. They inspect application models rather than exercising a complete deployed authentication pipeline.

## Summary

Generated HTTP authorization defaults are conditional on controller markers and existing metadata. Keep host policy setup and per-action coverage explicit, especially when a controller mixes annotated and unannotated actions.

## Next Steps

- Read [Generated Application Contracts](./generated-contracts.md) for generation metadata and host composition.
- Read [Spring Host Applications](../../samples/spring-sample/concepts/host-applications.md) for the sample's configured policy.
