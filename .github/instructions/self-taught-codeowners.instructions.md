---
applyTo: '.github/CODEOWNERS'
---

# Self-Taught Lessons: Code Owners

Governing thought: Validate owner access with GitHub before relying on CODEOWNERS protection.

> Drift check: Recheck owner permissions and GitHub's CODEOWNERS errors at the proposed commit.

## Rules (RFC 2119)

- Agents **SHOULD** check GitHub's CODEOWNERS errors at the proposed commit before relying on an owner entry. Why: GitHub rejected the workflow team as `Unknown owner` despite its valid syntax; the organization team passed after it was created with repository Write access.

## Scope and Audience

Agents maintaining repository code ownership.

## References

- [GitHub code owner requirements](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [Self-improvement governance](self-improvement.instructions.md)
