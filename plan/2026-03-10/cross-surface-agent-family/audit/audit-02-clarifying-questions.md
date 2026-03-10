# Clarifying Questions

## (A) Answered From Repo And Official Docs

### 1. Should the family use shared files across VS Code and GitHub.com?

Answer:
- Yes. Official GitHub Docs describe a common YAML frontmatter surface shared across GitHub.com, Copilot CLI, and supported IDEs unless otherwise noted.
- Official VS Code Docs describe `.agent.md` files in `.github/agents/` and note cross-environment behaviors such as `target`, `handoffs`, and hidden subagents.

Evidence:
- GitHub Docs: `Custom agents configuration` -> `YAML frontmatter properties` and the note stating `argument-hint` and `handoffs` are ignored on GitHub.com for compatibility.
- VS Code Docs: `Custom agents in VS Code` -> `Custom agent file structure`.

Triangulation:
- The two docs independently support a shared-file strategy.

### 2. Can `target` stay unset for cross-surface compatibility?

Answer:
- Yes. Official GitHub Docs state that if `target` is unset, the agent defaults to both environments.
- Official VS Code Docs list `target` as optional and define `vscode` and `github-copilot` values without requiring one.

Recommended default:
- Leave `target` unset unless a future agent genuinely must be surface-specific.

### 3. Is it safe to add `handoffs` for VS Code even though GitHub.com has a narrower frontmatter surface?

Answer:
- Yes, if the family does not depend on `handoffs` for correctness. GitHub Docs explicitly say `handoffs` are not supported on GitHub.com and are ignored to ensure compatibility.
- VS Code Docs document `handoffs` as a supported feature for guided sequential workflows.

Recommended default:
- Use `handoffs` only on the three entry agents, with `send: false`, and make sure the prompt body still works when handoffs are ignored.

### 4. Should tools be left unconstrained by default?

Answer:
- Yes. GitHub Docs state that omitting `tools` enables all available tools.
- VS Code Docs and tool docs describe tool availability as environment-driven, with custom agents able to scope tools only when helpful.

Recommended default:
- Leave `tools` unset unless a specific entry-agent capability requires a deliberate override.

### 5. How should visibility be controlled?

Answer:
- VS Code Docs document `user-invocable: false` for hidden subagents and `disable-model-invocation: true` for agents that should only be user-selected.
- GitHub Docs document the same fields and semantics for manual selection versus programmatic access.

Recommended default:
- Entry agents: `user-invocable: true`, `disable-model-invocation: true`.
- Internal specialists: `user-invocable: false`, `disable-model-invocation: false`.

### 6. Is `GPT-5.4 (copilot)` a valid preferred model choice?

Answer:
- Yes. GitHub Docs list `GPT-5.4` as a supported AI model across clients, including GitHub.com and supported IDEs.
- VS Code Docs specify that custom-agent `model` values and handoff `model` values use qualified names such as `GPT-5 (copilot)`.

Recommended default:
- Use `model: GPT-5.4 (copilot)` as the primary per-agent model string.

## (B) Questions For User

No blocking user questions remain after repo and docs verification.

The request already specifies the workflow shape, required roles, compatibility priorities, and preferred defaults clearly enough to proceed.

## CoV

- Claim: there are no blocking clarifying questions. Evidence: the request specifies required files, roles, naming pattern, model preference, tool strategy, handoff strategy, and compatibility preference in detail. Confidence: High.
- Claim: default decisions can now be recorded without more user input. Evidence: official docs resolved the schema and compatibility questions that could have blocked design. Confidence: High.
- Impact: proceed directly to decisions and the draft plan.