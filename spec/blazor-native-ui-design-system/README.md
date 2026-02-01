# Blazor Native UI Design System Spec

## Status

- Status: Draft
- Size: Large (verified)
- Approval checkpoint: Yes (public API additions for new/expanded component library)

## Index

- [learned.md](learned.md)
- [rfc.md](rfc.md)
- [verification.md](verification.md)
- [implementation-plan.md](implementation-plan.md)
- [progress.md](progress.md)

## Summary

Design and implement an enterprise-grade, Blazor-native UI design system and component library aligned with the authoritative hologram design language brief. This spec is the durable working memory for the task.

## Requirements (initial)

- Follow the authoritative hologram design language brief verbatim as the highest priority.
- Use atomic design to structure docs and components.
- Provide enterprise data-app controls with mobile-first behavior.
- **NO JAVASCRIPT**: All components MUST be pure C# Blazor only. No JS interop, no JS libraries, no Chart.js. Use CSS animations and Blazor rendering for all visual effects.
- Documentation-first deliverables under docs/Docusaurus/docs/refraction.
- Treat existing Refraction UI component code as untrusted legacy and remove/clear it to start fresh.
- Incorporate the expanded hologram UI handoff notes (z-layers, palette, typography, motion, and acceptance checklist) as mandatory design inputs.
