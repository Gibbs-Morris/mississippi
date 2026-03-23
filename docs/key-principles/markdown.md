# Markdown

Every document in this repository, every pull request description, every README,
and every agent instruction file is written in Markdown. An agent that cannot
write correct, idiomatic Markdown will produce outputs that render incorrectly,
fail linting, and confuse both human readers and downstream tooling. This
document provides the authoritative reference.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

Markdown is a lightweight plain-text formatting syntax that converts to
structurally valid HTML. Mastering its core rules, common extensions, and
linting conventions is essential for anyone producing technical documentation,
repository files, or structured agent output.

---

## Situation

Software teams need a way to write formatted documents — headings, lists,
tables, code blocks, links — that can be read comfortably as plain text, render
correctly in web browsers, and be version-controlled in Git. The format must be
simple enough that authors focus on content rather than markup ceremony.

## Complication

HTML is too verbose for day-to-day writing. Rich-text editors produce opaque
binary formats that cannot be diffed. Wiki syntaxes vary across platforms.
Without a single, widely adopted standard, documents drift into inconsistent
formatting that breaks rendering and confuses automated tools.

## Question

What is the standard lightweight markup language for technical documentation,
and what does an agent need to know to produce correct, lint-clean Markdown?

---

## Key-Line 1: What Markdown Is

### Origin and Authorship

Markdown was created by **John Gruber** in collaboration with **Aaron Swartz**
and published on 17 December 2004. Gruber's canonical description remains at
<https://daringfireball.net/projects/markdown/>.

> "Markdown is a text-to-HTML conversion tool for web writers. Markdown allows
> you to write using an easy-to-read, easy-to-write plain text format, then
> convert it to structurally valid XHTML (or HTML)."
>
> — John Gruber, *Daring Fireball* (2004)

### Design Philosophy

Markdown's overriding design goal is **readability**. A Markdown document should
be publishable as-is, as plain text, without looking like it has been marked up
with tags or formatting instructions.

> "The single biggest source of inspiration for Markdown's syntax is the format
> of plain text email."
>
> — John Gruber, *Markdown: Syntax* (2004)

### CommonMark: The Specification

Gruber's original description left many edge cases ambiguous. In 2014, John
MacFarlane and a group of contributors published **CommonMark**, a rigorous,
unambiguous specification of Markdown syntax with a comprehensive test suite.

- Specification: <https://spec.commonmark.org/>
- Current version: 0.31.2 (2024)
- Reference implementation: `cmark` (C), with bindings in many languages

CommonMark is the baseline that GitHub Flavored Markdown (GFM), VS Code's
Markdown renderer, and most modern tools build upon.

---

## Key-Line 2: Core Syntax Reference

### Headings

Use ATX-style headings (hash marks). Always leave a blank line before a heading.
Always use a space after the hash marks.

```markdown
# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6
```

Do **not** use Setext-style (underlined) headings — they support only two levels
and are harder to scan in source.

### Emphasis

| Syntax | Renders as |
|---|---|
| `*italic*` or `_italic_` | *italic* |
| `**bold**` or `__bold__` | **bold** |
| `***bold italic***` | ***bold italic*** |
| `~~strikethrough~~` | ~~strikethrough~~ (GFM extension) |

Prefer asterisks over underscores for consistency and to avoid ambiguity in
mid-word emphasis.

### Lists

**Unordered lists** use `-`, `*`, or `+` as markers. Pick one and be consistent
(most linters default to `-`).

```markdown
- First item
- Second item
  - Nested item
```

**Ordered lists** use numbers followed by a period. The actual numbers do not
need to be sequential — Markdown renumbers automatically — but starting from `1`
and using `1.` for every item is a common lint-friendly pattern:

```markdown
1. First item
1. Second item
1. Third item
```

### Code

**Inline code** uses single backticks:

```markdown
Use the `dotnet build` command.
```

**Fenced code blocks** use triple backticks with an optional language
identifier for syntax highlighting:

````markdown
```csharp
public class Example
{
    public string Name { get; set; }
}
```
````

Always specify the language identifier — it enables syntax highlighting and
helps screen readers.

### Links and Images

```markdown
[Link text](https://example.com)
[Link with title](https://example.com "Title text")
![Alt text for image](path/to/image.png)
```

For reference-style links (useful when the same URL appears multiple times):

```markdown
See the [CommonMark specification][commonmark].

[commonmark]: https://spec.commonmark.org/
```

### Blockquotes

Prefix lines with `>`:

```markdown
> This is a blockquote.
>
> It can span multiple paragraphs.
```

### Horizontal Rules

Use three or more hyphens on a line by themselves, with blank lines above and
below:

```markdown
---
```

### Tables (GFM Extension)

```markdown
| Column A | Column B | Column C |
|---|---|---|
| Cell 1 | Cell 2 | Cell 3 |
| Cell 4 | Cell 5 | Cell 6 |
```

Alignment control:

| Syntax | Alignment |
|---|---|
| `:---` | Left-aligned |
| `:---:` | Centre-aligned |
| `---:` | Right-aligned |

### Task Lists (GFM Extension)

```markdown
- [x] Completed task
- [ ] Incomplete task
```

---

## Key-Line 3: GitHub Flavored Markdown (GFM)

