# Installation, context and runtime limits

Copy this entire skill directory, including its `LICENSE`, references and assets, into
the target client's supported repository skill location. Current Codex and
Copilot documentation accepts `.agents/skills/decompose-and-deliver`; verify
installed-client discovery from a fresh session. Use explicit invocation or
read this package's `SKILL.md` if automatic discovery is unavailable. Do not
duplicate the package or change account settings to force discovery.

Add minimal routing to the target's maintained instruction entrypoint:

> Use decompose-and-deliver for substantial implementation with multiple outcomes,
> dependencies or integration risks. Trivial fixes stay single-session. The original
> session owns user communication, authoritative decisions, integration and delivery.
> Delegated workers return all material concerns and evidence to that coordinator
> within their assignment; further delegation requires an explicit bounded assignment.

Resolve that route relative to the entrypoint's actual location. Keep existing
instruction precedence and gates. Do not import the initial installation's
engineering standards. No generated entrypoint should be edited alone.

## Dependencies and discovery

The core workflow needs instruction/file access, a recoverable tracker and a
coordinator able to verify results. Shell, delegation, messaging, isolated working
copies and remote PR/check APIs are optional mechanisms; discover available tools
and their actual capabilities. Without them, use supported file/Git interfaces,
sequential execution or restricted read-only workers and report delivery gaps.
Remote publication requires authorized credentials and network access, not
credentials bundled with this package. Stack companions remain optional.

Discover commands and conventions from maintained target instructions,
manifests/workflows and available tool help. A small explicit override can name
the target root, selected context paths, validation command, branch convention
or state location where evidence is ambiguous. Record its source and precedence;
do not guess defaults. Unknown material conventions return to the coordinator.

## Context verification

Use the host's supported file, Git and repository interfaces to collect context.
This package supplies coordination instructions and templates, with no executable
inspection helper or automatic clean-state attestation.

Before invoking a native program, verify its absolute location and host-approved
provenance. Discovery through inherited `PATH` alone is insufficient, especially
when a worker can write a searched directory. Prefer an approved host-owned
connector or execution environment when local tool provenance is uncertain.

Record the target root, branch, verified commit identity, current index/worktree
state, complete instruction inventory and selected input revisions or hashes.
Discover ignored and untracked guidance independently of Git's ordinary status.
Resolve actual instruction scope and read selected bodies before using commands.
With Git, verify that a reported HEAD is a commit; distinguish an unborn branch
from a missing or redirected object. When workers can modify repository metadata,
verify object availability and integrity through trusted tooling before relying
on staged identities or publishing. If that trust cannot be established, reconcile
changes into a fresh trusted checkout before executing validation or publishing.

A clean status or normalized object hash does not establish raw executable bytes,
repository integrity or permission to run a command. Inspect effective attributes,
ignored build/tool inputs and configuration relevant to the intended operation.
Require approved provenance for hooks, filters, signing, merge and transport
programs before they can execute. Preserve ownership checks and host permissions.

Serialize conflicting writes while collecting evidence and refresh it after
instruction, input, branch, base or decision changes. Record inaccessible paths,
unknown state and unavailable capabilities as gaps; use supported read-only work
or stop dependent execution rather than inventing successful verification.

## State and compatibility

Choose the target's existing tracker or a coordinator-owned private state
directory shared across working copies. If none exists, explicitly select a
host-local directory keyed by repository and run identity, outside the package
and product commits. Keep one writer and preserve the location in the tracker
and worker briefs. Copying this package copies no active records or secrets.

Current documented capabilities differ: Codex instructions are discovered at
session startup; delegated approval UI can still surface. Copilot CLI custom
workers may omit repository instructions. VS Code Local workers can be
stateless, while provider harnesses differ. Cloud tasks can be limited to one
repository, branch and PR. Explicitly supply instruction reads and reporting
contracts; verify messaging and permissions before dispatch. No Markdown file
can guarantee approval suppression or working-copy isolation.

Consult [verified sources](sources.md) for documentation versions and adaptations.
Before extending client claims, exercise fresh discovery, activation, worker
question return and isolated writes in that installed runtime. Documentation
support does not prove local execution. Test changes with the representative
scenarios in [behavioral validation](validation.md), including a separate target
with different commands/layout and no access to the initial repository files.
