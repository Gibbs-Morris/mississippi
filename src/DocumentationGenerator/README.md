# DocumentationGenerator

A deterministic "code → docs" pipeline that generates Markdown/MDX documentation with Mermaid diagrams from the Mississippi repository's .NET code and project structure.

## Overview

The DocumentationGenerator is a .NET console application that:

1. Analyzes the repository's solution files and project structure
2. Generates Mermaid diagrams for project dependencies, class hierarchies, and Orleans grain interactions
3. Outputs MDX files into the Docusaurus documentation site

## Reports

The generator is organized around "reports" - self-contained components that produce documentation:

| Report | Description |
|--------|-------------|
| `ProjectDependencies` | Generates project dependency diagrams from .slnx solution files |
| `ClassDiagrams` | Generates class diagrams via Roslyn source analysis |
| `OrleansGrains` | Maps Orleans grain call relationships with concurrency classifications |

## Running Locally

### Using the PowerShell Script (Recommended)

```powershell
# Run all reports with defaults
pwsh ./eng/src/agent-scripts/generate-docs.ps1

# Run specific reports
pwsh ./eng/src/agent-scripts/generate-docs.ps1 -Reports ProjectDependencies,OrleansGrains

# Override output location
pwsh ./eng/src/agent-scripts/generate-docs.ps1 -OutputDir ./my-output
```

### Using dotnet run

```bash
# From repository root
dotnet run --project src/DocumentationGenerator -- --repoRoot . --config ./docsgen.json

# Run specific reports
dotnet run --project src/DocumentationGenerator -- --reports ProjectDependencies --reports OrleansGrains
```

## Configuration

Configuration is managed via `docsgen.json` at the repository root:

```json
{
  "enabledReports": [
    "ProjectDependencies",
    "ClassDiagrams",
    "OrleansGrains"
  ],
  "classDiagrams": {
    "includeProjects": [],
    "excludeProjects": ["Tests", "Benchmarks", "AppHost"],
    "maxTypesPerDiagram": 30,
    "publicOnly": false
  },
  "projectDependencies": {
    "includeSolutions": ["mississippi.slnx", "samples.slnx"],
    "excludeProjects": []
  },
  "orleans": {
    "excludeProjects": ["Tests", "Benchmarks"],
    "maxEdges": 100,
    "publicOnly": false
  }
}
```

## CLI Arguments

| Argument | Description |
|----------|-------------|
| `--repoRoot` | Override repository root path (autodiscovered by default) |
| `--docusaurusRoot` | Override Docusaurus root path (autodiscovered at docs/Docusaurus) |
| `--outputDir` | Override output directory (default: {DocusaurusRoot}/docs/generated) |
| `--slnx` | Solution files to analyze (repeatable; defaults to mississippi.slnx and samples.slnx) |
| `--reports` | Reports to run (repeatable; defaults to all enabled reports) |
| `--config` | Configuration file path (default: docsgen.json) |

## Output Structure

Generated documentation is written to `{DocusaurusRoot}/docs/generated/`:

```text
generated/
├── index.mdx                          # Overview and navigation
├── dependencies/
│   ├── index.mdx                      # Dependencies overview
│   ├── mississippi-project-references.mdx
│   └── samples-project-references.mdx
├── classes/
│   ├── index.mdx                      # Class diagrams overview
│   └── {ProjectName}.mdx              # Per-project class diagrams
└── orleans/
    ├── index.mdx                      # Orleans overview
    ├── grain-call-graph.mdx           # Grain interaction diagram
    └── grain-call-matrix.mdx          # Detailed call analysis
```

## Determinism

The generator is designed for deterministic output:

- Stable ordering of solutions, projects, types, and edges
- Sanitized Mermaid node IDs derived from stable inputs
- LF line endings
- No timestamps or environment-specific paths in output
- Running twice with no code changes produces byte-for-byte identical output

## Autodiscovery

The generator can autodiscover paths:

1. **Repository Root**: Walks up from current directory looking for both `mississippi.slnx` and `samples.slnx`
2. **Docusaurus Root**: Validates `docs/Docusaurus` contains `docusaurus.config.*` and `package.json`
3. **Docs Content**: Uses `{DocusaurusRoot}/docs`

If autodiscovery fails, clear error messages are provided with guidance on using explicit paths.

## Safety

- The generator **only** deletes and rewrites the `generated/` directory
- It never modifies other documentation files
- It never touches versioned docs (`versioned_docs/` or `versions.json`)

## Troubleshooting

### "Failed to discover repository root"

Ensure you're running from within the Mississippi repository, or specify `--repoRoot` explicitly.

### "Failed to discover Docusaurus root"

Ensure `docs/Docusaurus` exists with `docusaurus.config.ts` and `package.json`, or specify `--docusaurusRoot`.

### Build errors with MSBuild

The generator uses `Microsoft.Build.Locator` to find MSBuild. Ensure the .NET SDK is installed and on PATH.

### Empty Orleans diagrams

Orleans grain analysis requires grains that implement `IGrainBase`. Ensure the analyzed projects contain Orleans grains.
