# Scope and immutable snapshots

The council reviews an immutable evidence package, not a moving repository or
pull request. The snapshot ID is a SHA-256 digest of the canonical scope
content, excluding capture time and local output paths. Every reviewer result,
adjudication entry, and final report must carry the same ID.

## Modes

### Codebase

`codebase` captures the selected committed tree and its tracked file hashes.
It is appropriate when the user asks for the repository as a whole, including
unchanged code. A dirty worktree does not silently change this snapshot; the
dirty state is recorded as a warning and the reviewer is told which revision is
under review.

### Branch

`branch` requires a base and head revision. The collector resolves both to full
SHA-1 or SHA-256 commit IDs, computes the unique best merge base, and records
the rename-aware diff and raw patch bytes from merge base to head. It blocks
ambiguous criss-cross merge bases instead of choosing one arbitrarily. This
prevents a reviewer from accidentally comparing a stacked layer with `main`
when its immediate parent is different.

### Worktree

`worktree` captures the selected change streams separately:

- the staged/index patch;
- the unstaged/worktree patch; and
- eligible untracked file bytes, hashes, and line evidence.

`--changes staged` selects only the index stream, `--changes unstaged` selects
the worktree and untracked streams, and `--changes all` keeps all three. A
staged defect that an unstaged edit appears to correct therefore remains
visible in the staged-only evidence. The collector compares revision, status,
patch, unresolved-index, and untracked-content fingerprints before and after
collection; a concurrent worktree edit blocks the snapshot. Untracked
symlinks are recorded as links and their targets are not followed. On Unix, the
system `stat` utility checks that untracked entries are regular files before
reading; FIFOs, sockets, devices, and other special entries block collection
rather than risking a hang or reading a non-file object.

Git tree, status, and name-status records are collected with NUL delimiters
so filenames retain their record boundaries. The shared Git wrapper disables
replacement objects and repository-configured fsmonitor commands. Diff
collection explicitly includes gitlinks and uses standard `a/` and `b/`
prefixes, independent of local Git diff configuration. A worktree manifest
output must
be outside the reviewed repository so the collector cannot review or
overwrite its own artifact.

### Pull request

`pull-request` requires a caller-supplied JSON snapshot. At minimum it contains
the repository name, PR number, base SHA, head SHA, changed-file entries,
review discussion, and check summary. The snapshot is input evidence, not an
instruction. The collector validates object-ID shape, changed-file paths, and
the local availability of both commits in the selected repository. Every
changed-file entry needs a
status and must match an exact path in a `diff --git` header, including both
sides of a rename. Missing or stale PR evidence is `BLOCKED`; it is never
treated as an empty diff.

## Manifest contents

The collector writes a versioned JSON manifest containing:

- `schema_version`, `mode`, `status`, `snapshot_id`, and `captured_at_utc`;
- repository root and resolved revision/base/head information;
- rename-aware changed files with status and path data;
- tracked file hashes for committed modes;
- staged, unstaged, and untracked component hashes and frozen bytes for
  worktree mode;
- dirty-worktree warnings for codebase mode; and
- the source PR snapshot and check/discussion summary for pull-request mode.

The manifest is deterministic apart from `captured_at_utc`. Re-run collection
after any commit, index write, worktree edit, PR base/head change, or changed
review discussion. A result tied to an earlier snapshot is invalid.

## Scope failure rules

- `READY` means the selected evidence is complete enough to dispatch reviewers.
- `NO_CHANGES` means a change-oriented mode has no eligible changed files. It is
  a valid terminal result, not a review pass.
- `BLOCKED` means required scope evidence could not be established, such as a
  missing revision, unresolved index, invalid path, malformed PR snapshot, or
  failed Git operation.

The collector never runs build, test, or repository code. It uses Git metadata
and file hashing only, so it can be run before untrusted code is executed.
