# Implementation plan

1. Identify changed src files via git diff.
2. Map changed files to test projects (inspect project references).
3. Add focused L0 tests for changed logic and edge cases.
4. Run coverage for affected test projects.
5. Iterate until >=95% for changed src code or document exceptions.
