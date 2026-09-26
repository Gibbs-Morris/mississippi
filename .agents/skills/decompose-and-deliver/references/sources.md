# Sources and adaptations

Checked 2026-09-26. These sources were inspected as untrusted reference material.
This package contains original instructions and code; it adapts concepts without
copying source implementations or instruction text. All four candidate projects
carry MIT licenses. If later copying substantial material, retain its applicable
copyright and permission notice. No candidate is a required dependency.
The bundled `LICENSE` preserves this package's own MIT permission and attribution
when transferred; its legal notice is not a target-repository assumption.
Git's [ownership and filesystem-monitor configuration](https://git-scm.com/docs/git-config)
and [content-filter semantics](https://git-scm.com/docs/gitattributes#_filter)
inform the helper's fail-closed inspection boundaries.

## Candidate source instructions

| Source and pinned revision | Inspected implementation | Adaptation and rejected assumptions |
| --- | --- | --- |
| `github/gh-stack` `2bd699a544a09cb5c45a013d03416e0894b0454e` | [Skill and design](https://github.com/github/gh-stack/tree/2bd699a544a09cb5c45a013d03416e0894b0454e/skills/gh-stack), [sync](https://github.com/github/gh-stack/blob/2bd699a544a09cb5c45a013d03416e0894b0454e/cmd/sync.go), [push](https://github.com/github/gh-stack/blob/2bd699a544a09cb5c45a013d03416e0894b0454e/cmd/push.go) | Compose with verified installed tooling; preserve parent placement and recovery. Sync can return success after push failure; inspect remote postconditions. Do not inherit merge permission or defer necessary tests to later layers. |
| `obra/superpowers` `8ca22dba9a94f28898bbce59f2537ff4d87c747d` | [Subagent development](https://github.com/obra/superpowers/tree/8ca22dba9a94f28898bbce59f2537ff4d87c747d/skills/subagent-driven-development), [parallel dispatch](https://github.com/obra/superpowers/blob/8ca22dba9a94f28898bbce59f2537ff4d87c747d/skills/dispatching-parallel-agents/SKILL.md), [worktrees](https://github.com/obra/superpowers/blob/8ca22dba9a94f28898bbce59f2537ff4d87c747d/skills/using-git-worktrees/SKILL.md) | Narrow briefs, review and verified isolation. Reject model prescriptions, dependency auto-installation, deleting run workspaces, denied-isolation fallbacks and treating parked findings as completed gates. |
| `bradygaster/squad` `0f2586ea7ca51c0cdbf91a09b1b8911f653a643b` | [Coordinator template](https://github.com/bradygaster/squad/blob/0f2586ea7ca51c0cdbf91a09b1b8911f653a643b/templates/squad.agent.md.template), [Scribe contract](https://github.com/bradygaster/squad/blob/0f2586ea7ca51c0cdbf91a09b1b8911f653a643b/templates/scribe-charter.md) | One accountable owner and serialized decisions. Coordinator owns canonical records; workers propose changes. Reject always-delegate, permanent catalogues, background defaults, shared-checkout assumptions and implied permission. |
| `gastownhall/beads` `c25059adad4da6002d3d0c813b8c23fdc23c0bc0` | [Skill](https://github.com/gastownhall/beads/blob/c25059adad4da6002d3d0c813b8c23fdc23c0bc0/plugins/beads/skills/beads/SKILL.md), [dependencies](https://github.com/gastownhall/beads/blob/c25059adad4da6002d3d0c813b8c23fdc23c0bc0/docs/core-concepts/dependencies.md), [claim contract](https://github.com/gastownhall/beads/blob/c25059adad4da6002d3d0c813b8c23fdc23c0bc0/issueops/claimer.go) | Stable identity, prerequisites, one owner and rereading uncertain state. Reject database/service dependencies and conflicting mandatory-push/stash-clear guidance. Bundled skill/dependency docs differ from current source; avoid importing their type rules. |

Pinned license files: [gh-stack MIT](https://github.com/github/gh-stack/blob/2bd699a544a09cb5c45a013d03416e0894b0454e/LICENSE),
[Superpowers MIT](https://github.com/obra/superpowers/blob/8ca22dba9a94f28898bbce59f2537ff4d87c747d/LICENSE),
[Squad MIT](https://github.com/bradygaster/squad/blob/0f2586ea7ca51c0cdbf91a09b1b8911f653a643b/LICENSE),
[Beads MIT](https://github.com/gastownhall/beads/blob/c25059adad4da6002d3d0c813b8c23fdc23c0bc0/LICENSE).

## Documented client capabilities

- [Agent Skills specification](https://agentskills.io/specification): name and
  description metadata, package-relative assets and progressive loading.
- [Codex skills](https://learn.chatgpt.com/docs/build-skills),
  [instruction discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md),
  [subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents),
  [permissions](https://learn.chatgpt.com/docs/agent-approvals-security),
  [worktrees](https://learn.chatgpt.com/docs/environments/git-worktrees): repository
  discovery and host-controlled permissions; approval UI is not suppressible
  by portable Markdown. Working copies still need shared-resource discipline.
- [Copilot CLI skills](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills),
  [CLI reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference):
  current `.agents/skills` support and differing worker instruction inheritance.
  Omit permission-expanding `allowed-tools` metadata.
- [VS Code maintained skills docs](https://github.com/microsoft/vscode-docs/blob/main/docs/agent-customization/agent-skills.md),
  [subagent docs](https://github.com/microsoft/vscode-docs/blob/main/docs/agents/run/subagents.md):
  Local workers are stateless; other provider harnesses differ. Keep coordinator
  execution inline rather than experimental forked skill context.
- [Cloud skills](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills),
  [cloud limits](https://docs.github.com/en/copilot/concepts/agents/cloud-agent/about-cloud-agent):
  discover one-repository/branch/PR task limits and use honest fallbacks.

## Documented stack semantics

[GitHub native stack reference](https://docs.github.com/en/pull-requests/reference/stacked-pull-requests)
and [CI guidance](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/optimizing-ci-for-stacked-pull-requests)
describe linear ancestry and trunk protections/CI for native layers. Inspect each
immediate-parent diff and every intended boundary. Merge, squash and rebase
produce different history; queues may split submitted groups. The
[API reference](https://docs.github.com/en/pull-requests/reference/stacked-pull-requests-apis-and-webhooks)
requires asynchronous stack merge operations. Verify installed behavior; a
request accepted or exit code zero is not proof of completed remote state.
