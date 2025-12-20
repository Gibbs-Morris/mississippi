---
applyTo: '**'
---

# Grain Doc Maintenance

Governing thought: Keep `docs/grain-dependencies.md` accurate whenever new grains or controllers are added so the dependency diagram remains trustworthy.

> Drift check: Open `docs/grain-dependencies.md` before changing grain/controller topology or adding new Orleans/ASP.NET entry points.

## Rules (RFC 2119)

- Changes that add or remove Orleans grains or grain interfaces **MUST** update `docs/grain-dependencies.md` to reflect the new nodes and edges. Why: The diagram is the authoritative quick view of grain relationships.
- Changes that add or remove ASP.NET controllers exposing projections **MUST** update the API layer in `docs/grain-dependencies.md` accordingly. Why: Keeps API-to-grain flow visible for reviewers.
- Stateless workers **MUST** be annotated in the diagram as such (dashed teal class) when added. Why: Highlights Orleans activation model impacts.
- When dependency directions change (new calls/streams), the corresponding arrows in `docs/grain-dependencies.md` **MUST** be adjusted. Why: Prevents stale or misleading architecture views.

## Scope and Audience

Anyone adding/modifying Orleans grains, grain factories, or projection controllers that participate in `docs/grain-dependencies.md`.

## At-a-Glance Quick-Start

- Open `docs/grain-dependencies.md`; add/remove nodes and arrows for new/changed grains or controllers.
- Mark new `[StatelessWorker]` grains with the `stateless` class in Mermaid.
- Keep notes aligned with any new behaviors (e.g., new stream links).

## Core Principles

- Diagram accuracy prevents architectural drift.
- Stateless vs stateful grain cues aid capacity and lifecycle reviews.

## References

- Grain topology diagram: `docs/grain-dependencies.md`
- Instruction authoring template: `.github/instructions/authoring.instructions.md`
