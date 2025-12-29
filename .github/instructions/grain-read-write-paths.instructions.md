---
applyTo: '**/docs/grain-read-write-paths.md'
---

# Grain Read/Write Paths Documentation Maintenance

Governing thought: Keep `docs/grain-read-write-paths.md` accurate whenever grain reentrancy attributes change so performance bottlenecks remain visible.

> Drift check: Open `docs/grain-read-write-paths.md` and the relevant grain interfaces before adding/removing `[ReadOnly]`, `[Reentrant]`, or `[StatelessWorker]` attributes.

## Rules (RFC 2119)

- Changes that add or remove `[ReadOnly]` from grain interface methods **MUST** update `docs/grain-read-write-paths.md` to reflect the new reentrancy status. Why: The diagram is the authoritative quick view of performance bottlenecks.
- Changes that add or remove `[StatelessWorker]` from grain implementations **MUST** update both `docs/grain-dependencies.md` and `docs/grain-read-write-paths.md`. Why: StatelessWorker affects both topology and concurrency.
- When a grain method's implementation changes to mutate or stop mutating local state, the bottleneck analysis table **MUST** be updated. Why: ReadOnly safety depends on implementation, not just intent.
- New grains with read methods **SHOULD** be analyzed for `[ReadOnly]` eligibility and documented in the paths diagram. Why: Keeps performance visibility complete.

## Scope and Audience

Anyone adding/modifying Orleans grains, especially changes to reentrancy attributes or caching behavior.

## At-a-Glance Quick-Start

- Adding `[ReadOnly]`: Update diagram to show green/✅ path; remove from bottleneck table if applicable.
- Removing `[ReadOnly]`: Update diagram to show orange/⚠️ path; add to bottleneck table with reason.
- Adding cache mutation: Document in "Why Not ReadOnly" column.
- Review mitigation strategies section when bottlenecks are identified.

## Core Principles

- Visible bottlenecks enable informed architectural decisions.
- `[ReadOnly]` is only safe when the implementation truly doesn't mutate state.
- Trade-offs (performance vs. complexity) should be documented, not hidden.

## References

- Grain topology: `docs/grain-dependencies.md`
- Orleans reentrancy: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/orleans/grains/reentrancy)
- Instruction authoring: `.github/instructions/authoring.instructions.md`
