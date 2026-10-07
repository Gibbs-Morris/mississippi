---
applyTo: '**'
---

# Repository Technology Stack

Governing thought: Use the established stack for repository contributions and required project tooling; API requests and disposable local helpers do not expand that stack.

> Drift check: Inspect tracked files, manifests, workflows, and the maintenance boundaries below; a file extension alone does not establish an approved use.

## Rules (RFC 2119)

- Agents **MUST** use C#/.NET, Blazor/Razor, PowerShell, and GitHub Actions YAML for new repository application code, tests, and automation, subject to the scoped maintenance boundaries below. Why: These are the repository's default technologies.
- Agents **MAY** maintain the supporting formats and existing non-default roles listed below within their established purposes. Why: The repository also needs documentation, styling, assets, configuration, and existing integrations.
- Agents **MUST** obtain explicit user approval before using another language or scripting runtime in repository contributions or required project tooling outside those boundaries. Why: A general task request, an installed tool, or an existing file elsewhere is not approval to expand the stack.
- Agents **MUST** record approved departures, including the language/runtime, purpose, and affected paths, in the tracking issue and PR when those records exist, or in the conversation or report for read-only work. Why: Approval is scoped to the authorized work rather than a permanent exception, and this rule does not require a PR for an investigation.
- Agents **MUST** apply these boundaries to delivered source, tests, reusable scripts, executable examples, and required project tooling, including embedded code and tracked, staged, untracked, or ignored files. Why: Commit status, location, and file extension do not exempt a project dependency or contribution.
- Agents **MUST NOT** add Python or standalone JavaScript/Node helpers such as `.cjs` or `.mjs` files to repository contributions or required project tooling without explicit user approval. Why: Project helpers use PowerShell or C# by default.
- Agents **MAY** author and execute API request payloads, including GraphQL queries and mutations, without separate language approval when used only to operate existing APIs for the authorized task. Why: A task-local request does not introduce a repository language or runtime.
- Agents **MAY** write, modify, and execute disposable local investigation or validation helpers in other languages, including Python and JavaScript, when they are not delivered and introduce no repository dependency or required project workflow. Why: Temporary agent tooling does not change the project's technology choices.
- Agents **MUST** preserve action-specific authorization, security policies, and independent trust checks when using API payloads or local helpers. Why: A language allowance does not authorize the action itself or bypass a tool's safeguards.
- Agents **MAY** run established build, test, lint, browser, and GitHub tools whose internal implementation uses another language. Why: Running an established tool does not make its implementation language part of the project stack.
- Agents **MAY** use host-required tool-call syntax and shell transport to orchestrate established tools and permitted commands, including passing arguments and reading results. Why: Tool invocation syntax does not introduce a project language or runtime.
- Agents **MUST** request the missing approval and continue independent work within the established stack when a departure is needed. Why: The approval boundary preserves useful progress without silently changing the technology choice.

## Scope and Audience

All agents working on this repository. The approval boundary covers repository
contributions and tooling that project build, test, CI, deployment, or maintenance
workflows require, even if that tooling is uncommitted or stored outside the
checkout. Disposable local helpers and task-local API payloads are allowed only
while they remain outside those contributions and required workflows.

The maintenance boundaries permit the listed purposes, not general use of their
languages in new repository areas or new application, testing, or automation stacks.

## Maintenance Boundaries

| Area or format | Permitted purpose |
| --- | --- |
| CSS and HTML | Existing Blazor styling, client host markup, and documentation-site styling; existing CSS/token guidance still applies. |
| Markdown, text, and Mermaid | Documentation, instructions, licensing, analyzer release records, and Mermaid documentation diagrams; this does not authorize arbitrary embedded executable code. |
| JSON/JSONC, YAML, XML/MSBuild (`.csproj`, `.props`, `.slnx`), TOML, and repository settings | Configuration, manifests, lockfiles, test data, and existing tool settings; these formats do not authorize embedded code in another language. |
| PNG, JPG, SVG, and ICO | Images and icons; an asset format does not authorize embedded executable code. |
| TypeScript/TSX and React in `docs/Docusaurus/` | Maintenance of the existing documentation site, its configuration, and its Playwright tests; no general Node helpers or application logic. |
| `src/Reservoir.Client/wwwroot/mississippi.reservoir.devtools.js` | Maintenance of the existing browser DevTools interop; no standalone scripts or unrelated JavaScript features. |
| Existing Bash and JavaScript snippets in `.github/workflows/` | Maintenance of established Actions integration roles; new scripting steps use PowerShell. Generated workflow files retain their generator ownership. |
| Existing shell command examples in documentation and skills | Maintenance of established command examples for existing tools and integrations; new automation and standalone helpers use PowerShell or C#. |
| `.agents/skills/address-pull-request-feedback/scripts/list-review-threads.graphql` | Maintenance and execution of the existing GitHub review-thread query, including trusted temporary copies required by the feedback workflow; its independent trust checks still apply. No new query-language tooling or application layer. |

## At-a-Glance Quick-Start

Use PowerShell or C# for new project helpers. API requests and disposable local
helpers need no separate language approval; their actions still need the usual
authorization. Before delivering a helper or making it a required project tool,
check its language and purpose and obtain approval for any departure.

## References

- [Shared engineering guardrails](shared-policies.instructions.md)
- [PowerShell scripting](powershell.instructions.md)
- [CSS and design tokens](css-design-tokens.instructions.md)
- [Issue tracking and PR traceability](issue-tracking.instructions.md)
