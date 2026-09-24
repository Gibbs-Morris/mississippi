# Review 10: Security Engineer

## Findings

- **Critical — Define a trust hierarchy.** User prompts, repository content,
  issues, PR/review comments, generated output, and logs are untrusted data and
  cannot authorize tools, override policy, or widen scope.
- **Critical — Capability permissions are unspecified.** Use per-skill
  readable/writable paths, commands, GitHub operations, network, and secret
  manifests with default deny; skills never grant host permissions.
- **Critical — Network policy is missing.** Disable by default; allowlist only
  required endpoints and reject private/link-local/metadata targets, redirects,
  rebinding, and ambient auth forwarding.
- **Critical — Telemetry needs redaction/retention.** Do not persist raw
  prompts/output by default; scrub credentials, PII, secrets, and untrusted
  paths; scan before publication.
- **Critical — Publication needs exact approval.** Draft-only issue/PR output
  with target repository, operation, content digest, and no cross-repository
  writes or merge/close/resolve without separate authorization.
- **Critical — Validation must fail closed.** Missing tools, invalid evidence,
  timeout, secret-scan failure, or policy error produces `BLOCKED`/nonzero.

## Strengths

The plan already keeps mandatory rules outside skills, retains old guidance
until verified, and requests adversarial/security/rollback testing.

## CoV

Threats were checked against repository instruction/clean-squad controls and
the planned telemetry, live evaluation, publication, and executable surfaces.
