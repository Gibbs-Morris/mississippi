# Cosmos workflow migration evidence

Tracking: [issue #800](https://github.com/Gibbs-Morris/mississippi/issues/800).
Preparation base: `23cf0ba2e89d56f76d4fd358a5260b639099ed4f`.
This is one independent preparation slice; publication, integration validation,
current-head CI, review, approvals, and readiness remain with the delivery owner.

## Extraction and static checks

The [Aspire adapter](../instructions/aspire.instructions.md) retains all five
original Rules, frontmatter, governing thought, drift check, and scope/audience
exactly after LF normalization. Its mandatory route names the portable skill and
local binding and provides direct-file fallback. The broken Crescent Aspire test
path is replaced by inspected current [source bindings](cosmos-integration-bindings.md#inspect-current-sources).

| Measure | Original adapter | Migrated adapter |
| --- | ---: | ---: |
| LF-normalized lines | 74 | 34 |
| Words | 322 | 228 |
| UTF-8 bytes after LF normalization | 2,979 | 2,214 |
| `o200k_base` tokens | 716 | 509 |

The selected instruction-body reduction is 40 LF lines and 207 measured tokens.
The portable skill is 78 LF lines, 639 words, and 861 `o200k_base` tokens; it is
loaded for the bounded test-authoring task. These are context measurements, not
measured end-to-end runtime or total-task token savings.

- Skill-creator `quick_validate.py`: passed required frontmatter/name/description checks.
- Existing `npx --no-install markdownlint-cli2`: four changed Markdown files, zero issues under the repository configuration.
- [Routing cases](cosmos-integration-cases.json): nine unique IDs covering positive, negative, and collision requests; JSON validity is not behavioral execution.
- Static link/anchor checker: 28 relative references resolved, including actual source files and mandatory fallback routes.
- Content-derived Git blob hashes: all 2,121 tracked files outside the owned migration paths matched the preparation base. Sorted path/blob manifest SHA-256: `44cb02d573adacbf54f017a552995daf880aac504b7ebffc0615c91f8eaf29d1`.

## External consuming fixture

An isolated Orders consumer was prepared outside the repository with a real
Aspire AppHost, an empty xUnit v3/MTP test project, local package inputs, a bounded
user prompt, and an independent output checker. It requests create/read payload
round trip plus a wrong-partition NotFound assertion, with test-owned data and
teardown. It imports no Mississippi API, path, service key, or quality threshold.

The exact SDK `10.0.401` is installed; Docker Server `29.2.0` reports Linux after
an authorized daemon-access check. The supplied AppHost built against Aspire
`13.5.3` with zero warnings/errors. This establishes genuine AppHost compilation,
not test compilation, emulator startup, persisted assertions, or teardown.
The fixture checker rejects missing or empty executed-test evidence.

The fresh inherited-model worker inspected the fixture and current candidate,
but authorization review rejected its external test-file write. No test source
was written and zero tests executed; explicit fixture approval remains pending.
Its read-only installed-package inspection also found Aspire 13.5.3 requires
Newtonsoft.Json at least 13.0.4 while the supplied test manifest pins 13.0.3.
That is a fixture setup concern, not an observed build failure or skill outcome;
the earlier AppHost-only compile does not establish test-project compatibility.
Repair and execution await fixture authority; they cannot be claimed as passes.
Fresh inherited-model test authoring and observed runtime evaluation are pending.
NuGet restore, emulator image pull/data-plane startup, real test execution,
nonempty reports, and owned teardown still need observation for that outcome.
Full build/cleanup/test gates, the native-host selection matrix, and Copilot
behavior are unverified here; no Copilot retry or settings change was made.
The [retained citation limitation](cosmos-integration-bindings.md#retained-issue-reference-limitation)
is explicit and does not weaken the five retained rules.
