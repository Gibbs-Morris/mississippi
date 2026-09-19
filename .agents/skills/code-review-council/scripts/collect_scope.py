#!/usr/bin/env python3
"""Collect an immutable, review-only scope manifest for Code Review Council."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = "code-review-council/v1"
SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
MODES = ("codebase", "branch", "worktree", "pull-request")


class CollectionError(RuntimeError):
    """An expected failure while establishing the review scope."""


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_json(value: Any) -> str:
    encoded = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return sha256_bytes(encoded)


def iso_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def safe_path(path: str) -> str:
    """Return a portable repository-relative path or reject traversal."""

    candidate = path.replace("\\", "/") if os.sep == "\\" else path
    if not candidate or candidate.startswith("/") or re.match(r"^[A-Za-z]:/", candidate):
        raise CollectionError(f"absolute or empty path is not allowed: {path!r}")
    parts = [part for part in candidate.split("/") if part not in ("", ".")]
    if any(part == ".." for part in parts):
        raise CollectionError(f"path escapes repository root: {path!r}")
    return "/".join(parts)


def safe_io_path(value: str | Path, *, must_exist: bool, label: str, require_file: bool = True) -> Path:
    """Constrain CLI file paths to the worktree or OS temporary directory."""

    candidate = Path(value).expanduser()
    if ".." in candidate.parts:
        raise CollectionError(f"{label} path traversal is not allowed: {value!s}")
    try:
        resolved = candidate.resolve(strict=must_exist)
    except OSError as error:
        raise CollectionError(f"cannot resolve {label} path: {value!s}") from error
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise CollectionError(f"{label} path must be within the worktree or temporary directory: {value!s}")
    if must_exist and require_file and not resolved.is_file():
        raise CollectionError(f"{label} path is not a regular file: {value!s}")
    if must_exist and not require_file and not resolved.is_dir():
        raise CollectionError(f"{label} path is not a directory: {value!s}")
    return resolved


def validate_revision(value: str) -> str:
    if not value or value.startswith("-") or not re.fullmatch(r"[A-Za-z0-9_./~^:@+,-]+", value):
        raise CollectionError(f"revision contains unsupported characters: {value!r}")
    return value


def run_git(repo: Path, *arguments: str, check: bool = True) -> bytes:
    command = ["git", "-c", f"safe.directory={repo.as_posix()}", *arguments]
    completed = subprocess.run(command, cwd=repo, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if check and completed.returncode != 0:
        detail = completed.stderr.decode("utf-8", errors="replace").strip()
        raise CollectionError(f"Git command failed ({completed.returncode}): {' '.join(command)}; {detail}")
    return completed.stdout


def resolve_repository(value: str) -> Path:
    requested = safe_io_path(value, must_exist=True, label="repository", require_file=False)
    if not requested.exists() or not requested.is_dir():
        raise CollectionError(f"repository path does not exist: {requested}")
    top_level = run_git(requested, "rev-parse", "--show-toplevel").decode().strip()
    repository = Path(top_level).resolve()
    if not (repository / ".git").exists() and not (repository / ".git").is_file():
        raise CollectionError(f"resolved path is not a Git worktree: {repository}")
    return repository


def resolve_commit(repo: Path, revision: str) -> str:
    revision = validate_revision(revision)
    resolved = run_git(repo, "rev-parse", "--verify", "--end-of-options", f"{revision}^{{commit}}").decode().strip()
    if not SHA_PATTERN.fullmatch(resolved):
        raise CollectionError(f"revision did not resolve to a full commit SHA: {revision!r}")
    return resolved


def object_exists(repo: Path, revision: str) -> bool:
    if not SHA_PATTERN.fullmatch(revision):
        return False
    command = [
        "git",
        "-c",
        f"safe.directory={repo.as_posix()}",
        "cat-file",
        "-e",
        f"{revision}^{{commit}}",
    ]
    completed = subprocess.run(command, cwd=repo, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
    return completed.returncode == 0


def parse_tree(repo: Path, revision: str) -> list[dict[str, Any]]:
    raw = run_git(repo, "ls-tree", "-r", "-l", "-z", "--full-tree", revision)
    entries: list[dict[str, Any]] = []
    for record in raw.split(b"\0"):
        if not record:
            continue
        header, separator, path_bytes = record.partition(b"\t")
        if separator != b"\t":
            raise CollectionError("Git returned an invalid tree record")
        fields = header.decode("ascii", errors="strict").split()
        if len(fields) != 4 or fields[1] not in ("blob", "commit"):
            continue
        path = safe_path(os.fsdecode(path_bytes))
        if fields[1] == "commit":
            entries.append({"path": path, "gitlink_sha": fields[2], "type": "gitlink"})
        else:
            entries.append(
                {
                    "path": path,
                    "blob_sha": fields[2],
                    "size": None if fields[3] == "-" else int(fields[3]),
                }
            )
    return sorted(entries, key=lambda entry: entry["path"])


def parse_name_status(raw: bytes) -> list[dict[str, Any]]:
    tokens = raw.split(b"\0")
    result: list[dict[str, Any]] = []
    index = 0
    while index < len(tokens):
        if not tokens[index]:
            index += 1
            continue
        status = tokens[index].decode("utf-8", errors="replace")
        index += 1
        if index >= len(tokens):
            raise CollectionError("Git returned an incomplete name-status record")
        first_path = safe_path(os.fsdecode(tokens[index]))
        index += 1
        if status[:1] in ("R", "C"):
            if index >= len(tokens):
                raise CollectionError("Git returned an incomplete rename record")
            second_path = safe_path(os.fsdecode(tokens[index]))
            index += 1
            result.append({"status": status, "old_path": first_path, "path": second_path})
        else:
            result.append({"status": status, "path": first_path})
    return result


def diff_files(repo: Path, *revisions: str, cached: bool = False) -> list[dict[str, Any]]:
    arguments = ["diff", "--name-status", "--find-renames", "-z", "--no-ext-diff"]
    if cached:
        arguments.append("--cached")
    arguments.extend(revisions)
    return parse_name_status(run_git(repo, *arguments))


def patch_component(repo: Path, *revisions: str, cached: bool = False) -> dict[str, Any]:
    arguments = ["diff", "--binary", "--find-renames", "--no-ext-diff"]
    if cached:
        arguments.append("--cached")
    arguments.extend(revisions)
    content = run_git(repo, *arguments)
    return {
        "sha256": sha256_bytes(content),
        "bytes": len(content),
        "encoding": "base64",
        "content_base64": base64.b64encode(content).decode("ascii"),
    }


def parse_status(repo: Path) -> list[dict[str, Any]]:
    raw = run_git(repo, "status", "--porcelain=v1", "-z", "--untracked-files=all")
    tokens = raw.split(b"\0")
    result: list[dict[str, Any]] = []
    index = 0
    while index < len(tokens):
        token = tokens[index]
        index += 1
        if not token:
            continue
        if len(token) < 3 or token[2:3] != b" ":
            raise CollectionError("Git returned an invalid porcelain status record")
        code = token[:2].decode("ascii", errors="replace")
        path = safe_path(os.fsdecode(token[3:]))
        entry: dict[str, Any] = {"index": code[0], "worktree": code[1], "path": path}
        if code[0] in ("R", "C") or code[1] in ("R", "C"):
            if index >= len(tokens):
                raise CollectionError("Git returned an incomplete status rename record")
            entry["old_path"] = safe_path(os.fsdecode(tokens[index]))
            index += 1
        result.append(entry)
    return result


def real_file_entry(repo: Path, relative_path: str) -> dict[str, Any]:
    relative = safe_path(relative_path)
    candidate = (repo / Path(*relative.split("/"))).resolve(strict=True)
    try:
        candidate.relative_to(repo)
    except ValueError as error:
        raise CollectionError(f"file path resolves outside repository: {relative_path!r}") from error
    if not candidate.is_file():
        raise CollectionError(f"untracked path is not a regular file: {relative_path!r}")
    content = candidate.read_bytes()
    return {"path": relative, "sha256": sha256_bytes(content), "size": len(content)}


def material_id(material: dict[str, Any]) -> str:
    return f"sha256:{sha256_json(material)}"


def base_manifest(repo: Path, mode: str) -> dict[str, Any]:
    return {
        "schema_version": SCHEMA_VERSION,
        "mode": mode,
        "status": "BLOCKED",
        "snapshot_id": "sha256:" + ("0" * 64),
        "repository": {"root": str(repo)},
        "captured_at_utc": iso_now(),
    }


def finalize(manifest: dict[str, Any], material: dict[str, Any]) -> dict[str, Any]:
    manifest["snapshot_id"] = material_id(material)
    manifest["snapshot_material"] = material
    return manifest


def collect_codebase(repo: Path, head: str) -> dict[str, Any]:
    revision = resolve_commit(repo, head)
    files = parse_tree(repo, revision)
    material = {"mode": "codebase", "revision": revision, "files": files}
    manifest = base_manifest(repo, "codebase")
    manifest.update({"status": "READY", "revision": revision, "files": files, "changed_files": []})
    return finalize(manifest, material)


def collect_branch(repo: Path, base: str, head: str) -> dict[str, Any]:
    base_revision = resolve_commit(repo, base)
    head_revision = resolve_commit(repo, head)
    merge_base = run_git(repo, "merge-base", base_revision, head_revision).decode().strip()
    if not SHA_PATTERN.fullmatch(merge_base):
        raise CollectionError("Git did not return a full merge-base SHA")
    changed_files = diff_files(repo, merge_base, head_revision)
    patch = patch_component(repo, merge_base, head_revision)
    material = {
        "mode": "branch",
        "base": base_revision,
        "head": head_revision,
        "merge_base": merge_base,
        "changed_files": changed_files,
        "patch": patch,
    }
    manifest = base_manifest(repo, "branch")
    manifest.update(
        {
            "status": "READY" if changed_files else "NO_CHANGES",
            "base": base_revision,
            "head": head_revision,
            "merge_base": merge_base,
            "changed_files": changed_files,
            "patch": patch,
        }
    )
    return finalize(manifest, material)


def collect_worktree(repo: Path, changes: str) -> dict[str, Any]:
    revision = resolve_commit(repo, "HEAD")
    statuses = parse_status(repo)
    unresolved_index = bool(run_git(repo, "ls-files", "-u", "-z"))
    staged_files = diff_files(repo, cached=True)
    unstaged_files = diff_files(repo)
    staged_patch = patch_component(repo, cached=True)
    unstaged_patch = patch_component(repo)
    untracked = [
        real_file_entry(repo, status["path"])
        for status in statuses
        if status["index"] == "?" and status["worktree"] == "?"
    ]
    selected: dict[str, Any] = {"selection": changes}
    if changes in ("staged", "all"):
        selected["staged"] = {"files": staged_files, "patch": staged_patch}
    if changes in ("unstaged", "all"):
        selected["unstaged"] = {"files": unstaged_files, "patch": unstaged_patch}
        selected["untracked"] = untracked
    selected_files = []
    for stream in (selected.get("staged", {}), selected.get("unstaged", {})):
        selected_files.extend(stream.get("files", []))
    selected_files.extend({"status": "??", **entry} for entry in selected.get("untracked", []))
    selected_files = sorted(selected_files, key=lambda entry: (entry.get("path", ""), entry.get("status", "")))
    material = {
        "mode": "worktree",
        "revision": revision,
        "selection": changes,
        "statuses": statuses,
        "selected": selected,
        "unresolved_index": unresolved_index,
    }
    manifest = base_manifest(repo, "worktree")
    manifest.update(
        {
            "revision": revision,
            "statuses": statuses,
            "selected": selected,
            "changed_files": selected_files,
            "unresolved_index": unresolved_index,
            "status": "BLOCKED"
            if unresolved_index
            else ("READY" if selected_files else "NO_CHANGES"),
        }
    )
    return finalize(manifest, material)


def validate_pr_snapshot(repo: Path, source: Path) -> dict[str, Any]:
    source = safe_io_path(source, must_exist=True, label="pull-request snapshot")
    try:
        snapshot = json.loads(source.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise CollectionError(f"cannot read pull-request snapshot: {error}") from error
    if not isinstance(snapshot, dict):
        raise CollectionError("pull-request snapshot must be a JSON object")
    required = ("repository", "number", "base_sha", "head_sha", "changed_files", "diff", "discussion", "checks")
    missing = [field for field in required if field not in snapshot]
    if missing:
        raise CollectionError(f"pull-request snapshot is missing: {', '.join(missing)}")
    for field in ("base_sha", "head_sha"):
        if not isinstance(snapshot[field], str) or not SHA_PATTERN.fullmatch(snapshot[field]):
            raise CollectionError(f"pull-request {field} must be a full commit SHA")
    if not isinstance(snapshot["changed_files"], list):
        raise CollectionError("pull-request changed_files must be an array")
    for changed in snapshot["changed_files"]:
        if not isinstance(changed, dict) or not isinstance(changed.get("path"), str):
            raise CollectionError("pull-request changed_files contains an invalid path entry")
        safe_path(changed["path"])
    if not isinstance(snapshot["diff"], str):
        raise CollectionError("pull-request diff must be captured as text")
    has_files = bool(snapshot["changed_files"])
    has_diff = bool(snapshot["diff"].strip())
    if has_files != has_diff:
        raise CollectionError("pull-request changed_files and diff disagree about whether changes exist")
    for changed in snapshot["changed_files"]:
        candidate_paths = [changed["path"]]
        if changed.get("old_path"):
            candidate_paths.append(changed["old_path"])
        if not any(path in snapshot["diff"] or f"a/{path}" in snapshot["diff"] or f"b/{path}" in snapshot["diff"] for path in candidate_paths):
            raise CollectionError(f"pull-request diff does not contain changed path: {changed['path']}")
    local_availability = {
        "base": object_exists(repo, snapshot["base_sha"]),
        "head": object_exists(repo, snapshot["head_sha"]),
    }
    snapshot_copy = json.loads(json.dumps(snapshot, ensure_ascii=False, sort_keys=True))
    material = {"mode": "pull-request", "snapshot": snapshot_copy}
    manifest = base_manifest(repo, "pull-request")
    manifest.update(
        {
            "status": "READY" if has_files else "NO_CHANGES",
            "base": snapshot["base_sha"],
            "head": snapshot["head_sha"],
            "pull_request": snapshot_copy,
            "local_commit_availability": local_availability,
            "changed_files": snapshot["changed_files"],
            "diff_sha256": f"sha256:{sha256_bytes(snapshot['diff'].encode('utf-8'))}",
        }
    )
    return finalize(manifest, material)


def write_json(path: Path, value: dict[str, Any]) -> None:
    candidate = Path(path).expanduser()
    if ".." in candidate.parts:
        raise CollectionError(f"output path traversal is not allowed: {path!s}")
    resolved = candidate.resolve()
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise CollectionError(f"output path must be within the worktree or temporary directory: {path!s}")
    path = resolved
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_arguments(argv: Iterable[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mode", required=True, choices=MODES)
    parser.add_argument("--repo", default=".", help="Git worktree to inspect")
    parser.add_argument("--base", help="Base revision for branch mode")
    parser.add_argument("--head", default="HEAD", help="Head revision for branch mode")
    parser.add_argument("--changes", choices=("staged", "unstaged", "all"), default="all")
    parser.add_argument("--pull-request-snapshot", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args(list(argv))


def main(argv: Iterable[str] | None = None) -> int:
    arguments = parse_arguments(sys.argv[1:] if argv is None else argv)
    repository = Path(arguments.repo).expanduser().resolve()
    manifest = base_manifest(repository, arguments.mode)
    try:
        repository = resolve_repository(arguments.repo)
        if arguments.mode == "codebase":
            manifest = collect_codebase(repository, arguments.head)
        elif arguments.mode == "branch":
            if not arguments.base:
                raise CollectionError("branch mode requires --base")
            manifest = collect_branch(repository, arguments.base, arguments.head)
        elif arguments.mode == "worktree":
            manifest = collect_worktree(repository, arguments.changes)
        else:
            if not arguments.pull_request_snapshot:
                raise CollectionError("pull-request mode requires --pull-request-snapshot")
            manifest = validate_pr_snapshot(repository, arguments.pull_request_snapshot)
    except (CollectionError, OSError, ValueError) as error:
        manifest["error"] = str(error)
        manifest["snapshot_id"] = material_id({"mode": arguments.mode, "error": str(error)})
        write_json(arguments.output, manifest)
        print(f"BLOCKED: {error}", file=sys.stderr)
        return 2
    write_json(arguments.output, manifest)
    print(manifest["status"])
    return 0 if manifest["status"] in ("READY", "NO_CHANGES") else 2


if __name__ == "__main__":
    raise SystemExit(main())
