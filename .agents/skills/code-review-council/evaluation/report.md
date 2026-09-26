# Code Review Council evaluation

This is an offline deterministic fixture evaluation. It does not measure live model precision, recall, or provider latency.

- Cases: `24`
- Development cases: `13`
- Held-out cases: `11`

## Overall results

| Approach | Precision | Recall | P0/P1 recall | False blockers | Scope completeness | Policy |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `single-reviewer` | 0.4286 | 0.2500 | 0.2500 | 0.2308 | 0.8750 | 0.9583 |
| `all-lenses` | 0.6923 | 0.7500 | 0.7500 | 0.2308 | 0.8750 | 0.9583 |
| `council` | 1.0000 | 1.0000 | 1.0000 | 0.0000 | 0.8750 | 1.0000 |

## Interpretation

On these authored fixtures, the council path retained all seeded findings and
avoided false blockers, invalid anchors, and duplicate publication. It also
used more fixture tokens and latency than either baseline. Scope completeness
is 0.8750 because revision-change, truncated-diff, and unresolved-index cases
correctly report missing evidence instead of treating an incomplete snapshot
as complete. The policy score is lower because unsupported-model and
reviewer-failure cases correctly remain incomplete rather than being reported
as clean.

## Limitations

- No live Codex reviewer or GitHub publication was executed by this offline runner.
- A fresh `codex exec --ephemeral --sandbox read-only` smoke invocation reached
  host startup but could not obtain model output because the environment rejected
  the `api.openai.com` certificate (`UnknownIssuer`); reviewer execution,
  concurrency, and live latency are therefore not claimed.
- Fixture latency and token values are simulated observations for comparison only.
- A live promotion decision requires real traces, host capability metadata, and current CI/review evidence.
