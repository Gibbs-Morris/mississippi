# Council orchestration and adjudication

The coordinator owns the review lifecycle. Reviewers are independent evidence
producers; they do not see each other’s initial findings and they do not write
to GitHub.

## Capability discovery

Before dispatch, record the host’s actual capabilities:

- requested and effective model identifiers;
- requested and effective reasoning settings, if exposed;
- requested and effective concurrency;
- available repository, Git, browser, and GitHub tools; and
- time, token, or rate limits when observable.

If the requested model is unsupported, report `INCOMPLETE` with the requested
and available values. Do not silently substitute a different model. If
parallelism is unavailable, run bounded sequential waves and record that fact;
do not claim concurrency was exercised.

## Dispatch

1. Verify the scope manifest is `READY` and save its immutable ID.
2. Build one reviewer input per persona from the frozen scope and persona brief.
3. Start the ten reviewers independently where the host supports it.
4. Collect one completion record per slot, including failures and
   `not_applicable` reasons.
5. Reject any result whose snapshot ID differs from the scope ID.

A failed or missing required reviewer makes the result `INCOMPLETE` even if the
other nine reviewers found nothing. An explicit `not_applicable` record is
complete evidence when its reason is specific to the frozen scope.

## Adjudication

The coordinator reads the candidate findings only after all initial reviewer
records are captured. For each candidate:

1. Re-open the exact path, symbol, line, or PR anchor from the frozen snapshot.
2. Reproduce the failure or trace the contract that makes it possible.
3. Search for counter-evidence in tests, callers, configuration, upstream
   contracts, or the PR discussion.
4. Decide the change relation and evidence strength independently of severity.
5. Record one disposition and rationale in the ledger.

Deduplicate by stable fingerprint, not by location. If two findings share a
line but describe different failures, retain both. If several personas report
the same root cause, combine their persona IDs while preserving the evidence
that each contributed.

## Status matrix

| Condition | Status |
| --- | --- |
| Complete evidence, no validated P0/P1, no serious gaps | `PASS` |
| Validated P0/P1 remains | `BLOCKED` |
| Missing/failed reviewer, stale snapshot, invalid anchor, or unresolved evidence | `INCOMPLETE` |
| Change-oriented scope has no eligible changes | `NO_CHANGES` |

P2/P3 findings remain in the report and disposition ledger. The repository or
caller may choose to treat them as follow-up work, but the council does not
silently drop them.

## Output sequence

Write the scope/coverage manifest first, reviewer execution metadata second,
the disposition ledger third, and the concise Markdown plus final JSON last.
The final JSON is the only status authority. A narrative claiming “clean” does
not override `INCOMPLETE` or `BLOCKED` in the structured result.
