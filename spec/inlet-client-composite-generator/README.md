# Inlet Client Composite Generator

## Status

**Draft** → collecting verification answers

## Task Size

**Medium** - Single generator addition, affects Inlet.Client.Generators and sample wiring

## Approval Checkpoint

**No** - Not a breaking change; additive only (new generator, samples opt in)

## Files

- [learned.md](./learned.md) - Repository facts
- [rfc.md](./rfc.md) - Design proposal
- [verification.md](./verification.md) - Claims and evidence
- [implementation-plan.md](./implementation-plan.md) - Step-by-step plan
- [progress.md](./progress.md) - Timestamped log

## Summary

Create a new source generator that emits a single `Add{AppName}Inlet()` extension method consolidating:

- All aggregate feature registrations (`AddBankAccountAggregateFeature()`, etc.)
- Inlet client setup (`AddInletClient()`)
- Blazor SignalR setup with auto-scanned projection DTOs
- Built-in Reservoir features

This reduces Spring.Client/Program.cs from 5+ registration calls to 1.
