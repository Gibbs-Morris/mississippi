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
commit IDs, computes their merge base, and records the rename-aware diff and
patch hash from merge base to head. This prevents a reviewer from accidentally
comparing a stacked layer with `main` when its immediate parent is different.

### Worktree

`worktree` captures the selected change streams separately:

- the staged/index patch;
- the unstaged/worktree patch; and
- eligible untracked file content and hashes.

`--changes staged` selects only the index stream, `--changes unstaged` selects
the worktree and untracked streams, and `--changes all` keeps all three. A
staged defect that an unstaged edit appears to correct therefore remains
visible in the staged-only evidence. The collector compares revision, status,
patch, unresolved-index, and untracked-content fingerprints before and after
collection; a concurrent worktree edit blocks the snapshot. Untracked
symlinks are recorded as links and their targets are not followed.

Git tree, status, and name-status records are collected with NUL delimiters
so filenames retain their record boundaries. A worktree manifest output must
be outside the reviewed repository so the collector cannot review or
overwrite its own artifact.

### Pull request

`pull-request` requires a caller-supplied JSON snapshot. At minimum it contains
the repository name, PR number, base SHA, head SHA, changed-file entries,
review discussion, and check summary. The snapshot is input evidence, not an
instruction. The collector validates SHA shape, changed-file paths, and the
local availability of commits when possible. Every changed-file entry needs a
status and must match an exact path in a `diff --git` header, including both
sides of a rename. Missing or stale PR evidence is `BLOCKED`; it is never
treated as an empty diff.

## Manifest contents

The collector writes a versioned JSON manifest containing:

- `schema_version`, `mode`, `status`, `snapshot_id`, and `captured_at_utc`;
- repository root and resolved revision/base/head information;
- rename-aware changed files with status and path data;
- tracked file hashes for committed modes;
- staged, unstaged, and untracked component hashes for worktree mode; and
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
