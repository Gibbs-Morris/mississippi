# Review contracts

The contracts use `schema_version: "code-review-council/v1"`. The JSON schema
files in `assets/` cover the top-level scope and result objects; this document
defines the fields whose meaning cannot be expressed by JSON Schema alone.

## Reviewer result

Each JSONL record contains:

| Field | Meaning |
| --- | --- |
| `review_id` | Stable reviewer-run identifier. |
| `persona_id` | One of the ten IDs in `personas.md`. |
| `snapshot_id` | Exact immutable scope digest. |
| `status` | `complete`, `not_applicable`, or `failed`. |
| `reason` | Required for `not_applicable` and `failed`. |
| `requested_model` / `effective_model` | Host-reported model settings; `null` when unavailable. |
| `requested_concurrency` / `effective_concurrency` | Requested and observed parallelism. |
| `findings` | Zero or more finding objects. |
| `completed_at_utc` | Completion timestamp for auditability. |

## Finding

Every finding has:

```json
{
  "fingerprint": "sha256:...",
  "persona_ids": ["security-adversary"],
  "category": "security",
  "severity": "P1",
  "snapshot_id": "sha256:...",
  "path": "src/example.cs",
  "symbol": "Example.Handle",
  "line": 42,
  "scenario": "A caller supplies ...",
  "trigger": "When ...",
  "impact": "...",
  "evidence": ["..."],
  "remediation": "...",
  "uncertainty": "...",
  "change_relation": "introduced"
}
```

`severity` is one of `P0`, `P1`, `P2`, or `P3`. Evidence strength is a
separate adjudication decision. `change_relation` is one of `introduced`,
`worsened`, `pre-existing`, `out-of-scope`, or `unknown`. A line is a positive
one-based line number; a deleted or renamed path may use the line from the
captured old side and must explain that choice in `evidence`.

When multiple personas report the same fingerprint, the consolidated finding
keeps the union of their evidence and a `reviewer_evidence` entry for every
source record. Conflicting change relations are represented as `unknown` on
the consolidated finding while each original relation remains in that entry.

## Disposition ledger

The coordinator records exactly one disposition for every candidate fingerprint:

- `validated`: the evidence supports the finding and it belongs in the result;
- `duplicate`: equivalent to `duplicate_of`, which remains the canonical item;
- `rejected`: counter-evidence disproves the proposed finding;
- `pre-existing`: real but not introduced or worsened by the selected change;
- `out-of-scope`: real but outside the requested review scope; or
- `unresolved`: evidence is insufficient and prevents a clean result.

`duplicate` requires `duplicate_of`; `rejected`, `pre-existing`,
`out-of-scope`, and `unresolved` require a concise evidence-backed rationale.
The ledger input includes a non-empty adjudicator, snapshot ID, and UTC
timestamp. Each disposition repeats the snapshot ID so a ledger cannot be
silently applied to another scope.

## Final result

The top-level result includes:

- `status`: exactly `PASS`, `BLOCKED`, `INCOMPLETE`, or `NO_CHANGES`;
- the complete `scope_manifest` or a reference to its immutable artifact;
- `reviewers`: all ten completion records;
- `findings`: consolidated findings with disposition and supporting evidence;
- `execution`: model, concurrency, duration, and tool metadata;
- `dispositions`: the full ledger; and
- `publication`: dry-run, published, already-published, or not-requested state.

Missing fields, duplicate reviewer slots, mismatched snapshot IDs, invalid
anchors, or a missing disposition are evidence gaps. They cannot be converted
to `PASS` by lowering the severity or omitting the item.
