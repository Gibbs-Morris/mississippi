# Review 05: Platform Engineer

- Issue: The build and review agents should explicitly require inspection of deployment descriptors and workflow automation when the slice touches runtime shape. Why it matters: platform regressions often come from changed configuration rather than code alone. Proposed change: add to build/review workflow steps: inspect workflow files, deployment manifests, runtime configuration, and environment assumptions whenever operational behavior is affected. Evidence: requested platform, Kubernetes, SRE, and CI/CD bias. Confidence: Medium.
- Issue: The docs sidecar concept should capture operational documentation explicitly, not just product documentation. Why it matters: runbooks, alert semantics, rollback notes, and deployment caveats are part of enterprise delivery. Proposed change: expand the sidecar contract to include operational docs and runbook impacts. Evidence: requested emphasis on SRE, observability, and documentation quality. Confidence: High.
- Issue: Whole-branch verification should explicitly include operational rollback and failure-mode checks. Why it matters: reliability risk often emerges only after all slices compose together. Proposed change: add rollback-readiness and failure-mode synthesis to the whole-branch verification loop. Evidence: inference from enterprise deployment workflows and the requested review philosophy. Confidence: Medium.

## CoV

- Claim: platform verification belongs in the branch-wide loop, not only per-slice. Evidence: operational issues often emerge from composition. Confidence: Medium.