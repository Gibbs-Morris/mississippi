---
applyTo: "**/*.instructions.md"
---

# Instruction Authoring

## Scope
Writing `*.instructions.md` files. Overrides Markdown instructions for instruction files.

## Quick-Start
- Lowercase kebab-case names: `topic.instructions.md`
- Place in `.github/instructions/`
- YAML frontmatter with tight `applyTo` globs
- RFC 2119 keywords in uppercase
- Structure: Quick-Start → Scope → Core Principles → Implementation → Examples → Anti-Patterns → Enforcement

## Core Principles
One topic per file. Relative links. US English. Short sentences (15-20 words). Active voice. One action per step. Include "Why" lines. Real commands only. Document exit codes. Drift check note in Quick-Start.

## Template Sections
H1 title. YAML `applyTo`. Quick-Start (3-5 bullets/commands). Scope (what's in/out). Core Principles (MUST/SHOULD/MAY). Implementation details. Examples (1 GOOD, 1 BAD if needed). Anti-Patterns. Enforcement.

## Frontmatter
```yaml
---
applyTo: "**/*.{ext1,ext2}"
---
```

## Anti-Patterns
❌ Broad `**` globs. ❌ Placeholder examples. ❌ Secrets in examples. ❌ Duplicate content. ❌ Missing rationales.

## Enforcement
PR reviews: globs are tight, RFC 2119 correct, examples runnable, no duplication, drift check present. Mirror to `.mdc` via sync script.
