# Installation, context and runtime limits

Copy this entire skill directory, including its `LICENSE`, references, assets and scripts, into
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

The optional `scripts/snapshot-context.ps1` needs PowerShell 7 and Git. Discover
both commands before use; if missing, inspect the same evidence with supported
file/Git tools. Invoke the script by its resolved package path with an explicit
`-RepositoryRoot` and discovered repository-relative `-ContextPath` values.
It returns paths, Git identity/status and selected input hashes. It neither
selects applicable instructions nor executes commands from their contents.
Ambient repository/index/object Git overrides and linked context paths fail
closed; use a clean process or explicit manual inspection rather than silently
reading another target.
Git ownership failures remain failures. Any ownership/trust decision belongs
outside the helper and must follow the target's authorization rules.
Inspection disables filesystem-monitor hooks and optional index writes for
each Git command; it does not change repository or account configuration.
Configured clean/process filters, including inherited LFS settings, require
manual file/commit inspection or an explicitly authorized trusted workflow.
The helper rejects them before status rather than changing normalization and
reporting misleading dirtiness. Serialize Git configuration changes during use.
Two observations compare head, branch, full status, path inventory, staged
content and revalidated selected hashes. Observed changes fail closed. This
bounded check does not promise an atomic snapshot; serialize conflicting work
and refresh evidence before using it.
Read selected bodies and follow the target's loading procedure. A snapshot is
evidence identity, not proof of successful tests or a security attestation.
Selected inputs must be regular files. Unix runtimes without file-type metadata
require manual inspection; pipes, sockets and devices are rejected before hashing.

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
