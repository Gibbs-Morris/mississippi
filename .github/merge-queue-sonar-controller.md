# Trusted Sonar controller

This controller rebuilds a completed `SonarCloud` source run using the immutable workflow definition on the repository's default branch. It is a manual, independently usable layer. `sonar-cloud.yml` continues providing the existing PR/main behavior until the subsequent source-routing layer and operator deployment are complete.

The workflow and checkout revisions must also match the live default-branch tip at each controller-origin check. A rerun preserves its original workflow revision and is rejected after the default branch advances; unavailable or inconsistent ref metadata fails closed. This check does not revoke an already issued credential or make branch movement and environment admission atomic.

## Source identity

The controller reads source-run and current PR/queue metadata from GitHub. Candidate artifacts never supply identity or executable scripts to the host.

- PR: build GitHub's current immutable PR merge commit; report the PR's source head, number, source branch and immediate base branch.
- Main: analyze the exact current default-branch revision with an explicit branch identity.
- Manual branch: analyze the exact current selected branch with its own identity and the default branch as target.
- Queue: resolve the exact live candidate and complete validated constituent prefix. Analyze a distinct SHORT branch against an already analyzed, exact current target revision.

Fork source runs are rejected; this layer does not grant fork workflows credentials. Stale runs, unavailable metadata, ambiguous candidates and changed source identities fail. Rechecks allow only the existing resolver's verified, landed contiguous predecessor prefix.

## Credential boundary

`intake` has no environment. It rejects any globally visible `SONAR_TOKEN` or `SONAR_ANALYSIS_TOKEN`. The analysis job requests only `SONAR_ANALYSIS_TOKEN` from `sonar-analysis`, and validates exactly one deployment policy: branch `main` (the actual default branch), with no tags or wildcard policies. A reviewer-only environment is insufficient because candidate workflow code must never receive this credential. If the policy list omits its branch/tag type, intake queries that exact policy ID and requires matching name/ID plus explicit branch type. Missing or inconsistent detail fails closed. The controller repeats this deployment-policy validation after upload and completion verification before reporting success.

Pinned tools and the controller driver come from trusted inputs. Prepare installs scanner 11.3.0, coverage 18.11.0, GitVersion 6.5.1 and Pester 5.7.1 into separate tooling. The SDK image is pinned by digest.

Version/build/test execution uses a non-root container matching the runner's UID/GID, read-only root filesystem, dropped capabilities, fixed HOME/cache paths and minimal explicit environment. Candidate stages cannot mount the upload cache, host Docker socket or host credentials. The scanner configuration, rulesets, additional files and requested analyzer DLLs are projected read-only; original private downloads remain inaccessible. Candidate NuGet packages use a separate disk directory that is never mounted during upload. Locked package restore remains authoritative.

Begin and end execute in separate fresh containers. Begin uses a clean checkout and the protected token. Before exposing scanner assets to a build, the controller checks that they do not contain the actual credential. End receives the original private scanner configuration/cache, another clean checkout and only validated, bounded reports/generated analysis inputs. XML DTDs, external paths, links, unknown project settings and changes to existing source/configuration are rejected. Native errors stop the upload.

Before reading candidate reports on Linux, the handoff checks the file type using `/usr/bin/stat`. FIFOs, sockets and other nonregular files are rejected; missing or invalid type metadata fails.

Report validation protects this credential boundary. It does not establish that malicious build inputs cannot fabricate their own test or analysis data. Repository review remains necessary.

## Quality gate and evidence

The controller preserves the reviewed repository gate: A security/reliability/maintainability ratings, 100% review of new hotspots, zero new code smells/violations, duplication threshold 6% and coverage threshold 60%. Gate reassignment or criteria changes during analysis fail. Queue classification must retain the reviewed `(branch|release)-.*` policy and candidate SHORT classification; the controller checks both before and after upload.

Completion requires a fresh successful check from the genuine Sonar app (12526) on the exact source SHA. A check from GitHub Actions, an earlier analysis or a different revision cannot substitute. Native Sonar code-scanning protection remains applicable to ordinary PRs; [GitHub excludes merge groups from code-scanning merge protection](https://docs.github.com/en/code-security/concepts/code-scanning/merge-protection). Live held positive/negative candidate evidence is required before claiming the queue's genuine gate provides the reviewed security criteria.

## Operator deployment

Do not deploy credentials or enable production queue rules as part of merging this code alone. Prepare a reviewed deployment and rollback first:

1. Land the controller and source-routing code through required reviews/checks.
2. Record the current gate assignment/criteria, default-branch Sonar revision, repository/org secret inventories and environment policies. An inaccessible inventory is unknown, not proof of absence.
3. Protect `sonar-analysis` with the exact default-branch deployment policy, rotate the analysis token into its unique environment secret, and remove globally accessible copies. Coordinate this with source routing; the old workflow cannot analyze without its old token.
4. Refresh the main Sonar baseline to the exact current target revision through the trusted controller.
5. Run held PR/main/manual/queue controls, including code predecessor under docs follower, failed gate/security control, changed source metadata, token absence during candidate execution and environment denial on candidate refs. Keep the pilot Hold enforced and never publish Hold success on candidates.
6. Verify exact candidate SHA, SHORT classification, target baseline, genuine provider check, unchanged backend criteria and rollback before production activation.

After deployment, manual dispatch is:

```powershell
gh workflow run sonar-trusted-analysis.yml --ref main -f source-run-id=<completed-SonarCloud-run-id>
```

Dispatch targets the trusted default-branch definition. It does not select candidate workflow code. No environment, secret, production ruleset or queue setting is changed by this controller.
