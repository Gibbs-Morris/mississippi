---
applyTo: 'src/DocumentationGenerator/**,docs/Docusaurus/docs/generated/**,eng/src/agent-scripts/generate-docs.ps1'
---

# Documentation Generation

Governing thought: Generated documentation flows from source code via deterministic Roslyn analysis into the Docusaurus docs tree, producing byte-for-byte reproducible Mermaid diagrams.

> Drift check: Open `docsgen.json` and `src/DocumentationGenerator/README.md` before modifying generation behavior; they define the report registry and configuration schema.

## Rules (RFC 2119)

- Generated docs **MUST** be written only to `{DocusaurusRoot}/docs/generated/`; the generator **MUST NOT** modify other docs or versioned docs. Why: Safety and clear ownership.
- Running the generator twice without code changes **MUST** produce byte-for-byte identical output. Why: Determinism enables reliable CI diffs.
- All generated MDX files **MUST** use LF line endings and **MUST NOT** include timestamps or environment-specific paths. Why: Cross-platform consistency.
- Mermaid node IDs **MUST** be sanitized via `DeterministicWriter.SanitizeId()` to ensure valid identifiers. Why: Prevents Mermaid parsing errors.
- New reports **MUST** implement `IReport` and be registered in `Program.cs` DI configuration. Why: Maintains report registry pattern.
- Orleans grain analysis **MUST** use Roslyn `SemanticModel` to detect `IGrainBase` implementations and grain interface inheritance. Why: Accurate type resolution.
- Configuration changes **MUST** be reflected in `docsgen.json` and the `README.md` documentation. Why: Keeps config and docs in sync.

## Scope and Audience

Developers adding reports, modifying generation behavior, or troubleshooting documentation pipeline issues.

## At-a-Glance Quick-Start

- Run locally: `pwsh ./eng/src/agent-scripts/generate-docs.ps1`
- Run specific reports: `pwsh ./eng/src/agent-scripts/generate-docs.ps1 -Reports ProjectDependencies,OrleansGrains`
- View output: `docs/Docusaurus/docs/generated/`
- Configure: Edit `docsgen.json` at repository root

## Output Layout

```text
docs/Docusaurus/docs/generated/
├── index.mdx
├── dependencies/
│   ├── index.mdx
│   ├── mississippi-project-references.mdx
│   └── samples-project-references.mdx
├── classes/
│   ├── index.mdx
│   └── {ProjectName}.mdx
└── orleans/
    ├── index.mdx
    ├── grain-call-graph.mdx
    └── grain-call-matrix.mdx
```

## Report Types

| Report | Source | Output |
|--------|--------|--------|
| ProjectDependencies | .slnx XML parsing + .csproj ProjectReference | Mermaid graph LR |
| ClassDiagrams | Roslyn syntax/semantic analysis | Mermaid classDiagram |
| OrleansGrains | Roslyn IGrainBase detection + call site analysis | Mermaid graph LR + tables |

## Core Principles

- Source-based analysis (Roslyn) over runtime reflection
- Deterministic output for CI reliability
- Safe output isolation to `generated/` only
- Configuration-driven filtering and limits

## References

- Generator README: `src/DocumentationGenerator/README.md`
- Configuration: `docsgen.json`
- PowerShell script: `eng/src/agent-scripts/generate-docs.ps1`
- Docusaurus config: `docs/Docusaurus/docusaurus.config.ts`
