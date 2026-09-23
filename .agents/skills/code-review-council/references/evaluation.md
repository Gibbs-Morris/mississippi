# Evaluation method

The evaluation harness is an offline contract and safety evaluation. It does
not claim that a language model found the seeded defects unless real reviewer
traces are supplied separately. This distinction keeps deterministic fixture
results from being misreported as model precision or recall.

## Compared approaches

Each identical fixture is scored for:

1. `single-reviewer`: one ordinary reviewer with one general prompt;
2. `all-lenses`: one reviewer given all ten persona briefs; and
3. `council`: ten independent candidate outputs followed by deterministic
   scope, anchor, disposition, and publication checks.

The fixture records the seeded truth set and the candidate observations for
each approach. Truth fingerprints must be nonempty and unique within each
case, so duplicates cannot distort recall denominators. The runner verifies
the truth set, computes the metrics, and fails if required scenario coverage is
missing. When a Markdown report path is supplied, its parent directory is
created during output preflight before either result artifact is written.

## Required scenario coverage

The fixture set includes unchanged code with a defect, independently advanced
bases, stacked PRs, staged/unstaged/untracked selections, a staged defect
hidden by an unstaged correction, rename/delete paths, no changes, unresolved
index state, conditional distributed-system risk, an upstream-protected
non-defect, duplicate and same-location findings, stale PR discussion,
partial/rate-limited publication, revision changes, truncated diffs, reviewer
failure, unsupported models, unavailable parallelism, malicious instructions,
and a clean zero-finding case.

Development and held-out cases are tagged separately. The held-out set must be
scored without changing the expected truth or the evaluator’s rules.
The JSON result preserves separate `by_set.development` and
`by_set.held-out` metric objects so aggregate scores cannot hide held-out
regressions.

## Metrics

- `precision`: true predicted findings divided by all predicted findings;
- `recall`: true predicted findings divided by all seeded findings;
- `p0_p1_recall`: recall restricted to high-severity seeded defects;
- `false_blocker_rate`: clean cases incorrectly reported as blocking;
- `duplicate_publication_rate`: duplicate publication attempts divided by
  publication attempts;
- `invalid_anchor_rate`: findings with invalid or stale anchors divided by
  anchored findings;
- `scope_completeness`: required scope elements present divided by required
  elements;
- `policy_compliance`: safety-policy checks passed divided by checks run;
- `skill_trigger_accuracy`: expected trigger decisions matched;
- `latency_ms` and `tokens`: fixture-provided execution observations, clearly
  labelled as simulated when no live trace is supplied; and
- `unique_validated_contribution`: validated council findings not present in
  either baseline approach.

Run it with:

```text
pwsh .agents/skills/code-review-council/scripts/run-evaluation.ps1 -Fixtures .agents/skills/code-review-council/fixtures/evaluation.json -Output evaluation-results.json
```

The report must state which model, host, concurrency, and publication paths
were actually exercised. A missing live model or GitHub credential is an
explicit limitation, not a passing result.
