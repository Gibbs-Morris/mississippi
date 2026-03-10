# Repository Findings

## Search Terms Used

- `.github/agents`
- `*.agent.md`
- `handoffs:`
- `user-invocable`
- `disable-model-invocation`
- `manifest`
- `metadata:`

## Areas Inspected

- `agents.md`
- `.github/agents/`
- `.github/instructions/shared-policies.instructions.md`
- `.github/instructions/instruction-mdc-sync.instructions.md`
- Existing planner and builder agents in `.github/agents/`

## Findings

### 1. The repository already contains multiple agent families, so the new family needs a distinct short prefix.

Evidence:
- `.github/agents/` currently contains `flow-*`, `epic-*`, `CoV-mississippi-*`, `technical-writer.agent.md`, `issue-refiner.agent.md`, `rules-manager.agent.md`, and other standalone agent files.
- Existing agent names visible from the directory listing include `flow-planner.agent.md`, `flow-build.agent.md`, `epic-planner.agent.md`, `epic-builder.agent.md`, and multiple `CoV-mississippi-*` files.

Second source:
- No existing manifest-style family index file is present under `.github/agents/`; a search for `.github/agents/*manifest*.md` returned no results.

Implication for plan:
- The new family should adopt a new prefix that does not collide with `flow`, `epic`, `CoV`, or standalone agent names. A short new family prefix is justified and a family manifest will add discoverability missing today.

### 2. Repository policy treats repo-local instructions as authoritative over generic guidance.

Evidence:
- `agents.md` lines 1-19 require agents to read repository instructions first and to follow those documents when planning or writing code.

Second source:
- `.github/instructions/shared-policies.instructions.md` lines 1-27 define shared engineering guardrails such as zero warnings, centralized package management, and repo-specific policy precedence.

Implication for plan:
- The new family prompts must explicitly instruct agents to read repo-local policy files first and to fall back to solid, testable, enterprise-grade code only when the repo is silent.

### 3. Existing repository agents already use guided handoffs, so handoffs are a local precedent rather than a brand-new pattern.

Evidence:
- `.github/agents/epic-planner.agent.md` lines 1-8 define a `handoffs` block that targets `epic-builder`.

Second source:
- `.github/agents/epic-builder.agent.md` lines 1-11 define a `handoffs` block with transitions back to `epic-planner` and to the next builder step.

Implication for plan:
- Adding handoffs to the new entry agents is consistent with existing repo practice, provided the chosen handoff fields remain compatible with GitHub.com by being safely ignorable there.

### 4. Existing repo agents vary in sophistication, but the common baseline is still Markdown body plus YAML frontmatter in `.github/agents/`.

Evidence:
- `.github/agents/dev.agent.md` lines 1-4 use minimal frontmatter with only `description` and `name`.

Second source:
- `.github/agents/technical-writer.agent.md` lines 1-4 also use minimal frontmatter and then a structured Markdown body with mission, guardrails, workflow, and output expectations.

Implication for plan:
- The new family should use a consistent frontmatter subset and a repeatable prompt-body structure instead of inventing a novel file format.

### 5. Some existing agents use a `metadata` block, but that should not be treated as required for the new family.

Evidence:
- `.github/agents/flow-planner.agent.md` lines 1-10 include a `metadata` block.

Second source:
- `.github/agents/epic-builder.agent.md` lines 1-18 also include a `metadata` block.

Implication for plan:
- The new family should avoid depending on `metadata` for behavior because it is not part of the minimum documented cross-surface set and adds no correctness benefit for this workflow.

### 6. There is currently no discoverable family index for humans choosing among repo agents.

Evidence:
- The `.github/agents/` directory contains multiple families and standalone agents, but no manifest or index file describing when to start with which agent.

Second source:
- Search for `.github/agents/*manifest*.md` returned no results.

Implication for plan:
- The requested family manifest is useful, not redundant. It should clearly mark the three entry points as Start here and document internal-specialist usage.

## CoV

- Claim: a unique prefix is necessary. Evidence: multiple existing families in `.github/agents/`; no current family manifest to disambiguate them. Confidence: High.
- Claim: prompts must explicitly honor repo-local policy. Evidence: `agents.md` and shared engineering guardrails both require repo instructions to outrank generic advice. Confidence: High.
- Claim: handoffs fit the repo. Evidence: both epic planner and epic builder already use them. Confidence: High.
- Claim: metadata is optional rather than necessary. Evidence: local files are inconsistent about using it; no repo policy requires it. Confidence: Medium.
- Impact: the implementation plan should choose a unique prefix, add a manifest, use a consistent prompt template, and keep frontmatter to the documented cross-surface subset.