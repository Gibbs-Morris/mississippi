---
applyTo: '**/*.cs'
---

# Feature-Centric C# Naming & Commenting Playbook

Governing thought: Use feature-first namespaces and StyleCop-compliant naming/documentation so identifiers explain themselves.

## Rules (RFC 2119)

- Namespaces **MUST** be feature-oriented (no generic "Services"/"Models"), max five PascalCase segments, with only industry-standard abbreviations. 
- Types: classes/records/structs **MUST** be PascalCase nouns; interfaces prefixed `I`; enums singular with PascalCase members; constants PascalCase. Boolean properties **MUST** start with `Is`/`Has`/`Can`/`Should`.
- Members: methods/properties **MUST** use PascalCase (verb phrases for methods, nouns for properties); DI dependencies **MUST** follow `private Type Name { get; }`; private fields/locals **MUST** be camelCase without underscores.
- Access: prefer least privilege; expose public interfaces/types only when deliberately part of the API; avoid inheritance unless justified.
- Documentation: every public/protected/unsealed symbol **MUST** have factual XML comments with `<summary>` and matching `<param>/<typeparam>/<returns>`; use imperative voice, no placeholders. `<example>` blocks **SHOULD** compile; internal/private docs only when behavior is non-trivial. StyleCop SA13xx/SA16xx **MUST** be clean.
- When large naming/doc cleanups are needed, agents **SHOULD** create small `.scratchpad/tasks` entries instead of bundling.
- Follow aligned guidance in C#, service-registration, logging, Orleans, and projects for consistent patterns.

## Scope and Audience

All C# code in this repository.

## Quick Start

- Derive namespaces from `Company.Product.Feature[.SubFeature]` (≤5 segments).
- Name types/members per rules above; prefer records/init-only when appropriate.
- Add concise XML docs and fix StyleCop naming/documentation warnings.

## Review Checklist

- [ ] Namespace and identifiers follow feature-first PascalCase rules; no underscores.
- [ ] Boolean prefixes and DI property pattern honored; private fields camelCase.
- [ ] Public/protected/unsealed symbols documented with correct tags and voice; StyleCop clean.
- [ ] Exposure and inheritance are minimal and justified.
