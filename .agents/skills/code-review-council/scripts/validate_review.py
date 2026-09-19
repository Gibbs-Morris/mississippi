#!/usr/bin/env python3
"""Validate and consolidate Code Review Council reviewer evidence."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import tempfile
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = "code-review-council/v1"
PERSONA_IDS = (
    "domain-purist",
    "distributed-systems-pessimist",
    "security-adversary",
    "boundary-architect",
    "reluctant-maintainer",
    "performance-accountant",
    "test-sceptic",
    "framework-consumer",
    "compiler-engineer",
    "on-call-engineer",
)
FINGERPRINT_PATTERN = re.compile(r"^sha256:[0-9a-f]{64}$")
SNAPSHOT_PATTERN = FINGERPRINT_PATTERN
SEVERITIES = ("P0", "P1", "P2", "P3")
REVIEWER_STATUSES = ("complete", "not_applicable", "failed")
DISPOSITIONS = ("validated", "duplicate", "rejected", "pre-existing", "out-of-scope", "unresolved")


class ValidationError(RuntimeError):
    """A review artifact violates the council contract."""


def load_json(path: Path) -> Any:
    path = safe_io_path(path, must_exist=True, label="input")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ValidationError(f"cannot read JSON {path}: {error}") from error


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    path = safe_io_path(path, must_exist=True, label="reviewer input")
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as error:
        raise ValidationError(f"cannot read reviewer JSONL {path}: {error}") from error
    records: list[dict[str, Any]] = []
    for line_number, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError as error:
            raise ValidationError(f"reviewer JSONL line {line_number} is invalid: {error}") from error
        if not isinstance(value, dict):
            raise ValidationError(f"reviewer JSONL line {line_number} is not an object")
        records.append(value)
    return records


def write_json(path: Path, value: dict[str, Any]) -> None:
    path = safe_io_path(path, must_exist=False, label="output")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def safe_io_path(value: str | Path, *, must_exist: bool, label: str) -> Path:
    candidate = Path(value).expanduser()
    if ".." in candidate.parts:
        raise ValidationError(f"{label} path traversal is not allowed: {value!s}")
    try:
        resolved = candidate.resolve(strict=must_exist)
    except OSError as error:
        raise ValidationError(f"cannot resolve {label} path: {value!s}") from error
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise ValidationError(f"{label} path must be within the worktree or temporary directory: {value!s}")
    if must_exist and not resolved.is_file():
        raise ValidationError(f"{label} path is not a regular file: {value!s}")
    return resolved


def sha256_json(value: Any) -> str:
    encoded = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def required_text(value: Any, field: str, errors: list[str]) -> None:
    if not isinstance(value, str) or not value.strip():
        errors.append(f"{field} must be a non-empty string")


def scope_paths(scope: dict[str, Any]) -> set[str]:
    paths: set[str] = set()
    for entry in scope.get("changed_files", []):
        if isinstance(entry, dict) and isinstance(entry.get("path"), str):
            paths.add(entry["path"].replace("\\", "/"))
        if isinstance(entry, dict) and isinstance(entry.get("old_path"), str):
            paths.add(entry["old_path"].replace("\\", "/"))
    if scope.get("mode") == "codebase":
        for entry in scope.get("files", []):
            if isinstance(entry, dict) and isinstance(entry.get("path"), str):
                paths.add(entry["path"].replace("\\", "/"))
    for stream_name in ("staged", "unstaged"):
        stream = scope.get("selected", {}).get(stream_name, {})
        for entry in stream.get("files", []):
            if isinstance(entry, dict) and isinstance(entry.get("path"), str):
                paths.add(entry["path"].replace("\\", "/"))
            if isinstance(entry, dict) and isinstance(entry.get("old_path"), str):
                paths.add(entry["old_path"].replace("\\", "/"))
    for entry in scope.get("selected", {}).get("untracked", []):
        if isinstance(entry, dict) and isinstance(entry.get("path"), str):
            paths.add(entry["path"].replace("\\", "/"))
    return paths


def validate_finding(
    finding: Any,
    reviewer_persona: str,
    snapshot_id: str,
    allowed_paths: set[str],
    mode: str,
    location: str,
) -> list[str]:
    errors: list[str] = []
    if not isinstance(finding, dict):
        return [f"{location} is not an object"]
    for field in (
        "fingerprint",
        "category",
        "path",
        "scenario",
        "trigger",
        "impact",
        "remediation",
        "uncertainty",
    ):
        required_text(finding.get(field), f"{location}.{field}", errors)
    fingerprint = finding.get("fingerprint")
    if not isinstance(fingerprint, str) or not FINGERPRINT_PATTERN.fullmatch(fingerprint):
        errors.append(f"{location}.fingerprint is not a sha256 fingerprint")
    if finding.get("snapshot_id") != snapshot_id:
        errors.append(f"{location}.snapshot_id does not match the scope snapshot")
    if finding.get("severity") not in SEVERITIES:
        errors.append(f"{location}.severity is invalid")
    if finding.get("change_relation") not in ("introduced", "worsened", "pre-existing", "out-of-scope", "unknown"):
        errors.append(f"{location}.change_relation is invalid")
    persona_ids = finding.get("persona_ids")
    if not isinstance(persona_ids, list) or not persona_ids or any(persona not in PERSONA_IDS for persona in persona_ids):
        errors.append(f"{location}.persona_ids must contain known persona IDs")
    elif reviewer_persona not in persona_ids:
        errors.append(f"{location}.persona_ids does not contain its reviewer persona")
    line = finding.get("line")
    if not isinstance(line, int) or isinstance(line, bool) or line < 1:
        errors.append(f"{location}.line must be a positive integer")
    evidence = finding.get("evidence")
    if not isinstance(evidence, list) or not evidence or any(not isinstance(item, str) or not item.strip() for item in evidence):
        errors.append(f"{location}.evidence must contain non-empty strings")
    path = finding.get("path")
    if isinstance(path, str):
        normalized = path.replace("\\", "/")
        if normalized.startswith("/") or ":" in normalized.split("/")[0] or ".." in normalized.split("/"):
            errors.append(f"{location}.path escapes the review root")
        elif mode != "codebase" and normalized not in allowed_paths:
            errors.append(f"{location}.path is not present in the immutable change scope")
        elif mode == "codebase" and allowed_paths and normalized not in allowed_paths:
            errors.append(f"{location}.path is not present in the codebase snapshot")
    return errors


def validate_scope(scope: Any) -> tuple[dict[str, Any], list[str]]:
    errors: list[str] = []
    if not isinstance(scope, dict):
        return {}, ["scope manifest is not an object"]
    if scope.get("schema_version") != SCHEMA_VERSION:
        errors.append("scope schema_version is invalid")
    snapshot_id = scope.get("snapshot_id")
    if not isinstance(snapshot_id, str) or not SNAPSHOT_PATTERN.fullmatch(snapshot_id):
        errors.append("scope snapshot_id is invalid")
    if scope.get("mode") not in ("codebase", "branch", "worktree", "pull-request"):
        errors.append("scope mode is invalid")
    if scope.get("status") not in ("READY", "NO_CHANGES", "BLOCKED"):
        errors.append("scope status is invalid")
    if not isinstance(scope.get("repository"), dict):
        errors.append("scope repository is missing")
    material = scope.get("snapshot_material")
    if not isinstance(material, dict):
        errors.append("scope snapshot_material is missing")
    elif isinstance(snapshot_id, str) and SNAPSHOT_PATTERN.fullmatch(snapshot_id):
        if f"sha256:{sha256_json(material)}" != snapshot_id:
            errors.append("scope snapshot_id does not match snapshot_material")
        mirrored = {
            "mode": "mode",
            "revision": "revision",
            "base": "base",
            "head": "head",
            "merge_base": "merge_base",
            "changed_files": "changed_files",
            "files": "files",
            "statuses": "statuses",
            "selected": "selected",
            "unresolved_index": "unresolved_index",
        }
        for material_key, manifest_key in mirrored.items():
            if material_key in material and manifest_key in scope and material[material_key] != scope[manifest_key]:
                errors.append(f"scope mirrored field differs from snapshot_material: {manifest_key}")
        if "snapshot" in material and scope.get("pull_request") != material["snapshot"]:
            errors.append("scope pull_request differs from snapshot_material")
    return scope, errors


def validate_reviewers(
    records: list[dict[str, Any]], scope: dict[str, Any], errors: list[str]
) -> tuple[dict[str, dict[str, Any]], list[dict[str, Any]]]:
    by_persona: dict[str, dict[str, Any]] = {}
    candidates: list[dict[str, Any]] = []
    snapshot_id = scope.get("snapshot_id")
    allowed_paths = scope_paths(scope)
    if scope.get("status") == "NO_CHANGES" and not records:
        return by_persona, candidates
    for index, reviewer in enumerate(records, start=1):
        location = f"reviewer[{index}]"
        persona = reviewer.get("persona_id")
        if persona not in PERSONA_IDS:
            errors.append(f"{location}.persona_id is unknown")
            continue
        if persona in by_persona:
            errors.append(f"duplicate reviewer persona: {persona}")
            continue
        by_persona[persona] = reviewer
        if reviewer.get("review_id") in (None, ""):
            errors.append(f"{location}.review_id is missing")
        if reviewer.get("snapshot_id") != snapshot_id:
            errors.append(f"{location}.snapshot_id does not match the scope snapshot")
        for field in ("requested_model", "effective_model", "requested_concurrency", "effective_concurrency", "completed_at_utc"):
            if field not in reviewer:
                errors.append(f"{location}.{field} is missing")
        if reviewer.get("requested_model") is not None and reviewer.get("effective_model") not in (None, reviewer.get("requested_model")):
            errors.append(f"{location} requested_model and effective_model differ")
        if reviewer.get("requested_concurrency") is not None and reviewer.get("effective_concurrency") not in (None, reviewer.get("requested_concurrency")):
            errors.append(f"{location} requested_concurrency and effective_concurrency differ")
        status = reviewer.get("status")
        if status not in REVIEWER_STATUSES:
            errors.append(f"{location}.status is invalid")
        if status in ("not_applicable", "failed"):
            required_text(reviewer.get("reason"), f"{location}.reason", errors)
        findings = reviewer.get("findings")
        if not isinstance(findings, list):
            errors.append(f"{location}.findings must be an array")
            continue
        if status == "failed" and findings:
            errors.append(f"{location} failed reviewer must not contain findings")
        if status == "not_applicable" and findings:
            errors.append(f"{location} not_applicable reviewer must not contain findings")
        for finding_index, finding in enumerate(findings, start=1):
            finding_errors = validate_finding(
                finding,
                persona,
                snapshot_id,
                allowed_paths,
                scope.get("mode", ""),
                f"{location}.findings[{finding_index}]",
            )
            errors.extend(finding_errors)
            if not finding_errors:
                candidates.append(finding)
    missing = [persona for persona in PERSONA_IDS if persona not in by_persona]
    errors.extend(f"missing reviewer persona: {persona}" for persona in missing)
    return by_persona, candidates


def consolidate(candidates: list[dict[str, Any]], errors: list[str]) -> dict[str, dict[str, Any]]:
    consolidated: dict[str, dict[str, Any]] = {}
    for candidate in candidates:
        fingerprint = candidate["fingerprint"]
        existing = consolidated.get(fingerprint)
        if existing is None:
            consolidated[fingerprint] = json.loads(json.dumps(candidate))
            continue
        stable_fields = ("category", "severity", "path", "line", "scenario", "trigger", "impact")
        if any(existing.get(field) != candidate.get(field) for field in stable_fields):
            errors.append(f"conflicting reviewer evidence for fingerprint {fingerprint}")
        personas = set(existing.get("persona_ids", [])) | set(candidate.get("persona_ids", []))
        existing["persona_ids"] = sorted(personas)
    return consolidated


def validate_dispositions(
    value: Any, candidates: dict[str, dict[str, Any]], snapshot_id: str, errors: list[str]
) -> dict[str, dict[str, Any]]:
    if not isinstance(value, dict) or not isinstance(value.get("dispositions"), list):
        errors.append("adjudication must contain a dispositions array")
        return {}
    result: dict[str, dict[str, Any]] = {}
    for index, entry in enumerate(value["dispositions"], start=1):
        location = f"disposition[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{location} is not an object")
            continue
        fingerprint = entry.get("fingerprint")
        if fingerprint not in candidates:
            errors.append(f"{location} references an unknown candidate fingerprint")
            continue
        if fingerprint in result:
            errors.append(f"duplicate disposition for {fingerprint}")
            continue
        if entry.get("snapshot_id") != snapshot_id:
            errors.append(f"{location}.snapshot_id does not match the scope snapshot")
        disposition = entry.get("disposition")
        if disposition not in DISPOSITIONS:
            errors.append(f"{location}.disposition is invalid")
        required_text(entry.get("rationale"), f"{location}.rationale", errors)
        if disposition == "duplicate":
            duplicate_of = entry.get("duplicate_of")
            if duplicate_of not in candidates or duplicate_of == fingerprint:
                errors.append(f"{location}.duplicate_of must name another candidate")
        result[fingerprint] = entry
    for fingerprint, entry in result.items():
        if entry.get("disposition") != "duplicate":
            continue
        seen: set[str] = set()
        current = fingerprint
        while result.get(current, {}).get("disposition") == "duplicate":
            if current in seen:
                errors.append(f"duplicate disposition cycle includes {fingerprint}")
                break
            seen.add(current)
            current = result[current].get("duplicate_of")
        if current not in result or result[current].get("disposition") == "duplicate":
            errors.append(f"duplicate disposition for {fingerprint} has no canonical target")
    missing = sorted(set(candidates) - set(result))
    errors.extend(f"missing disposition for {fingerprint}" for fingerprint in missing)
    return result


def status_for(
    scope: dict[str, Any],
    reviewers: dict[str, dict[str, Any]],
    candidates: dict[str, dict[str, Any]],
    dispositions: dict[str, dict[str, Any]],
    errors: list[str],
) -> str:
    if errors:
        return "INCOMPLETE"
    if scope.get("status") == "NO_CHANGES":
        return "NO_CHANGES"
    if scope.get("status") == "BLOCKED":
        return "INCOMPLETE"
    if any(reviewer.get("status") == "failed" for reviewer in reviewers.values()):
        return "INCOMPLETE"
    if any(entry.get("disposition") == "unresolved" for entry in dispositions.values()):
        return "INCOMPLETE"
    for fingerprint, entry in dispositions.items():
        candidate = candidates[fingerprint]
        if entry.get("disposition") == "validated" and candidate.get("severity") in ("P0", "P1"):
            return "BLOCKED"
    return "PASS"


def markdown_report(result: dict[str, Any]) -> str:
    lines = [
        "# Code Review Council",
        "",
        f"- Status: `{result['status']}`",
        f"- Snapshot: `{result['snapshot_id']}`",
        f"- Reviewers: `{len(result.get('reviewers', []))}/{len(PERSONA_IDS)}`",
        f"- Candidate findings: `{len(result.get('findings', []))}`",
        "",
        "## Findings",
        "",
    ]
    findings = result.get("findings", [])
    if not findings:
        lines.append("No findings were produced for the selected snapshot.")
    else:
        for finding in findings:
            lines.extend(
                [
                    f"### {finding.get('severity', '?')} {finding.get('fingerprint', '?')}",
                    "",
                    f"- Disposition: `{finding.get('disposition', 'unknown')}`",
                    f"- Evidence: `{finding.get('path', '?')}:{finding.get('line', '?')}`",
                    f"- Scenario: {finding.get('scenario', '')}",
                    f"- Impact: {finding.get('impact', '')}",
                    f"- Remediation: {finding.get('remediation', '')}",
                    "",
                ]
            )
    lines.extend(["## Review coverage", ""])
    for reviewer in result.get("reviewers", []):
        lines.append(f"- `{reviewer.get('persona_id', '?')}`: `{reviewer.get('status', '?')}`")
    lines.extend(["", "## Limitations", ""])
    limitations = result.get("errors", []) or ["No validation limitations recorded."]
    lines.extend(f"- {limitation}" for limitation in limitations)
    return "\n".join(lines) + "\n"


def build_result(scope: dict[str, Any], reviewers: list[dict[str, Any]], adjudication: Any) -> dict[str, Any]:
    errors: list[str] = []
    validated_scope, scope_errors = validate_scope(scope)
    errors.extend(scope_errors)
    reviewer_map, candidate_list = validate_reviewers(reviewers, validated_scope, errors)
    candidates = consolidate(candidate_list, errors)
    dispositions = validate_dispositions(adjudication, candidates, validated_scope.get("snapshot_id"), errors)
    status = status_for(validated_scope, reviewer_map, candidates, dispositions, errors)
    final_findings: list[dict[str, Any]] = []
    for fingerprint, candidate in sorted(candidates.items()):
        item = json.loads(json.dumps(candidate))
        disposition = dispositions.get(fingerprint)
        if disposition:
            item["disposition"] = disposition.get("disposition")
            item["disposition_rationale"] = disposition.get("rationale")
            if disposition.get("duplicate_of"):
                item["duplicate_of"] = disposition["duplicate_of"]
        final_findings.append(item)
    reviewer_statuses = Counter(reviewer.get("status") for reviewer in reviewer_map.values())
    models = sorted(
        {
            reviewer.get("effective_model")
            for reviewer in reviewer_map.values()
            if reviewer.get("effective_model")
        }
    )
    result = {
        "schema_version": SCHEMA_VERSION,
        "status": status,
        "snapshot_id": validated_scope.get("snapshot_id"),
        "scope_manifest": validated_scope,
        "reviewers": [reviewer_map[persona] for persona in PERSONA_IDS if persona in reviewer_map],
        "findings": final_findings,
        "dispositions": [dispositions[fingerprint] for fingerprint in sorted(dispositions)],
        "execution": {
            "reviewer_status_counts": dict(sorted(reviewer_statuses.items())),
            "effective_models": models,
            "reviewer_count": len(reviewer_map),
            "required_reviewer_count": len(PERSONA_IDS),
        },
        "publication": {"status": "not-requested"},
        "errors": errors,
    }
    return result


def parse_arguments(argv: Iterable[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scope", required=True, type=Path)
    parser.add_argument("--reviewers", required=True, type=Path)
    parser.add_argument("--adjudication", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--markdown-output", type=Path)
    return parser.parse_args(list(argv))


def main(argv: Iterable[str] | None = None) -> int:
    arguments = parse_arguments(sys.argv[1:] if argv is None else argv)
    try:
        scope = load_json(arguments.scope)
        reviewers = load_jsonl(arguments.reviewers)
        adjudication = load_json(arguments.adjudication)
        result = build_result(scope, reviewers, adjudication)
    except ValidationError as error:
        error_snapshot = f"sha256:{hashlib.sha256(str(error).encode('utf-8')).hexdigest()}"
        result = {
            "schema_version": SCHEMA_VERSION,
            "status": "INCOMPLETE",
            "snapshot_id": error_snapshot,
            "reviewers": [],
            "findings": [],
            "dispositions": [],
            "execution": {"reviewer_count": 0, "required_reviewer_count": len(PERSONA_IDS)},
            "publication": {"status": "not-requested"},
            "errors": [str(error)],
        }
    write_json(arguments.output, result)
    if arguments.markdown_output:
        markdown_path = safe_io_path(arguments.markdown_output, must_exist=False, label="Markdown output")
        markdown_path.parent.mkdir(parents=True, exist_ok=True)
        markdown_path.write_text(markdown_report(result), encoding="utf-8")
    print(result["status"])
    return 0 if result["status"] in ("PASS", "NO_CHANGES") else 2


if __name__ == "__main__":
    raise SystemExit(main())
