# Trusted Sonar controller

This controller rebuilds a completed `SonarCloud` source run using the immutable workflow definition on the repository's default branch. It supports manual dispatch and default-off automatic admission from successful completed source runs. `sonar-cloud.yml` continues providing the existing PR/main behavior until the subsequent source-routing layer and operator deployment are complete.

The workflow and checkout revisions must also match the live default-branch tip at each controller-origin check. A rerun preserves its original workflow revision and is rejected after the default branch advances; unavailable or inconsistent ref metadata fails closed. This check does not revoke an already issued credential or make branch movement and environment admission atomic.

## Source identity

The controller reads source-run and current PR/queue metadata from GitHub. Candidate artifacts never supply identity or executable scripts to the host.

Both pinned host checkouts explicitly bind Gibbs-Morris/mississippi and its protected `main` branch. Before importing any repository driver or module, an inline workflow step requires a successful Git lookup and the checkout SHA equal to the valid `GITHUB_WORKFLOW_SHA` attestation. A changed checkout, missing attestation or Git failure stops execution before import. The driver then verifies the default-branch workflow origin and live default-branch tip before accepting source metadata. Renaming the default branch requires a reviewed workflow update; the fixed checkout and origin checks otherwise fail closed. GitHub defines this value as the [workflow-file commit](https://docs.github.com/en/actions/reference/workflows-and-actions/variables#default-environment-variables).

- Ordinary PR: require confirmed mergeability and exactly the current base and source head as merge parents. Build that immutable revision, fetch the immediate base into both workspaces while retaining the default ref for versioning, and report the source head, PR number and actual target.
- Native stacked PR: authenticate documented stack number/position/size/trunk and complete ordered membership. Bind fresh full prefix PRs to their listed head/base identities and repository IDs. Verify each active two-parent merge against its current head and the preceding verified synthetic merge, anchored to the fresh trunk. Use that trunk for both workspace fetches and Sonar's baseline; raw branch heads cannot replace synthetic parents. No opaque PR stack ID or lightweight merge SHA is assumed.
- Main: analyze the exact current default-branch revision with an explicit branch identity.
- Manual branch: analyze the exact current selected branch with its own identity; snapshot the current default-branch commit as target, require that exact LONG main baseline in Sonar before and after upload, and reject target movement during source rechecks.
- Queue: resolve the exact live candidate and complete validated constituent prefix. Analyze a distinct SHORT branch against an already analyzed, exact current target revision.

Both fresh source workspaces retain release tags from the trusted repository for GitVersion's offline version calculation. The checkout stays bound to the verified immutable candidate revision.

Fork source runs are rejected; this layer does not grant fork workflows credentials. Stale runs, unavailable metadata, ambiguous candidates and changed source identities fail. Rechecks allow only the existing resolver's verified, landed contiguous predecessor prefix.

A leading merged native prefix requires closed/merged metadata and proof that every immutable landing is contained in the current trunk. The first remaining active PR must already target that trunk after automatic rebase; transient or inconsistent metadata blocks intake. Prefix identity is rechecked before and after upload. Unrelated upper-layer additions alone do not invalidate it. Ordinary exact-parent checks and Sonar's existing fork rejection remain in force. This read-only identity proof does not establish deployed credentials or genuine held analysis.

## Credential boundary

`intake` has no environment. It rejects any globally visible `SONAR_TOKEN` or `SONAR_ANALYSIS_TOKEN`. The analysis job requests only `SONAR_ANALYSIS_TOKEN` from `sonar-analysis`, and validates exactly one deployment policy: branch `main` (the actual default branch), with no tags or wildcard policies. A reviewer-only environment is insufficient because candidate workflow code must never receive this credential. If the policy list omits its branch/tag type, intake queries that exact policy ID and requires matching name/ID plus explicit branch type. Missing or inconsistent detail fails closed. The controller repeats this deployment-policy validation after upload and completion verification before reporting success.

The stage dispatcher has executable L0 coverage for Prepare, Version, Begin, Build and End, including native failures, pinned module download failure, candidate credential rejection and missing protected credentials. Tests invoke the actual script in a separate PowerShell process with filesystem and tool side effects replaced; these fixtures do not prove deployed credential or provider behavior.

Pinned tools and the controller driver come from trusted inputs. Prepare installs scanner 11.3.0, coverage 18.11.0, GitVersion 6.5.1 and Pester 5.7.1 into separate tooling. The SDK image is pinned by digest.

Version/build/test execution uses a non-root container matching the runner's UID/GID, read-only root filesystem, dropped capabilities, fixed HOME/cache paths and minimal explicit environment. Candidate stages cannot mount the upload cache, host Docker socket or host credentials. The scanner configuration, rulesets, additional files and requested analyzer DLLs are projected read-only; original private downloads remain inaccessible. Candidate NuGet packages use a separate disk directory that is never mounted during upload. Locked package restore remains authoritative.

Begin and end execute in separate fresh containers. Begin uses a clean checkout and the protected token. Before exposing scanner assets to a build, the controller checks that they do not contain the actual credential. End receives the original private scanner configuration/cache, another clean checkout and only validated, bounded reports/generated analysis inputs. XML DTDs, external paths, links, unknown project settings and changes to existing source/configuration are rejected. Native errors stop the upload.

Before reading candidate reports on Linux, the handoff checks the file type using `/usr/bin/stat`. FIFOs, sockets and other nonregular files are rejected; missing or invalid type metadata fails.

Report validation protects this credential boundary. It does not establish that malicious build inputs cannot fabricate their own test or analysis data. Repository review remains necessary.

## Quality gate and evidence

PR analysis requires exactly one existing Sonar analysis of its resolved target branch at the exact current target SHA. This applies to ordinary main, feature and topic targets and the verified native stack trunk. Missing, ambiguous, malformed or stale baselines fail before analysis, immediately before upload and again after genuine provider verification. Ordinary PR targets may be SHORT or LONG; the separate manual and queue requirements for a LONG main baseline remain in force.

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

## Automatic admission and ordering

`sonar-trusted-analysis.yml` also listens for completed `SonarCloud` workflow runs. Automatic intake is off unless `SONAR_TRUSTED_ANALYSIS_ENABLED` is true; manual source-run dispatch remains available. The workflow definition must be on the default branch. Source routing is a separate rollout layer; this variable alone does not protect or relocate credentials.

Intake requires a successful approved repository run and correlates its ID, attempt, event, head SHA and ref with freshly fetched API metadata. Forks, recursion, failed/incomplete runs and changed attempts are rejected. Those constraints follow every later source read, including both post-upload checks. Exported completion callers must explicitly pass the admission object for automatic runs or null for manual dispatch; omitting that parameter is a binding error.

Protected analysis jobs serialize by validated Sonar identity: PR number or the hash of the exact branch name. This applies to manual and automatic requests. Distinct queue branches can run independently; separate main source runs cannot upload concurrently. GitHub concurrency retains only the latest pending job for a group. A request that becomes stale while waiting fails its fresh source check.

Automatic queue admission waits for ordinary main-baseline lag before execution and again before upload, because a predecessor may land during the candidate build. Each pass permits at most 31 attempts and 30 one-minute sleeps. API time is additional; the existing 120-minute analysis timeout still bounds the job. Held-pilot timing must establish the fit within the queue timeout before activation.

Only a valid LONG/isMain default branch with a different revision is treated as pending. Missing/duplicate/malformed baseline data, classification changes, unreviewed criteria and API failures fail immediately. The controller compares gate assignment, criteria and classification across retries, refreshes the live candidate after policy reads, and accepts a landed prefix only through the existing verified membership proof. Manual baseline mismatch remains an immediate failure. Post-upload checks remain strict; source and provider updates are not atomic.
