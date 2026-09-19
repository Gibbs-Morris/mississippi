#!/usr/bin/env python3
"""Run the offline three-approach Code Review Council evaluation."""

from __future__ import annotations

import argparse
import json
import sys
import tempfile
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = "code-review-council/v1"
APPROACHES = ("single-reviewer", "all-lenses", "council")
REQUIRED_CASES = {
    "unchanged-codebase-defect",
    "branch-advanced-base",
    "stacked-pull-request",
    "staged-only",
    "unstaged-only",
    "untracked-only",
    "staged-hidden-by-unstaged",
    "rename-delete",
    "no-change",
    "unresolved-index",
    "conditional-distributed-system",
    "protected-non-defect",
    "duplicate-findings",
    "distinct-same-location",
    "stale-pr-comments",
    "partial-publication",
    "revision-change",
    "truncated-diff",
    "reviewer-failure",
    "unsupported-model",
    "unavailable-parallelism",
    "malicious-instructions",
    "clean-zero-findings",
    "adjacent-unrelated-request",
}


class EvaluationError(RuntimeError):
    """The evaluation fixture is incomplete or malformed."""


def load_fixture(path: Path) -> dict[str, Any]:
    path = safe_io_path(path, must_exist=True, label="fixture")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise EvaluationError(f"cannot read fixture: {error}") from error
    if not isinstance(value, dict) or value.get("schema_version") != SCHEMA_VERSION:
        raise EvaluationError("fixture schema_version is invalid")
    cases = value.get("cases")
    if not isinstance(cases, list) or not cases:
        raise EvaluationError("fixture must contain a non-empty cases array")
    case_ids = {case.get("id") for case in cases if isinstance(case, dict)}
    missing = sorted(REQUIRED_CASES - case_ids)
    if missing:
        raise EvaluationError(f"fixture is missing required cases: {', '.join(missing)}")
    sets = {case.get("set") for case in cases if isinstance(case, dict)}
    if not {"development", "held-out"}.issubset(sets):
        raise EvaluationError("fixture must contain development and held-out cases")
    return value


def finding_map(approach: dict[str, Any]) -> list[dict[str, Any]]:
    findings = approach.get("findings")
    if not isinstance(findings, list):
        raise EvaluationError("approach findings must be an array")
    result = []
    for finding in findings:
        if not isinstance(finding, dict) or not isinstance(finding.get("fingerprint"), str):
            raise EvaluationError("approach finding requires a fingerprint")
        result.append(finding)
    return result


def approach_for_case(fixture: dict[str, Any], case: dict[str, Any], approach_name: str) -> dict[str, Any]:
    defaults = fixture.get("defaults", {}).get(approach_name, {})
    overrides = case.get("approaches", {}).get(approach_name, {})
    if not isinstance(defaults, dict) or not isinstance(overrides, dict):
        raise EvaluationError(f"invalid defaults or overrides for {approach_name}")
    merged = json.loads(json.dumps(defaults))
    merged.update(overrides)
    if isinstance(defaults.get("publication"), dict) and isinstance(overrides.get("publication"), dict):
        merged["publication"] = {**defaults["publication"], **overrides["publication"]}
    return merged


