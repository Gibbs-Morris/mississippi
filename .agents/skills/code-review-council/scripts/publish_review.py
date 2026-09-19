#!/usr/bin/env python3
"""Prepare or publish one idempotent Code Review Council PR comment."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = "code-review-council/v1"
SHA_PATTERN = re.compile(r"^sha256:[0-9a-f]{64}$")
REPO_PATTERN = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")


class PublicationError(RuntimeError):
    """A publication precondition or provider operation failed."""


def load_json(path: Path) -> Any:
    path = safe_io_path(path, must_exist=True, label="input")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise PublicationError(f"cannot read JSON {path}: {error}") from error


def write_json(path: Path, value: dict[str, Any]) -> None:
    candidate = Path(path).expanduser()
    if ".." in candidate.parts:
        raise PublicationError(f"output path traversal is not allowed: {path!s}")
    resolved = candidate.resolve()
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise PublicationError(f"output path must be within the worktree or temporary directory: {path!s}")
    path = resolved
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def safe_io_path(value: str | Path, *, must_exist: bool, label: str) -> Path:
    candidate = Path(value).expanduser()
    if ".." in candidate.parts:
        raise PublicationError(f"{label} path traversal is not allowed: {value!s}")
    try:
        resolved = candidate.resolve(strict=must_exist)
    except OSError as error:
        raise PublicationError(f"cannot resolve {label} path: {value!s}") from error
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise PublicationError(f"{label} path must be within the worktree or temporary directory: {value!s}")
    if must_exist and not resolved.is_file():
        raise PublicationError(f"{label} path is not a regular file: {value!s}")
    return resolved


def markdown_inline(value: Any) -> str:
    text = str(value)
    text = text.replace("\\", "\\\\").replace("\r", "").replace("\n", " ")
    for character in ("`", "*", "_", "[", "]", "<", ">", "@"):
        text = text.replace(character, f"\\{character}")
    return text


def stable_key(review: dict[str, Any]) -> str:
    material = {
        "schema_version": SCHEMA_VERSION,
        "snapshot_id": review.get("snapshot_id"),
        "status": review.get("status"),
        "findings": sorted(
            (finding.get("fingerprint"), finding.get("disposition"))
            for finding in review.get("findings", [])
            if isinstance(finding, dict)
        ),
    }
    encoded = json.dumps(material, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def marker(key: str) -> str:
    return f"<!-- code-review-council:v1:{key} -->"


def markdown_body(review: dict[str, Any], key: str, markdown_path: Path | None) -> str:
    if markdown_path:
        markdown_path = safe_io_path(markdown_path, must_exist=True, label="Markdown input")
        try:
            content = markdown_path.read_text(encoding="utf-8").strip()
        except OSError as error:
            raise PublicationError(f"cannot read Markdown review {markdown_path}: {error}") from error
    else:
        lines = [
            "## Code Review Council",
            "",
            f"Status: **{review.get('status', 'UNKNOWN')}**",
            f"Snapshot: `{review.get('snapshot_id', 'unknown')}`",
            "",
        ]
        findings = review.get("findings", [])
        if findings:
            lines.append("### Findings")
            lines.append("")
            for finding in findings:
                lines.extend(
                    [
                        f"- **{markdown_inline(finding.get('severity', '?'))}** `{markdown_inline(finding.get('path', '?'))}:{finding.get('line', '?')}` - {markdown_inline(finding.get('scenario', ''))}",
                        f"  - Disposition: `{markdown_inline(finding.get('disposition', 'unknown'))}`",
                    ]
                )
        else:
            lines.append("No findings were produced for this snapshot.")
        content = "\n".join(lines)
    body = f"{marker(key)}\n\n{content}\n"
    if len(body.encode("utf-8")) > 60000:
        raise PublicationError("publication body exceeds the safe 60,000-byte limit")
    return body


def validate_review(review: Any) -> tuple[dict[str, Any], str, str]:
    if not isinstance(review, dict):
        raise PublicationError("review result must be a JSON object")
    if review.get("schema_version") != SCHEMA_VERSION:
        raise PublicationError("review schema_version is invalid")
    if review.get("status") not in ("PASS", "BLOCKED"):
        raise PublicationError("only final PASS or BLOCKED results can be published")
    snapshot_id = review.get("snapshot_id")
    if not isinstance(snapshot_id, str) or not SHA_PATTERN.fullmatch(snapshot_id):
        raise PublicationError("review snapshot_id is invalid")
    if review.get("errors"):
        raise PublicationError("review contains unresolved validation errors")
    scope = review.get("scope_manifest")
    if not isinstance(scope, dict):
        raise PublicationError("review scope_manifest is required")
    if scope.get("snapshot_id") != snapshot_id:
        raise PublicationError("review snapshot_id does not match scope_manifest.snapshot_id")
    key = stable_key(review)
    return review, key, markdown_body(review, key, None)


def load_ledger(path: Path) -> dict[str, Any]:
    path = safe_io_path(path, must_exist=False, label="ledger")
    if not path.exists():
        return {"schema_version": SCHEMA_VERSION, "published": []}
    value = load_json(path)
    if not isinstance(value, dict) or not isinstance(value.get("published"), list):
        raise PublicationError("publication ledger must contain a published array")
    return value


def publish_mock(review: dict[str, Any], key: str, body: str, ledger_path: Path) -> dict[str, Any]:
    ledger = load_ledger(ledger_path)
    marker_text = marker(key)
    for entry in ledger["published"]:
        if isinstance(entry, dict) and entry.get("marker") == marker_text:
            return {"status": "already-published", "provider": "mock", "idempotency_key": key}
    ledger["published"].append(
        {
            "marker": marker_text,
            "idempotency_key": key,
            "snapshot_id": review["snapshot_id"],
            "status": review["status"],
            "body": body,
        }
    )
    write_json(ledger_path, ledger)
    return {"status": "published", "provider": "mock", "idempotency_key": key}


def gh_api(endpoint: str, *arguments: str, input_value: dict[str, Any] | None = None) -> Any:
    command = ["gh", "api", endpoint, *arguments]
    try:
        completed = subprocess.run(
            command,
            input=None if input_value is None else json.dumps(input_value),
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
    except OSError as error:
        raise PublicationError(f"cannot execute gh: {error}") from error
    if completed.returncode != 0:
        detail = completed.stderr.strip()
        raise PublicationError(f"gh api failed ({completed.returncode}): {detail}")
    try:
        return json.loads(completed.stdout)
    except json.JSONDecodeError as error:
        raise PublicationError(f"gh api returned invalid JSON: {error}") from error


def flatten_pages(value: Any) -> list[Any]:
    if not isinstance(value, list):
        return []
    flattened: list[Any] = []
    for page in value:
        if isinstance(page, list):
            flattened.extend(page)
        else:
            flattened.append(page)
    return flattened


def patch_ranges(patch: str) -> list[tuple[int, int]]:
    ranges: list[tuple[int, int]] = []
    for line in patch.splitlines():
        match = re.match(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", line)
        if not match:
            continue
        old_start, old_count, new_start, new_count = match.groups()
        ranges.extend(
            (
                (int(old_start), int(old_count or "1")),
                (int(new_start), int(new_count or "1")),
            )
        )
    return ranges


def validate_live_anchors(review: dict[str, Any], files: list[dict[str, Any]]) -> None:
    by_path: dict[str, dict[str, Any]] = {}
    for file in files:
        if not isinstance(file, dict):
            continue
        if isinstance(file.get("filename"), str):
            by_path[file["filename"]] = file
        if isinstance(file.get("previous_filename"), str):
            by_path[file["previous_filename"]] = file
    for finding in review.get("findings", []):
        path = finding.get("path") if isinstance(finding, dict) else None
        line = finding.get("line") if isinstance(finding, dict) else None
        file = by_path.get(path)
        if file is None:
            raise PublicationError(f"finding anchor is absent from the live PR diff: {path!r}")
        patch = file.get("patch")
        if not isinstance(patch, str):
            raise PublicationError(f"live PR diff has no patch for anchored path: {path!r}")
        if not isinstance(line, int) or not any(start <= line < start + count for start, count in patch_ranges(patch)):
            raise PublicationError(f"finding line is absent from the live PR hunks: {path}:{line}")


def pr_scope(review: dict[str, Any]) -> tuple[str, str, set[str]]:
    scope = review["scope_manifest"]
    base = scope.get("base")
    head = scope.get("head")
    if not isinstance(base, str) or not re.fullmatch(r"[0-9a-f]{40}", base):
        raise PublicationError("scope manifest does not contain a full base SHA")
    if not isinstance(head, str) or not re.fullmatch(r"[0-9a-f]{40}", head):
        raise PublicationError("scope manifest does not contain a full head SHA")
    paths = {
        path
        for entry in scope.get("changed_files", [])
        if isinstance(entry, dict)
        for path in (entry.get("path"), entry.get("old_path"))
        if isinstance(path, str)
    }
    return base, head, paths


def validate_anchors(review: dict[str, Any], paths: set[str]) -> None:
    for finding in review.get("findings", []):
        if not isinstance(finding, dict):
            raise PublicationError("review contains a non-object finding")
        path = finding.get("path")
        if not isinstance(path, str) or path not in paths:
            raise PublicationError(f"finding anchor is not present in the reviewed PR diff: {path!r}")
        if not isinstance(finding.get("line"), int) or finding["line"] < 1:
            raise PublicationError(f"finding anchor line is invalid for {path!r}")


def publish_github(
    review: dict[str, Any],
    key: str,
    body: str,
    repo: str,
    number: int,
    expected_base: str | None,
    expected_head: str | None,
    execute: bool,
) -> dict[str, Any]:
    if not REPO_PATTERN.fullmatch(repo):
        raise PublicationError("--repo must be an owner/name GitHub repository")
    scope = review["scope_manifest"]
    pull_request = scope.get("pull_request")
    if not isinstance(pull_request, dict) or pull_request.get("number") != number:
        raise PublicationError("target pull request does not match the reviewed PR snapshot")
    snapshot_repository = pull_request.get("repository")
    if isinstance(snapshot_repository, dict):
        snapshot_repository = snapshot_repository.get("full_name")
    if snapshot_repository and snapshot_repository != repo:
        raise PublicationError("target repository does not match the reviewed PR snapshot")
    scope_base, scope_head, paths = pr_scope(review)
    if expected_base and expected_base != scope_base:
        raise PublicationError("--expected-base does not match the reviewed scope")
    if expected_head and expected_head != scope_head:
        raise PublicationError("--expected-head does not match the reviewed scope")
    validate_anchors(review, paths)
    endpoint = f"repos/{repo}/pulls/{number}"
    if not execute:
        return {
            "status": "dry-run",
            "provider": "github",
            "idempotency_key": key,
            "requires_revalidation": True,
            "expected_base": scope_base,
            "expected_head": scope_head,
        }
    live_pr = gh_api(endpoint)
    if live_pr.get("state") != "open":
        raise PublicationError("target pull request is not open")
    live_base = live_pr.get("base", {}).get("sha")
    live_head = live_pr.get("head", {}).get("sha")
    if live_base != scope_base or live_head != scope_head:
        raise PublicationError("pull-request base or head changed since the reviewed snapshot")
    live_files = flatten_pages(gh_api(f"repos/{repo}/pulls/{number}/files", "--paginate", "--slurp"))
    validate_live_anchors(review, [file for file in live_files if isinstance(file, dict)])
    publisher = gh_api("user").get("login")
    comments = gh_api(f"repos/{repo}/issues/{number}/comments", "--paginate", "--slurp")
    flattened = flatten_pages(comments)
    if any(
        marker(key) in str(comment.get("body", "")) and comment.get("user", {}).get("login") == publisher
        for comment in flattened
        if isinstance(comment, dict)
    ):
        return {"status": "already-published", "provider": "github", "idempotency_key": key}
    gh_api(
        f"repos/{repo}/issues/{number}/comments",
        "--method",
        "POST",
        "--input",
        "-",
        input_value={"body": body},
    )
    return {"status": "published", "provider": "github", "idempotency_key": key}


def parse_arguments(argv: Iterable[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--review", required=True, type=Path)
    parser.add_argument("--provider", choices=("mock", "github"), default="mock")
    parser.add_argument("--ledger", type=Path)
    parser.add_argument("--repo")
    parser.add_argument("--pr", type=int)
    parser.add_argument("--expected-base")
    parser.add_argument("--expected-head")
    parser.add_argument("--markdown", type=Path)
    parser.add_argument("--execute", action="store_true", help="perform the explicitly requested GitHub write")
    parser.add_argument("--output", type=Path)
    return parser.parse_args(list(argv))


def main(argv: Iterable[str] | None = None) -> int:
    arguments = parse_arguments(sys.argv[1:] if argv is None else argv)
    try:
        review = load_json(arguments.review)
        review, key, generated_body = validate_review(review)
        body = markdown_body(review, key, arguments.markdown)
        if arguments.provider == "mock":
            if arguments.execute:
                raise PublicationError("--execute is only meaningful for the guarded GitHub provider")
            if not arguments.ledger:
                raise PublicationError("mock provider requires --ledger")
            result = publish_mock(review, key, body, arguments.ledger)
        else:
            if arguments.pr is None or arguments.pr < 1 or not arguments.repo:
                raise PublicationError("GitHub provider requires --repo and a positive --pr")
            result = publish_github(
                review,
                key,
                body,
                arguments.repo,
                arguments.pr,
                arguments.expected_base,
                arguments.expected_head,
                arguments.execute,
            )
        result["body_sha256"] = hashlib.sha256(body.encode("utf-8")).hexdigest()
        result["body_preview"] = generated_body[:200] if not arguments.markdown else "loaded-from-markdown"
    except PublicationError as error:
        result = {"status": "blocked", "error": str(error)}
    if arguments.output:
        write_json(arguments.output, result)
    print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    return 0 if result.get("status") in ("published", "already-published", "dry-run") else 2


if __name__ == "__main__":
    raise SystemExit(main())
