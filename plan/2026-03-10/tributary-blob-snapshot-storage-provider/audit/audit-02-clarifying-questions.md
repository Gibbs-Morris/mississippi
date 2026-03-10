# 02 Clarifying Questions

## A. Answered from the repository
1. **Should the Blob provider preserve the existing shared snapshot abstraction in V1?**
   - Answer: Yes. The existing abstraction is intentionally narrow and the issue explicitly prefers keeping it unchanged unless implementation evidence proves otherwise.
2. **Should provider registration follow existing Mississippi keyed-client and hosted-startup patterns?**
   - Answer: Yes. Existing Tributary and Brooks storage providers plus repository instructions converge on keyed client registrations and async bootstrap during hosted startup.
3. **Should emulator-backed verification live in a dedicated Mississippi L2 project?**
   - Answer: Yes. Repo testing guidance and existing `tests/*L2Tests + AppHost` projects support this directly.
4. **Where should the public docs live?**
   - Answer: Under the existing Tributary storage-provider docs area alongside the current Cosmos provider page.

## B. Questions for the user
1. **What should count as the required repeatable live Azure Blob smoke path?**
   - **A.** A dedicated opt-in Mississippi L2/live test project that runs only when Azure credentials/config are provided
   - **B.** A repo script/workflow path that exercises the provider against a real Azure Blob account outside the standard L2 project
   - **C.** A documented manual verification path only
   - **X.** I don't care — pick the best repo-consistent default

2. **Which concrete wiring example should the docs and verification path emphasize?**
   - **A.** A Mississippi runtime-host wiring example under `tests/`/framework-style hosting only
   - **B.** A sample application wiring example (for example Spring-style Aspire/runtime wiring)
   - **C.** Both a framework-host example and a sample-host example
   - **X.** I don't care — pick the best repo-consistent default

## CoV
- **Key claims**: Most design decisions are already constrained by repository evidence; the main remaining ambiguity is how to express the required live Azure smoke path and which wiring example should be canonical.
- **Evidence**: Repository findings in `01-repo-findings.md`.
- **Confidence**: High.
- **Impact**: These answers determine the verification/documentation slices before the master plan is finalized and decomposed.