GitHub Flavored Markdown is a superset of CommonMark maintained by GitHub. It
adds:

- **Tables** — pipe-delimited tables as shown above.
- **Task lists** — checkbox items in lists.
- **Strikethrough** — `~~text~~`.
- **Autolinks** — bare URLs are automatically linked.
- **Disallowed raw HTML** — certain HTML tags are filtered for security.
- **Footnotes** — `[^1]` syntax for footnote references.
- **Alerts** — `> [!NOTE]`, `> [!WARNING]`, `> [!IMPORTANT]`, `> [!TIP]`,
  `> [!CAUTION]` blockquote syntax for callout boxes.

Specification: <https://github.github.com/gfm/>

> "GitHub Flavored Markdown, often abbreviated as GFM, is the dialect of
> Markdown that is currently supported for user content on GitHub.com and
> GitHub Enterprise."
>
> — GitHub, *GitHub Flavored Markdown Spec* (2024)

---

## Key-Line 4: YAML Frontmatter

Many Markdown-based systems (Jekyll, Docusaurus, Hugo, VS Code agents, and this
repository's instruction and agent files) support YAML frontmatter — a block of
YAML at the very top of the file delimited by triple dashes:

```markdown
---
title: My Document
description: A brief summary
applyTo: '**/*.cs'
---

# Document content starts here
```

Frontmatter is not part of the CommonMark or GFM specification. It is a
convention adopted by static site generators, documentation frameworks, and
tooling. The parser treats everything between the opening `---` and closing
`---` as YAML metadata and does not render it as document content.

---

## Key-Line 5: Markdown Linting

### markdownlint

The most widely used Markdown linter is **markdownlint**, available as:

- A VS Code extension: `davidanson.vscode-markdownlint`
- A CLI tool: `markdownlint-cli2` (npm package)
- A library: `markdownlint` (Node.js)

Reference: <https://github.com/DavidAnson/markdownlint>

### Key Rules

| Rule | Description |
|---|---|
| MD001 | Heading levels should increment by one level at a time. |
| MD003 | Use consistent heading style (prefer ATX). |
| MD009 | No trailing spaces (use `<br>` for hard line breaks if needed). |
| MD012 | No multiple consecutive blank lines. |
| MD013 | Line length limit (configurable; many projects set 120 or disable). |
| MD022 | Headings should be surrounded by blank lines. |
| MD024 | No duplicate heading text (in same parent scope). |
| MD031 | Fenced code blocks should be surrounded by blank lines. |
| MD032 | Lists should be surrounded by blank lines. |
| MD033 | No inline HTML (configurable — some projects allow specific tags). |
| MD041 | First line should be a top-level heading. |
| MD047 | Files should end with a single newline character. |

### Configuration

Projects configure markdownlint via `.markdownlint.json`,
`.markdownlint.yaml`, or `.markdownlint-cli2.jsonc` at the repo root:

```json
{
  "MD013": { "line_length": 120 },
  "MD033": false
}
```

---

## Key-Line 6: Markdown in This Repository

In this repository, Markdown is used for:

| File Type | Location | Purpose |
|---|---|---|
| Instruction files | `.github/instructions/*.instructions.md` | Agent rules with YAML frontmatter |
| Agent definitions | `.github/agents/*.agent.md` | Custom agent personas with YAML frontmatter |
| Prompt files | `.github/prompts/*.prompt.md` | Reusable prompt templates |
| Skill definitions | `.github/skills/*/SKILL.md` | Agent skill packages |
| Documentation | `docs/Docusaurus/docs/**/*.md` | Docusaurus documentation site |
| Project root | `README.md`, `AGENTS.md`, `todo.md` | Repository-level documentation |
| PR descriptions | GitHub PRs | Pull request body content |

All authored Markdown that is within the repository's markdownlint scope should follow these conventions:

1. Pass markdownlint with the project's configured rules.
2. Use ATX-style headings.
3. Use fenced code blocks with language identifiers.
4. Include blank lines around headings, lists, and code blocks.
5. End with a single newline character.

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **Original Markdown** | Gruber, J. (2004). *Markdown*. Daring Fireball. <https://daringfireball.net/projects/markdown/> |
| **CommonMark specification** | MacFarlane, J. et al. (2014–2024). *CommonMark Spec*. <https://spec.commonmark.org/> |
| **GitHub Flavored Markdown** | GitHub. *GitHub Flavored Markdown Spec*. <https://github.github.com/gfm/> |
| **markdownlint** | Anson, D. *markdownlint*. <https://github.com/DavidAnson/markdownlint> |
| **VS Code Markdown** | Microsoft. *Markdown in VS Code*. <https://code.visualstudio.com/docs/languages/markdown> |

---

## Summary

Markdown is the universal lightweight markup language for technical
documentation. It was created by John Gruber in 2004, formalised by CommonMark
in 2014, and extended by GitHub Flavored Markdown for platform use. Core syntax
covers headings, emphasis, lists, code blocks, links, images, tables, and task
lists. YAML frontmatter provides metadata for static site generators and agent
 tooling. markdownlint enforces consistency for authored Markdown within the
 configured lint scope, excluding ignored paths such as `.scratchpad/**`,
 `.github/agents/**`, and `node_modules/**`.
