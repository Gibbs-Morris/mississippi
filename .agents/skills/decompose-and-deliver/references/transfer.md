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

The optional `scripts/snapshot-context.ps1` needs PowerShell 7.4 or later, Git and
Linux `stat` and `prlimit` utilities. Discover these commands before use; if missing,
inspect the same evidence with supported
file/Git tools. Invoke the script by its resolved package path with an explicit
`-RepositoryRoot` and discovered repository-relative `-ContextPath` values.
It returns paths, Git identity/status and selected input hashes. It neither
selects applicable instructions nor executes commands from their contents.
Public inspection runs in one owned worker bounded to thirty seconds, including
root discovery, filesystem existence/link probes, selected-path preflight and
inventory filtering. Slower or stalled filesystems take the manual fallback;
native Git, hashing and metadata children retain their shorter ten-second bounds.
Each captured native or worker output stream has a one-MiB byte limit. Oversized
inventories or diagnostics terminate the owned inspection and require manual
inspection before path splitting or JSON processing. Both streams are drained
in bounded chunks under the same deadline, including process exit and EOF.
The worker is gated until assigned to a private Windows job or a Linux session
and process group. Its native children inherit that ownership. Cleanup terminates
remaining members even after the worker exits and allows at most two seconds to
confirm no live members remain. Linux confirmation requires readable `/proc`
metadata and the inherited private ownership nonce; unrelated groups are never
signaled. Previously verified PID/start-time identities remain tracked while
exiting processes clear their environment. Unconfirmed termination requires manual reconciliation before retrying
or releasing resources. Unsupported job/group capabilities fail before target
inspection. The helper supports Windows and Linux; macOS uses manual inspection.
The hidden `InspectionContext` and `WaitForInspectionOwner` parameters are internal
entry points, not a public snapshot interface.
An initialized repository without commits reports `Head: null` and its unborn
branch explicitly. A missing detached HEAD remains an inspection failure.
Every Linux Git child starts through `prlimit` with a 256-MiB hard and soft
address-space ceiling before Git executes. The gated Windows job limits combined
committed memory for the worker and its children to 512 MiB. Limits are applied
before target inspection; unavailable enforcement, allocation failure or memory
pressure diagnostics require manual inspection. Timeout and captured-byte limits
also remain active. The core workflow does not require these optional mechanisms.
Ambient repository/index/object/configuration Git overrides and linked context paths fail
closed; use a clean process or explicit manual inspection rather than silently
reading another target.
Git ownership failures remain failures. Any ownership/trust decision belongs
outside the helper and must follow the target's authorization rules.
Repository-local `objects/info/alternates` and `http-alternates` also require
manual inspection, including regular files in shared clones. They are rejected
before Git reads objects, because a matching root and clean status do not prove
that the reported commit or its ancestry belongs to this repository's store.
Regular `info/grafts` metadata also requires manual inspection before Git reads
objects, even with deprecation advice suppressed and replacement refs disabled.
External `GIT_GRAFT_FILE` and `GIT_SHALLOW_FILE` overrides are rejected before Git
queries; embedded metadata inspection cannot verify an external history boundary.
Shallow boundaries also require manual inspection, including legitimate shallow
clones. Effective replacement refs, including packed refs, are rejected before
objects are attributed; invocation-only replacement disabling cannot protect
later ordinary review and publication commands. `GIT_REPLACE_REF_BASE` overrides
are rejected rather than hiding a different effective replacement namespace.
Inspection disables filesystem-monitor hooks and optional index writes for
each Git command; it does not change repository or account configuration.
Each Git command also uses `core.commitGraph=false`, so cached commit-graph tree
metadata cannot hide staged changes behind an otherwise matching reported HEAD.
Each Git command uses `core.untrackedCache=false`, so stale or crafted untracked
cache entries cannot conceal new files. The target's index remains unchanged.
Index cache-tree objects are checked against NUL-delimited staged files and
directory entries before each status observation, including valid children
under invalidated parents. Inspection uses bounded native tree reads without
writing the index or object store. Embedded indexes up to one MiB, versions
2–4 and SHA-1/SHA-256 object formats are supported. Split indexes, unknown
mandatory extensions and cache trees deeper than 256 levels require manual
inspection. The helper does not rebuild or discard target metadata.
Every inspection Git command also disables filename folding with
`core.ignoreCase=false`, preserving case-distinct instruction paths in status
and both inventory queries without changing the target's configuration.
Opaque directory entries from Git inventory require manual instruction discovery.
This includes ignored embedded repositories whose scoped guidance Git does not
enumerate through the outer repository's path queries.
Unix inventory entry types are checked in one bounded `stat` invocation; special
entries such as FIFOs and sockets require manual discovery before paths return.
Configured clean/process filters, including inherited LFS settings, require
manual file/commit inspection or an explicitly authorized trusted workflow.
The helper rejects them before status rather than changing normalization and
reporting misleading dirtiness. Serialize Git configuration changes during use.
Every tracked regular file is hashed with bounded `git hash-object --stdin-paths`
without `-w`, independently of cached index stat fields. Built-in line-ending,
encoding and ident normalization remain intact; executable clean/process filters
have already been rejected. Missing or mismatching content makes the result dirty.
Linked or special tracked files require manual inspection. UTF-8 input records
use Git quoting and a one-MiB limit, with concurrent input/output under the native
deadline. Neither index nor object store is written.
Two observations compare head, branch, full status, path inventory, staged
and actual tracked content identities and revalidated selected hashes. Observed changes fail closed.
Merge, rebase, cherry-pick, revert, sequencer, bisect and index-lock markers are checked before each
observation; their presence requires manual recovery even with empty status.
This bounded check does not promise an atomic snapshot; serialize conflicting work
and refresh evidence before using it.
Read selected bodies and follow the target's loading procedure. A snapshot is
evidence identity, not proof of successful tests or a security attestation.
Selected inputs must be regular files. Unix inspection uses the existing system
`stat` utility (GNU on Linux); absence requires manual inspection.
Linux/Windows paths are exercised locally; macOS ownership is unsupported.
Pipes, sockets and devices are rejected before hashing. Selected hashes run in
an owned child with a ten-second timeout, including file opening and reads.
Metadata and content come from the same open handle. Unix handle inspection
requires inherited descriptors and `/dev/fd`; unsupported capabilities fail
for manual inspection. Windows handle attributes require the runtime's
`File.GetAttributes(SafeFileHandle)` API. The hidden `HashPath` parameter is an
internal child entry point, not repository evidence or a replacement snapshot.
Selected identity includes file type and mode (Unix type/permission bits or
Windows attributes), so content hashes alone cannot establish unchanged inputs.
Git submodule entries require manual inspection, including uninitialized entries;
the helper rejects them before status can descend into nested configurations.
Assume-unchanged and skip-worktree index flags also require manual inspection;
otherwise Git status can conceal source edits or sparse working-copy state.
Configured `core.worktree` redirects require manual inspection, including
legitimate separated Git directories that use this setting. Root checks repeat
for both observations and before emission; nested ordinary working copies remain supported.
The helper supports embedded `.git` directories only and revalidates their binding
to the selected root. Git files or linked metadata directories, including legitimate
linked worktrees and submodules, require manual identity/backlink inspection.
The absolute common directory must also equal the embedded metadata directory;
`commondir` redirects require manual inspection even when the worktree/gitdir match.
The helper rejects linked entries anywhere inside embedded Git metadata before
running Git, including nested refs, objects and index links. Metadata traversal
is limited to 100,000 entries and ten seconds; larger inspections use the manual
fallback. An owned timed child also bounds blocking enumerator advancement;
the hidden `MetadataPath` parameter is its internal entry point. The traversal
inspects each entry before descending into a directory.
Inventory reads NUL-delimited UTF-8 paths with ordinal identity.
Ignored `AGENTS.md`, `*.instructions.md`, `CLAUDE.md`, Copilot's repository guide,
and explicitly selected context files also appear. Additional target-specific
guidance conventions still need independent discovery and instruction selection.
Existing untracked dangling links stay in the path inventory without
following their targets; actual deleted entries are excluded. Selected linked
content and live inventory links require manual inspection, including regular
targets. Instruction loading must not follow an unchecked link to a special file.
Missing link-metadata support fails.
The repository test harness reports this optional suite as skipped below
PowerShell 7.4, on unsupported platforms or without its Linux prerequisites.
The root permission fixture also reports an explicit skip without `setpriv`;
ordinary permission controls do not require that optional privilege utility.
Invalid encoding or any Git subprocess exceeding ten seconds fails instead of
emitting a snapshot.
Nonempty stderr also fails, even with an accepted exit code: traversal warnings
can mean that Git omitted unreadable paths from an apparently clean inventory.
Every query, including configuration reads, uses a timed native subprocess with
targeted child-process cleanup; blocking Git metadata takes a manual fallback.
Root containment uses exact ordinal spelling on every platform. Root case aliases
take a manual fallback even on case-insensitive Windows directories; Windows
per-directory case sensitivity is never assumed away.
Status uses command-local default stat checks with ctime trusted and ignoreStat
disabled. These overrides preserve configuration and index contents; a clean Git
view alone is not validation evidence for deliberately preserved metadata.
On Unix, status also honors executable-bit changes for every tracked file using
command-local `core.fileMode=true`, including files outside selected context.
Windows retains its filesystem's configured mode behavior.
Every tracked symlink requires manual content inspection, including dangling
links and legitimate Windows conversions with `core.symlinks=false`. Cached stat
fields can conceal changed link text; plain-file conversions can look clean while
their filesystem behavior differs.
Path joining and separator normalization follow the host filesystem: Unix
backslashes remain literal characters in inventories, roots and selected identities.
Git root paths lose only the command's single line terminator; real trailing
spaces, tabs, carriage returns and newlines remain part of Unix root identity.
Inspection disables replacement objects so status remains bound to the reported commit.
Partial/promisor repositories require manual inspection. Child-only environment
settings also disable lazy fetching and disallow all remote protocols, preserving
repository/account settings while preventing inspection from launching transports.

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
