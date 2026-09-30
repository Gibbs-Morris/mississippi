# Execution, integration and recovery

Read for substantial delegation or delivery operations. The target's instructions
and current tool help determine exact commands and policy gates.

## Dispatch and decisions

Dispatch only ready tasks whose prerequisites and resource reservations are
verified. Supply the complete bounded contract from the work-record template;
do not rely on inherited conversation, skills or instructions. Workers with
follow-up messaging return questions to the coordinator. Stateless workers
return a blocked report and receive a fresh assignment after resolution.

Answer routine questions from agreed requirements and evidence. Record material
assumptions. Consolidate related user questions in the original conversation,
without delaying urgent blockers. Send resulting decisions and the new decision
revision to all affected workers; stop or revoke stale work before redispatch.
If permissions are denied, retain the denial and affected scope; use a permitted
alternative or report the required approval through the coordinator. Do not
bypass host UI, make workers independently ask the user, or expand permissions.

Bound retries before dispatch. Diagnose repeated failures and change the
hypothesis or method before another equivalent attempt. Stop or quarantine
the old attempt before replacement, release resources only after observing its
termination, and preserve recoverable results. Reject late results until their
identity and applicability have been reconciled. Reduce concurrency when CI,
review or integration becomes the bottleneck.

## Integration and publication

One owner under the coordinator controls integration and stack mutation.
Before review commands or any Git mutation, inspect effective configuration
origins, environment overrides, attributes and executable settings in a trusted
host context. Require approved provenance for hooks (including the default
`.git/hooks` directory and `core.hooksPath`), signing programs and custom merge
drivers. Unapproved callbacks require a trusted host-owned execution environment
or a blocked operation; they are never covered by approval for the enclosing Git
command. Apply this preflight to commits, merges, cherry-picks, rebases, tags and
pushes. Keep required approved hooks/signing enabled; use an invocation-only
trusted empty hooks directory only where target policy authorizes disabling hooks.
Inspect signing controls such as `commit.gpgSign`, `merge.gpgSign`, `tag.gpgSign`,
`push.gpgSign`, `gpg.program`, `gpg.ssh.program` and format-specific programs.
Inspect `merge.*.driver` and attribute-selected drivers before integration;
validation afterward cannot authorize a program that already ran.
Inspect worker diffs and evidence before accepting results; preserve unrelated
work. Integrate using the target's conventions and validate the combination.
Shared contracts have one owner; dependent workers do not improvise them.

Before each remote mutation, inspect current local/remote state and operation
status. Verify non-interactive command help and explicit repository, branch,
base and PR targets. Keep recoverable refs before authorized history changes.
Before any remote access, inspect effective configuration origins, URL rewrites
and the resolved transport. Require approved provenance for executable transport
settings such as `core.sshCommand`, proxy commands and external helpers.
Require approved provenance for effective HTTPS configuration too: `http.proxy`,
`remote.<name>.proxy`, `http.sslVerify`, CA/certificate/key settings and
`http.curloptResolve`, including URL-scoped values and environment overrides.
Pinning a URL or ref does not authenticate the proxy route or TLS trust policy.
Do not supply credentials until the endpoint and transport trust are verified.
When a repository controls unapproved transport settings, use an approved host-owned
connector or verified transport environment; otherwise retain the prepared local
result and report the blocked remote action through the coordinator.
Check actual remote refs, ancestry, PR identity/base and check publication after
execution; exit code zero is insufficient. Reuse an existing matching PR after
partial publication. A failed creation does not prove no PR was created.

Freeze worker writes to shared Git configuration during publication preflight
and execution; otherwise use a trusted connector or report the operation blocked.
For every push, pin the verified endpoint and one explicit source-to-destination
refspec, such as `VERIFIED_SOURCE_OID:refs/heads/VERIFIED_BRANCH`. Inspect and reject
or neutralize `remote.<name>.mirror`, configured push refspecs and implicit tag
expansion before execution. An explicit refspec bypasses configured push mappings;
also disable mirror mode and follow-tags for that invocation, for example with
`-c remote.NAME.mirror=false -c push.followTags=false -c push.recurseSubmodules=no`
before `push` and `--no-mirror --no-follow-tags --recurse-submodules=no` after it.
Unless signed publication is expressly authorized, add `--no-signed` to neutralize
implicit `push.gpgSign` behavior. Authorized signed publication requires verified
signer provenance and the target's required signing policy; do not disable it.
Submodule publication needs separate authorization; verify required submodule
revisions are available before publishing the parent. Replace `NAME` with the verified remote;
check these options against current help. Reject or clear configured
`push.pushOption` values, for example with `-c push.pushOption=` before `push`;
they can alter server automation even when the destination ref is pinned.
Supply any authorized server options explicitly with `--push-option` after
checking the destination's semantics. Do not infer the destination from
`HEAD`, upstream configuration or push defaults. Wider ref sets, deletion and tag
publication require separate authorization and explicit expected destinations.
Use fast-forward updates where possible. Where an authorized rewrite is necessary,
capture the verified expected remote revision and use a lease naming that exact
destination and revision. A lease rejection requires fresh reconciliation, not
an unguarded force push. Check every affected remote ref afterward; do not retry
a running operation or overwrite unrelated remote work.

## Stacks and landing boundaries

For genuinely dependent PRs, discover a suitable installed stack skill/tool and
compose with it; read its design/recovery reference and current command help.
This package does not duplicate a command manual or require a particular tool.
If unavailable, use the target's authorized ordinary Git workflow only when it
can preserve every base/protection/check invariant; otherwise land dependencies
first with approval or report the limitation. Never silently install tooling.

Keep correct linear ancestry, immediate-parent diffs, intended bases and
necessary tests in each layer. Branching/joining work dependencies may need
independent PRs plus an explicit integration PR; do not invent multi-parent
stack support. Respect target gates before beginning dependent layers.
Verify actual native membership and protections rather than assuming manually
chained branches receive trunk CI. One-branch/one-PR hosted tasks require a
verified capable coordinator for wider delivery, or an honest sequential limit.

After lower-layer review edits or upstream changes, stop affected assignments,
update decisions, propagate using the verified stack workflow and revalidate
affected boundaries. After squash/rebase merges, compare actual remote ancestry
and patch content with recoverable old refs; recover descendants without replaying
already merged changes. Interrupted rebases require inspecting Git's actual
state before continuing or aborting. Partial pushes require reconciling each
remote ref and PR independently. Keep evidence tied to the resulting revisions.

Merge methods and queues can alter ancestry and group landing. Verify all
selected boundaries and applicable checks/approvals/comments for the actual
candidate; grouped submission does not promise atomic deployment. Report
unexercised queue behavior explicitly. Merging and deployment still require
separate authorization; publication alone is not a merge.
