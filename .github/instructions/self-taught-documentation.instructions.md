---
applyTo: 'docs/Docusaurus/tsconfig.json,docs/Docusaurus/docusaurus.config.ts,docs/Docusaurus/package.json'
---

# Self-Taught Lessons: Documentation

Governing thought: Preserve the site's Node requirements and configured behavior during dependency updates.

> Drift check: Inspect installed package metadata, inherited configuration, and the site's typecheck and build output.

## Rules (RFC 2119)

- TypeScript upgrades **SHOULD** validate configuration with both the compiler and Playwright. Why: Inherited `baseUrl` caused TS5102 in TypeScript 7; a `null` override typechecked but broke Playwright's `paths[1]` parsing. Equivalent supported options with relative `paths` passed both.
- Docusaurus upgrades **SHOULD** compare blanket future flags with the previous release's enabled options. Why: In 3.10, `v4: true` enabled Faster and broke a Webpack build without `@docusaurus/faster`; explicit prior flags restored it.
- Fresh npm resolutions **SHOULD** be checked against the site's Node requirement. Why: Mermaid's open-ended range selected 12.1.0, requiring Node 22.12; permitted release 11.17.2 kept the graph compatible with Node 20.

## Scope and Audience

Agents updating Docusaurus dependencies or configuration.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [TypeScript 7 migration](https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/)
- [Docusaurus 3.10 future flags](https://docusaurus.io/blog/releases/3.10)
