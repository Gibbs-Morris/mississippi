#!/usr/bin/env python3
"""Deterministic standard-library tests for Code Review Council helpers."""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

import collect_scope  # noqa: E402
import publish_review  # noqa: E402
import run_evaluation  # noqa: E402
import validate_review  # noqa: E402


PERSONAS = validate_review.PERSONA_IDS
SNAPSHOT_ID = "sha256:" + ("0" * 64)
FINGERPRINT = "sha256:" + ("1" * 64)


def git(repo: Path, *arguments: str) -> None:
    command = ["git", "-c", f"safe.directory={repo}", *arguments]
    completed = subprocess.run(command, cwd=repo, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if completed.returncode:
        raise AssertionError(completed.stderr.decode("utf-8", errors="replace"))


class CodeReviewCouncilTests(unittest.TestCase):
    def test_worktree_snapshot_keeps_staged_and_unstaged_evidence_separate(self) -> None:
        with tempfile.TemporaryDirectory(prefix="code-review-council-") as directory:
            repo = Path(directory)
            git(repo, "init", "--quiet")
            git(repo, "config", "user.name", "Council Test")
            git(repo, "config", "user.email", "council@example.invalid")
            tracked = repo / "tracked.txt"
            tracked.write_text("base\n", encoding="utf-8")
            git(repo, "add", "tracked.txt")
            git(repo, "commit", "--quiet", "-m", "base")
            tracked.write_text("staged defect\n", encoding="utf-8")
            git(repo, "add", "tracked.txt")
            tracked.write_text("unstaged correction\n", encoding="utf-8")
            (repo / "untracked.txt").write_text("new\n", encoding="utf-8")
            output = repo / "scope.json"
            exit_code = collect_scope.main(
                ["--mode", "worktree", "--repo", str(repo), "--changes", "all", "--output", str(output)]
            )
            self.assertEqual(exit_code, 0)
            scope = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(scope["status"], "READY")
            self.assertEqual(len(scope["selected"]["staged"]["files"]), 1)
            self.assertEqual(len(scope["selected"]["unstaged"]["files"]), 1)
            self.assertEqual(scope["selected"]["untracked"][0]["path"], "untracked.txt")
            self.assertNotEqual(
                scope["selected"]["staged"]["patch"]["sha256"],
                scope["selected"]["unstaged"]["patch"]["sha256"],
            )

    def test_committed_and_pull_request_modes_pin_revisions(self) -> None:
        with tempfile.TemporaryDirectory(prefix="code-review-council-") as directory:
            repo = Path(directory)
            git(repo, "init", "--quiet")
            git(repo, "config", "user.name", "Council Test")
            git(repo, "config", "user.email", "council@example.invalid")
            source = repo / "example.txt"
            source.write_text("base\n", encoding="utf-8")
            git(repo, "add", "example.txt")
            git(repo, "commit", "--quiet", "-m", "base")
            base = subprocess.check_output(
                ["git", "-c", f"safe.directory={repo}", "rev-parse", "HEAD"], cwd=repo, text=True
            ).strip()
            source.write_text("changed\n", encoding="utf-8")
            git(repo, "add", "example.txt")
            git(repo, "commit", "--quiet", "-m", "change")
            head = subprocess.check_output(
                ["git", "-c", f"safe.directory={repo}", "rev-parse", "HEAD"], cwd=repo, text=True
            ).strip()
            codebase_output = repo / "codebase.json"
            branch_output = repo / "branch.json"
            self.assertEqual(
                collect_scope.main(
                    ["--mode", "codebase", "--repo", str(repo), "--head", head, "--output", str(codebase_output)]
                ),
                0,
            )
            self.assertEqual(
                collect_scope.main(
                    [
                        "--mode",
                        "branch",
                        "--repo",
                        str(repo),
                        "--base",
                        base,
                        "--head",
                        head,
                        "--output",
                        str(branch_output),
                    ]
                ),
                0,
            )
            branch = json.loads(branch_output.read_text(encoding="utf-8"))
            self.assertEqual(branch["status"], "READY")
            self.assertEqual(branch["base"], base)
            self.assertEqual(branch["head"], head)
            pr_snapshot = {
                "repository": "example/repository",
                "number": 1,
                "base_sha": base,
                "head_sha": head,
                "changed_files": [{"status": "M", "path": "example.txt"}],
                "diff": "diff --git a/example.txt b/example.txt\n",
                "discussion": [],
                "checks": [],
            }
            pr_input = repo / "pr.json"
            pr_output = repo / "pr-scope.json"
            pr_input.write_text(json.dumps(pr_snapshot), encoding="utf-8")
            self.assertEqual(
                collect_scope.main(
                    [
                        "--mode",
                        "pull-request",
                        "--repo",
                        str(repo),
                        "--pull-request-snapshot",
                        str(pr_input),
                        "--output",
                        str(pr_output),
                    ]
                ),
                0,
            )
            self.assertEqual(json.loads(pr_output.read_text(encoding="utf-8"))["status"], "READY")

    def test_validator_requires_all_reviewers_and_adjudication(self) -> None:
        with tempfile.TemporaryDirectory(prefix="code-review-council-") as directory:
            root = Path(directory)
            scope_path = root / "scope.json"
            reviewers_path = root / "reviewers.jsonl"
            adjudication_path = root / "adjudication.json"
            output_path = root / "review.json"
            scope = {
                "schema_version": validate_review.SCHEMA_VERSION,
                "mode": "branch",
                "status": "READY",
                "snapshot_id": SNAPSHOT_ID,
                "repository": {"root": str(root)},
                "captured_at_utc": "2026-09-19T00:00:00Z",
                "changed_files": [{"status": "M", "path": "src/example.cs"}],
            }
            finding = {
                "fingerprint": FINGERPRINT,
                "persona_ids": ["domain-purist"],
                "category": "correctness",
                "severity": "P2",
                "snapshot_id": SNAPSHOT_ID,
                "path": "src/example.cs",
                "symbol": "Example.Handle",
                "line": 1,
                "scenario": "A request reaches the changed path.",
                "trigger": "When the input is repeated.",
                "impact": "The result diverges.",
                "evidence": ["The changed branch has no guard."],
                "remediation": "Make the transition idempotent.",
                "uncertainty": "Low.",
                "change_relation": "introduced",
            }
            reviewers = []
            for persona in PERSONAS:
                reviewers.append(
                    {
                        "review_id": f"review-{persona}",
                        "persona_id": persona,
                        "snapshot_id": SNAPSHOT_ID,
                        "status": "complete",
                        "findings": [finding] if persona == "domain-purist" else [],
                    }
                )
            adjudication = {
                "dispositions": [
                    {
                        "fingerprint": FINGERPRINT,
                        "disposition": "validated",
                        "rationale": "Reproduced against the captured branch evidence.",
                        "snapshot_id": SNAPSHOT_ID,
                    }
                ]
            }
            scope_path.write_text(json.dumps(scope), encoding="utf-8")
            reviewers_path.write_text("\n".join(json.dumps(reviewer) for reviewer in reviewers) + "\n", encoding="utf-8")
            adjudication_path.write_text(json.dumps(adjudication), encoding="utf-8")
            self.assertEqual(
                validate_review.main(
                    [
                        "--scope",
                        str(scope_path),
                        "--reviewers",
                        str(reviewers_path),
                        "--adjudication",
                        str(adjudication_path),
                        "--output",
                        str(output_path),
                    ]
                ),
                0,
            )
            self.assertEqual(json.loads(output_path.read_text(encoding="utf-8"))["status"], "PASS")
            reviewers_path.write_text("\n".join(json.dumps(reviewer) for reviewer in reviewers[:-1]) + "\n", encoding="utf-8")
            self.assertEqual(
                validate_review.main(
                    [
                        "--scope",
                        str(scope_path),
                        "--reviewers",
                        str(reviewers_path),
                        "--adjudication",
                        str(adjudication_path),
                        "--output",
                        str(output_path),
                    ]
                ),
                2,
            )
            self.assertEqual(json.loads(output_path.read_text(encoding="utf-8"))["status"], "INCOMPLETE")

    def test_mock_publication_is_idempotent(self) -> None:
        with tempfile.TemporaryDirectory(prefix="code-review-council-") as directory:
            root = Path(directory)
            review_path = root / "review.json"
            ledger_path = root / "ledger.json"
            review = {
                "schema_version": publish_review.SCHEMA_VERSION,
                "status": "PASS",
                "snapshot_id": SNAPSHOT_ID,
                "scope_manifest": {"base": "a" * 40, "head": "b" * 40, "changed_files": []},
                "findings": [],
                "errors": [],
            }
            review_path.write_text(json.dumps(review), encoding="utf-8")
            first = publish_review.main(
                ["--review", str(review_path), "--provider", "mock", "--ledger", str(ledger_path)]
            )
            second = publish_review.main(
                ["--review", str(review_path), "--provider", "mock", "--ledger", str(ledger_path)]
            )
            self.assertEqual(first, 0)
            self.assertEqual(second, 0)
            ledger = json.loads(ledger_path.read_text(encoding="utf-8"))
            self.assertEqual(len(ledger["published"]), 1)

    def test_fixture_evaluation_covers_required_cases(self) -> None:
        fixture_path = SCRIPT_DIR.parent / "fixtures" / "evaluation.json"
        fixture = run_evaluation.load_fixture(fixture_path)
        result = run_evaluation.evaluate(fixture)
        self.assertEqual(set(result["required_cases"]), run_evaluation.REQUIRED_CASES)
        self.assertGreaterEqual(result["set_counts"]["development"], 1)
        self.assertGreaterEqual(result["set_counts"]["held-out"], 1)
        self.assertIn("council", result["overall"])


if __name__ == "__main__":
    unittest.main()