def metric_summary(fixture: dict[str, Any], cases: list[dict[str, Any]], approach_name: str) -> dict[str, Any]:
    counts = defaultdict(float)
    totals = defaultdict(float)
    unique_baseline_findings: set[str] = set()
    unique_council_findings: set[str] = set()
    for case in cases:
        if not isinstance(case, dict):
            raise EvaluationError("case is not an object")
        if not isinstance(case.get("truth"), list) or any(not isinstance(item, str) for item in case["truth"]):
            raise EvaluationError(f"case {case.get('id')} truth must be a list of finding IDs")
        high = case.get("high_severity_truth", [])
        if not isinstance(high, list) or any(item not in case["truth"] for item in high):
            raise EvaluationError(f"case {case.get('id')} high_severity_truth must be a subset of truth")
        if not isinstance(case.get("trigger_expected", True), bool):
            raise EvaluationError(f"case {case.get('id')} trigger_expected must be boolean")
        approaches = case.get("approaches", {})
        if approach_name not in approaches or not isinstance(approaches[approach_name], dict):
            raise EvaluationError(f"case {case.get('id')} has no {approach_name} approach")
        approach = approach_for_case(fixture, case, approach_name)
        findings = finding_map(approach)
        truth = set(case.get("truth", []))
        high_severity = set(case.get("high_severity_truth", []))
        predicted = {finding["fingerprint"] for finding in findings}
        true_predictions = predicted & truth
        true_high = predicted & high_severity
        counts["predicted"] += len(predicted)
        counts["true_predictions"] += len(true_predictions)
        counts["truth"] += len(truth)
        counts["high_truth"] += len(high_severity)
        counts["true_high"] += len(true_high)
        counts["invalid_anchors"] += sum(not finding.get("valid_anchor", False) for finding in findings)
        counts["anchored_findings"] += len(findings)
        if not truth and (approach.get("result_status") == "BLOCKED" or any(finding.get("blocked") for finding in findings)):
            counts["false_blockers"] += 1
        totals["clean_cases"] += int(not truth)
        required_scope = approach.get("scope_elements_required", case.get("scope_elements_required", 1))
        present_scope = approach.get("scope_elements_present", case.get("scope_elements_present", required_scope if approach.get("scope_complete") else 0))
        if not isinstance(required_scope, int) or required_scope < 1 or not isinstance(present_scope, int) or present_scope < 0 or present_scope > required_scope:
            raise EvaluationError(f"case {case.get('id')} has invalid scope completeness counts")
        totals["scope_elements_required"] += required_scope
        counts["scope_elements_present"] += present_scope
        totals["policy_cases"] += 1
        counts["policy_matches"] += int(bool(approach.get("policy_compliant")))
        totals["trigger_cases"] += 1
        counts["trigger_matches"] += int(approach.get("triggered") == case.get("trigger_expected", True))
        publication = approach.get("publication", {})
        totals["publication_attempts"] += publication.get("attempts", 0)
        counts["duplicate_publications"] += publication.get("duplicate_attempts", 0)
        totals["latency_ms"] += approach.get("latency_ms", 0)
        totals["tokens"] += approach.get("tokens", 0)
        if approach_name == "council":
            unique_council_findings |= {finding["fingerprint"] for finding in findings if finding.get("validated")}
            for baseline in ("single-reviewer", "all-lenses"):
                baseline_approach = approach_for_case(fixture, case, baseline)
                unique_baseline_findings |= {finding["fingerprint"] for finding in finding_map(baseline_approach)}
        else:
            unique_baseline_findings |= predicted
    if approach_name == "council":
        counts["unique_validated_contribution"] = len(unique_council_findings - unique_baseline_findings)
    else:
        counts["unique_validated_contribution"] = 0
    predicted = counts["predicted"]
    truth = counts["truth"]
    high_truth = counts["high_truth"]
    return {
        "cases": len(cases),
        "precision": round(counts["true_predictions"] / predicted, 4) if predicted else 1.0,
        "recall": round(counts["true_predictions"] / truth, 4) if truth else 1.0,
        "p0_p1_recall": round(counts["true_high"] / high_truth, 4) if high_truth else 1.0,
        "false_blocker_rate": round(counts["false_blockers"] / totals["clean_cases"], 4)
        if totals["clean_cases"]
        else 0.0,
        "duplicate_publication_rate": round(counts["duplicate_publications"] / totals["publication_attempts"], 4)
        if totals["publication_attempts"]
        else 0.0,
        "invalid_anchor_rate": round(counts["invalid_anchors"] / counts["anchored_findings"], 4)
        if counts["anchored_findings"]
        else 0.0,
        "scope_completeness": round(counts["scope_elements_present"] / totals["scope_elements_required"], 4),
        "policy_compliance": round(counts["policy_matches"] / totals["policy_cases"], 4),
        "skill_trigger_accuracy": round(counts["trigger_matches"] / totals["trigger_cases"], 4),
        "latency_ms_total": int(totals["latency_ms"]),
        "tokens_total": int(totals["tokens"]),
        "unique_validated_contribution": int(counts["unique_validated_contribution"]),
    }


