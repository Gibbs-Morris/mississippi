---
applyTo: '**'
---

# Repository Technology Stack

Governing thought: Use the established stack and obtain explicit user approval before introducing another language or scripting runtime.

> Drift check: Inspect tracked files, manifests, workflows, and the maintenance boundaries below; a file extension alone does not establish an approved use.

## Rules (RFC 2119)

- Agents **MUST** use C#/.NET, Blazor/Razor, PowerShell, and GitHub Actions YAML for new application code, tests, and automation, subject to the scoped maintenance boundaries below. Why: These are the repository's default technologies.
- Agents **MAY** maintain the supporting formats and existing non-default roles listed below within their established purposes. Why: The repository also needs documentation, styling, assets, configuration, and existing integrations.
- Agents **MUST** obtain explicit user approval before writing, modifying, or executing custom code in another language or scripting runtime outside those boundaries. Why: A general task request, an installed tool, or an existing file elsewhere is not approval to expand the stack.
- Agents **MUST** record approved departures, including the language/runtime, purpose, and affected paths, in the tracking issue and PR. Why: Approval is scoped to the authorized work rather than a permanent exception.
- Agents **MUST** apply these boundaries to source, tests, tooling, examples, ignored files, scratchpads, temporary files inside or outside the checkout, extensionless files, inline commands, and workflow or configuration snippets. Why: Uncommitted helpers and embedded code still introduce another language.
- Agents **MUST NOT** add Python or standalone JavaScript/Node helpers such as `.cjs` or `.mjs` files without explicit user approval. Why: One-off convenience scripts belong in PowerShell or C# by default.
- Agents **MAY** run established build, test, lint, browser, and GitHub tools whose internal implementation uses another language. Why: Invoking an existing tool does not authorize authoring custom code in its implementation language.
- Agents **MUST** request the missing approval and continue independent work within the established stack when a departure is needed. Why: The approval boundary preserves useful progress without silently changing the technology choice.

## Scope and Audience

All agents working on this repository, including local investigation and validation.
The maintenance boundaries permit the listed purposes, not general use of their
languages in new areas or new application, testing, or automation stacks.

## Maintenance Boundaries

| Area or format | Permitted purpose |
| --- | --- |
| CSS and HTML | Existing Blazor styling, client host markup, and documentation-site styling; existing CSS/token guidance still applies. |
| Markdown and text | Documentation, instructions, licensing, and analyzer release records. |
| JSON/JSONC, YAML, XML/MSBuild (`.csproj`, `.props`, `.slnx`), TOML, and repository settings | Configuration, manifests, lockfiles, test data, and existing tool settings; these formats do not authorize embedded code in another language. |
| PNG, JPG, SVG, and ICO | Images and icons; an asset format does not authorize embedded executable code. |
| TypeScript/TSX and React in `docs/Docusaurus/` | Maintenance of the existing documentation site, its configuration, and its Playwright tests; no general Node helpers or application logic. |
| `src/Reservoir.Client/wwwroot/mississippi.reservoir.devtools.js` | Maintenance of the existing browser DevTools interop; no standalone scripts or unrelated JavaScript features. |
| Existing Bash and JavaScript snippets in `.github/workflows/` | Maintenance of established Actions integration roles; new scripting steps use PowerShell. Generated workflow files retain their generator ownership. |
| `.agents/skills/address-pull-request-feedback/scripts/list-review-threads.graphql` | Maintenance of the existing GitHub review-thread query; no new query-language tooling or application layer. |

## At-a-Glance Quick-Start

Use PowerShell or C# for a new helper. Check both its language and purpose before
writing it, even if it will be temporary or ignored. If the work needs another
language or runtime, explain the need and obtain explicit approval first.

## References

- [Shared engineering guardrails](shared-policies.instructions.md)
- [PowerShell scripting](powershell.instructions.md)
- [CSS and design tokens](css-design-tokens.instructions.md)
- [Issue tracking and PR traceability](issue-tracking.instructions.md)
