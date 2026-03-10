# Review 11: Source Generator And Tooling Specialist

- Issue: The plan should explicitly avoid using undocumented frontmatter fields for VS Code-first orchestration, especially the `agents` field, unless the final implementation step re-verifies both surfaces and accepts the tradeoff deliberately. Why it matters: the user asked for cross-surface correctness first, and GitHub Docs do not document all VS Code orchestration fields equally. Proposed change: record a deliberate omission: do not use `agents` in the initial family; rely on hidden specialists plus prompt instructions instead. Evidence: VS Code docs document `agents`, but GitHub Docs do not list it in the common frontmatter table. Confidence: High.
- Issue: The final verification pass should include a file-level schema review against the official docs pages used for planning. Why it matters: frontmatter drift is easy to miss in repetitive Markdown files. Proposed change: require a final checklist comparing every frontmatter block against the chosen allowed-field matrix before completion. Evidence: inference from the 21-file family size and cross-surface compatibility requirements. Confidence: High.

## CoV

- Claim: omitting `agents` is the safer compatibility default. Evidence: asymmetry between the VS Code docs and the GitHub Docs common frontmatter reference. Confidence: High.