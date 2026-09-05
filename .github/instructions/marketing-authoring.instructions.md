---
applyTo: 'docs/Docusaurus/src/**/*.{md,mdx,ts,tsx},docs/Docusaurus/docusaurus.config.ts'
---

# Marketing Authoring

Governing thought: Mississippi marketing makes a bold, memorable startup case while keeping technical proof, product maturity, and trust boundaries honest.

> Drift check: Verify product claims against `README.md`, current source and tests, and the technical documentation authoring model before publishing.

## Rules (RFC 2119)

- Marketing authors **SHOULD** use a distinctive, opinionated voice. Why: Early-stage products need memorable ideas, not neutral feature catalogs.
- Marketing authors **MAY** use metaphor, contrast, and ambitious vision when doing so clarifies the product's point of view. Why: Creative language can make an unfamiliar application model understandable.
- Marketing authors **MUST** classify material claims as implemented capability, reasoned outcome, vision, measured result, or customer proof during drafting. Why: Each claim type requires different evidence and language.
- Aspirational claims **MUST** make future intent recognizable. Why: Readers need to distinguish ambition from current behavior.
- Aspirational claims **MUST NOT** be presented as implemented behavior or a current guarantee. Why: Vision cannot replace product evidence.
- Technical claims about APIs, defaults, security, authorization, compatibility, performance, reliability, supported providers, and production readiness **MUST** be backed by current repository evidence. Why: Creative license does not extend to technical contracts or trust boundaries.
- Outcome claims **MUST** identify the product mechanism that could produce the outcome. Why: Mechanism-led benefits remain testable before commercial metrics exist.
- Outcome claims **MUST NOT** imply measured impact without reproducible evidence. Why: Plausible value is not a measured result.
- Customer names, logos, quotations, adoption claims, case studies, and quantified results **MUST NOT** be published without attributable evidence and permission. Why: Fabricated social proof destroys trust.
- Early-alpha status and material adoption constraints **MUST** remain visible near evaluation or conversion actions. Why: Visitors should understand the maturity of what they are being invited to try.
- Each marketing page **MUST** answer one primary visitor question. Why: Page boundaries should follow reader intent rather than internal feature boundaries.
- Each marketing page **MUST** offer one primary action. Why: Competing calls to action obscure the evaluation path.
- New marketing pages **MUST** have a distinct audience question, unique evidence, a meaningful action, and enough durable content to avoid repetition. Why: One convincing page is more useful than a large empty sitemap.
- Calls to action **MUST** lead to maintained, verified destinations. Why: The first product interaction is part of the proof.
- Calls to action **MUST NOT** promise a frictionless demo, installation, or production path that has not been tested. Why: Unverified promises spend user trust immediately.
- AI messaging **MUST** distinguish deterministic framework generation from developer-owned domain work. Why: Mississippi generates repeatable seams rather than the whole application.
- AI messaging **MUST NOT** imply that generated MCP tools automatically inherit HTTP authorization. Why: Shared domain operations do not create a shared trust boundary.
- Marketing MDX **SHOULD** use Markdown for headings, prose, lists, links, and code whenever layout or behavior does not require JSX. Why: The landing page should remain content-first and approachable to the same authors who maintain the technical docs.
- JSX in marketing MDX **SHOULD** be limited to layout wrappers, styled calls to action, and genuinely interactive or reusable elements. Why: An `.mdx` extension should not conceal a second template language for ordinary copy.
- Marketing copy moved into technical documentation **MUST** be rewritten for the technical documentation claim model. Why: Marketing and documentation have different jobs even in one publishing system.

## Scope and Audience

Contributors and agents authoring Mississippi marketing pages, components, and
site-level marketing metadata under `docs/Docusaurus/`.

## At-a-Glance Quick-Start

- Start with one audience question and one action.
- Write the boldest honest promise the evidence can support.
- Connect every outcome to an implemented mechanism.
- Label vision as vision and measurements as measurements.
- Put exact behavior, configuration, and failure semantics in technical docs.
- Verify every destination before making it a call to action.

## Claim Ladder

| Claim Type | Treatment |
| --- | --- |
| Implemented capability | State directly and link to evidence |
| Reasoned outcome | Explain the causal mechanism and avoid measured language |
| Vision | Frame explicitly as what Mississippi is building toward |
| Measured result | Publish the method, date, environment, and result |
| Customer proof | Attribute it and confirm publication permission |

## Core Principles

- **Conviction With Receipts**: Strong opinions become credible when visitors can inspect the proof.
- **Ambition Is Not A Guarantee**: Vision should expand the story without falsifying the present.
- **One Page Must Earn The Next**: Expand the sitemap only when evidence and visitor intent justify it.
- **Trust Boundaries Stay Literal**: Security language is never metaphorical.

## References

- Framework maturity and public overview: `README.md`
- Technical documentation authoring: `.github/instructions/documentation-authoring.instructions.md`
- Public documentation guide: `docs/Docusaurus/docs/contributing/documentation-guide.md`
- Architectural model: `docs/Docusaurus/docs/concepts/architectural-model.md`
- Design goals and trade-offs: `docs/Docusaurus/docs/concepts/design-goals-and-trade-offs.md`
- Markdown standards: `.github/instructions/markdown.instructions.md`
