---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Self-Taught Lessons: Documentation

Governing thought: Record verified documentation rendering failures that source and build checks missed.

> Drift check: Check overlapping instructions for conflicts or duplicates before adding a lesson.

## Rules (RFC 2119)

- Authors **SHOULD** quote Mermaid flowchart node labels containing parentheses or braces. Why: Two published diagrams showed parse errors despite a successful Docusaurus build.

- Documentation browser sweeps **SHOULD** wait for every source-declared Mermaid diagram. Why: The sweep passed with three selector diagrams removed until it checked expected SVG counts.

## Scope and Audience

Authors of public Docusaurus documentation pages.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Documentation authoring](documentation-authoring.instructions.md)
