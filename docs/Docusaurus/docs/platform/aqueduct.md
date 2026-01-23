---
sidebar_position: 5
title: Aqueduct
description: High-level overview of the Orleans-backed SignalR backplane.
---

# Aqueduct

Aqueduct is the Orleans-backed SignalR backplane that lets Orleans grains and ASP.NET hosts route real-time messages across servers without external backplanes.

## Purpose

- Replace the default SignalR lifetime manager with an Orleans-backed backplane.
- Track connections, groups, and servers via grains.
- Deliver cross-server messages and broadcasts reliably.

## Where it fits

Aqueduct powers SignalR delivery for server-side hubs such as Inlet. It bridges ASP.NET SignalR infrastructure to Orleans grains.

## Source code reference

- [AqueductHubLifetimeManager](../../../../src/Aqueduct/AqueductHubLifetimeManager.cs)
- [ISignalRClientGrain](../../../../src/Aqueduct.Abstractions/Grains/ISignalRClientGrain.cs)
