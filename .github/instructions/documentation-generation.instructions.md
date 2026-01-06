---
applyTo: '**/docs/**/*.mdx,**/docs/**/*.md,**/src/DocumentationGenerator/**/*.cs'
---

# Documentation Generation

Governing thought: Use the DocumentationGenerator console app to produce deterministic, code-derived Markdown/MDX documentation with Mermaid diagrams.

> Drift check: Review `src/DocumentationGenerator/` and `docsgen.json` for current report configuration and CLI options.

## Rules (RFC 2119)

- The generated documentation **MUST** be written only to `docs/Docusaurus/docs/generated/`; the generator **MUST NOT** modify any other directories or versioned docs. Why: Prevents accidental modification of hand-written content.
- Running the generator twice without code changes **MUST** produce byte-identical output; sorting, IDs, and file layout **MUST** be deterministic. Why: Enables reliable CI verification.
- Reports **MUST** be self-contained components that write to `{OutputDirectory}/{reportName}/`; new reports **MUST** be registered in `ReportRegistry` and follow the `IDocumentationReport` interface. Why: Maintains separation of concerns.
- The generator **MUST** autodiscover repository root, Docusaurus root, and solution files when not explicitly provided. Why: Removes hardcoded paths.
- All generated MDX files **MUST** include the auto-generated notice admonition. Why: Clearly indicates files should not be manually edited.
- CLI arguments **MUST** override configuration file settings; configuration file settings **MUST** override defaults. Why: Follows standard precedence.

## Scope and Audience

Developers adding or modifying documentation generation reports, or consuming generated documentation.

## At-a-Glance Quick-Start

- Run locally: `pwsh ./generate-docs.ps1`
- Run with specific reports: `pwsh ./generate-docs.ps1 -Reports "Dependencies","OrleansGrains"`
- Direct CLI: `dotnet run --project src/DocumentationGenerator/DocumentationGenerator.csproj --`
- Configuration: Edit `docsgen.json` in repository root

## Core Principles

- Generated content supplements hand-written documentation
- Mermaid diagrams provide visual architecture representations
- Orleans grain analysis captures runtime behavior classifications

## Available Reports

| Report | Description | Output Location |
|--------|-------------|-----------------|
| Index | Main index page linking to all reports | `generated/index.mdx` |
| Dependencies | Project dependency graphs per solution | `generated/dependencies/` |
| OrleansGrains | Grain call mapping and classifications | `generated/grains/` |

## Adding New Reports

1. Create a new class implementing `IDocumentationReport`
2. Set `Name`, `Description`, and `Order` properties
3. Implement `GenerateAsync` to write MDX files
4. Register the report in `Program.cs` with `registry.Register()`
5. Add tests for the new report

## References

- Docusaurus configuration: `docs/Docusaurus/docusaurus.config.ts`
- Generator project: `src/DocumentationGenerator/`
- Configuration: `docsgen.json`