def markdown_report(result: dict[str, Any]) -> str:
    lines = [
        "# Code Review Council evaluation",
        "",
        "This is an offline deterministic fixture evaluation. It does not measure "
        "live model precision, recall, or provider latency.",
        "",
        f"- Cases: `{result['case_count']}`",
        f"- Development cases: `{result['set_counts'].get('development', 0)}`",
        f"- Held-out cases: `{result['set_counts'].get('held-out', 0)}`",
        "",
        "## Overall results",
        "",
        "| Approach | Precision | Recall | P0/P1 recall | False blockers | Policy |",
        "| --- | ---: | ---: | ---: | ---: | ---: |",
    ]
    for approach, metrics in result["overall"].items():
        lines.append(
            f"| `{approach}` | {metrics['precision']:.4f} | {metrics['recall']:.4f} | "
            f"{metrics['p0_p1_recall']:.4f} | {metrics['false_blocker_rate']:.4f} | "
            f"{metrics['policy_compliance']:.4f} |"
        )
    lines.extend(["", "## Limitations", "", "- No live Codex reviewer or GitHub publication was executed by this offline runner.", "- Fixture latency and token values are simulated observations for comparison only.", "- A live promotion decision requires real traces, host capability metadata, and current CI/review evidence.", ""])
    return "\n".join(lines)


def evaluate(fixture: dict[str, Any]) -> dict[str, Any]:
    cases = fixture["cases"]
    for case in cases:
        if not isinstance(case, dict) or not isinstance(case.get("approaches"), dict):
            raise EvaluationError("every case must contain approaches")
        for approach in APPROACHES:
            if approach not in case["approaches"]:
                raise EvaluationError(f"case {case.get('id')} is missing approach {approach}")
    set_counts = defaultdict(int)
    for case in cases:
        set_counts[case.get("set", "unknown")] += 1
    result = {
        "schema_version": SCHEMA_VERSION,
        "evaluation": "offline-deterministic",
        "case_count": len(cases),
        "set_counts": dict(sorted(set_counts.items())),
        "overall": {approach: metric_summary(fixture, cases, approach) for approach in APPROACHES},
        "by_set": {
            case_set: {
                approach: metric_summary(fixture, [case for case in cases if case.get("set") == case_set], approach)
                for approach in APPROACHES
            }
            for case_set in sorted(set_counts)
        },
        "required_cases": sorted(REQUIRED_CASES),
        "limitations": [
            "No live model execution was performed by this runner.",
            "No live GitHub publication was performed by this runner.",
            "Latency and token values are fixture observations, not production measurements.",
        ],
    }
    return result


def parse_arguments(argv: Iterable[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fixtures", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--markdown-output", type=Path)
    return parser.parse_args(list(argv))


def safe_io_path(value: str | Path, *, must_exist: bool, label: str) -> Path:
    candidate = Path(value).expanduser()
    if ".." in candidate.parts:
        raise EvaluationError(f"{label} path traversal is not allowed: {value!s}")
    try:
        resolved = candidate.resolve(strict=must_exist)
    except OSError as error:
        raise EvaluationError(f"cannot resolve {label} path: {value!s}") from error
    allowed_roots = (Path.cwd().resolve(), Path(tempfile.gettempdir()).resolve())
    if not any(resolved == root or root in resolved.parents for root in allowed_roots):
        raise EvaluationError(f"{label} path must be within the worktree or temporary directory: {value!s}")
    if must_exist and not resolved.is_file():
        raise EvaluationError(f"{label} path is not a regular file: {value!s}")
    return resolved


def main(argv: Iterable[str] | None = None) -> int:
    arguments = parse_arguments(sys.argv[1:] if argv is None else argv)
    try:
        result = evaluate(load_fixture(arguments.fixtures))
    except EvaluationError as error:
        print(f"INCOMPLETE: {error}", file=sys.stderr)
        return 2
    output_path = safe_io_path(arguments.output, must_exist=False, label="output")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if arguments.markdown_output:
        markdown_path = safe_io_path(arguments.markdown_output, must_exist=False, label="Markdown output")
        markdown_path.parent.mkdir(parents=True, exist_ok=True)
        markdown_path.write_text(markdown_report(result), encoding="utf-8")
    print(json.dumps(result["overall"], ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
