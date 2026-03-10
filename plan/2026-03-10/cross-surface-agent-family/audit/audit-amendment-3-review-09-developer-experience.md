# Amendment 3 Review — Developer Experience (DX) Reviewer

## Persona

Developer Experience — API ergonomics from the consuming developer's perspective, discoverability, pit-of-success design, error message quality, registration ceremony, number of concepts to learn, migration friction, sample/documentation alignment.

## Findings

### 1. FLAW — Cognitive load: user must understand 10+ artifact files before they can reason about workflow state

- **Issue**: The working directory contract defines 10 required files (00-09) and 5 optional files (10-14). A user inspecting the working directory sees a wall of numbered files with no obvious entry point.
- **Why it matters**: The whole point of the working directory is inspectability. If it's overwhelming, users won't inspect it.
- **Proposed change**: Make `09-handoff.md` the canonical "read this first" file. Rename or restructure its purpose to serve as both handoff brief AND status dashboard. Its required format should include: current phase, current blocker (if any), last completed action, which files contain the most relevant state, and what action is expected next.
- **Evidence**: Handoff Rule says "reference specific files the next agent should read first" but `09-handoff.md` is numbered ninth — a user wouldn't naturally start there. The Format Rules section defines a generic 7-part structure that doesn't prioritize discoverability.
- **Confidence**: High.

### 2. FLAW — No README or index file in the working directory

- **Issue**: When a user opens `./plan/2026-03-10/my-task/`, they see `00-intake.md` through `09-handoff.md` plus optional files. There's no `README.md` or index explaining what each file is and why it's there.
- **Why it matters**: First-time users of the VFE family will be confused by the file structure.
- **Proposed change**: Require the entry agent to create a brief `README.md` in the working directory at initialization. It should list each file, its purpose, and which is the current status file. One paragraph, one table — not a documentation effort.
- **Evidence**: Working Directory Contract defines files but has no index/readme requirement.
- **Confidence**: Medium — could also be solved by making `09-handoff.md` serve this purpose.

### 3. GAP — No "quick start" path for small tasks

- **Issue**: The plan optimizes for large enterprise tasks with full specialist rounds, ~15 Markdown files, multi-phase handoffs, and review loops. But many real uses will be: "Review this 50-line PR" or "Plan a simple config change." The plan doesn't offer a lightweight mode.
- **Why it matters**: If the family feels too heavy for small tasks, users will avoid it and use simpler families instead, reducing the verification-first coverage the family is meant to provide.
- **Proposed change**: Define a "light mode" protocol: "For tasks the entry agent judges to be small (single slice, no architecture impact, <100 lines changed), the agent may: skip working-directory creation, skip specialist invocation, and operate linearly. It should note this decision to the user."
- **Evidence**: The "When To Use This Family" section says to prefer simpler families for simple work, but this defeats the purpose — users should be able to use VFE for small tasks without full ceremony.
- **Confidence**: High.

### 4. GAP — Entry-agent argument-hint values not specified

- **Issue**: The frontmatter policy says `argument-hint` is allowed "when helpful for VS Code usability" but doesn't specify what the argument hints should be. The builder will have to invent them.
- **Why it matters**: Good argument hints significantly improve discoverability in VS Code's agent picker.
- **Proposed change**: Specify recommended argument hints:
  - `VFE Plan`: "Describe the feature or task you want to plan"
  - `VFE Build`: "Path to plan directory or describe what to implement"
  - `VFE Review`: "Branch name or describe what to review"
- **Evidence**: Frontmatter Policy → Entry-agent allowed fields mentions `argument-hint` without values.
- **Confidence**: High.

### 5. MINOR — 8-section prompt template might exceed what specialists need

- **Issue**: The prompt template requires 8 sections for every agent including specialists. Some specialists (e.g., `vfe-technical-writer`) don't need "Evidence to gather" as a separate section from their workflow.
- **Why it matters**: Prompt bloat reduces effective instruction quality.
- **Proposed change**: Allow specialists to merge or omit sections when the content would be redundant. The 8-section template is a guideline, not a rigid requirement for specialist bodies.
- **Evidence**: Prompt Template For Every Agent says "Use a consistent body shape" with 8 sections.
- **Confidence**: Medium.
