# Spring Registration Cleanup

## Status

**Draft** – Initial discovery and plan

## Size Classification

**Medium** – Changes to multiple files in Spring.Silo and Spring.Server, adds new registration classes, no public API changes outside samples

## Approval Checkpoint

**No** – This is sample/demo code refactoring with no public API, data, or breaking changes

## Task Summary

Refactor Spring.Silo and Spring.Server `Program.cs` files to move registration logic into cohesive extension method classes that group related concerns together, making it easier for new developers to understand what each registration does.

## Files

- [learned.md](learned.md) – Verified repository facts
- [rfc.md](rfc.md) – RFC-style design document
- [verification.md](verification.md) – Claims and verification
- [implementation-plan.md](implementation-plan.md) – Detailed step-by-step plan
- [progress.md](progress.md) – Timestamped log of key decisions
