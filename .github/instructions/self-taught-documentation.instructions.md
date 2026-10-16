---
applyTo: 'docs/Docusaurus/tsconfig.json,docs/Docusaurus/docusaurus.config.ts,docs/Docusaurus/package.json,docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Self-Taught Lessons: Documentation

Governing thought: Preserve verified documentation configuration and rendering behavior.

> Drift check: Inspect installed package metadata, configuration, typecheck and browser output, and overlapping lessons before relying on this guidance.

## Rules (RFC 2119)

- TypeScript upgrades **SHOULD** validate configuration with both the compiler and Playwright. Why: Inherited `baseUrl` caused TS5102 in TypeScript 7; a `null` override typechecked but broke Playwright's `paths[1]` parsing. Equivalent supported options with relative `paths` passed both.
- Docusaurus upgrades **SHOULD** compare blanket future flags with the previous release's enabled options. Why: In 3.10, `v4: true` enabled Faster and broke a Webpack build without `@docusaurus/faster`; explicit prior flags restored it.
- Fresh npm resolutions **SHOULD** be checked against the site's Node requirement. Why: Mermaid's open-ended range selected 12.1.0, requiring Node 22.12; permitted release 11.17.2 kept the graph compatible with Node 20.
- Authors **SHOULD** quote Mermaid flowchart node labels containing parentheses or braces. Why: Two published diagrams showed parse errors despite a successful Docusaurus build.
- Documentation browser sweeps **SHOULD** wait for every source-declared Mermaid diagram. Why: The sweep passed with three selector diagrams removed until it checked expected SVG counts.

## Scope and Audience

Agents updating Docusaurus dependencies, configuration, or public documentation pages.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Documentation authoring](documentation-authoring.instructions.md)
- [TypeScript 7 migration](https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/)
- [Docusaurus 3.10 future flags](https://docusaurus.io/blog/releases/3.10)
