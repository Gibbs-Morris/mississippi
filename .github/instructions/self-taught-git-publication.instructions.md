---
applyTo: '**'
---

# Self-Taught Lessons: Git Publication

Governing thought: Keep remote updates within the authorized destinations.

> Drift check: Check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD constrain each authorized push to verified explicit source/destination refspecs. Why: The [PR #803 publication controls](https://github.com/Gibbs-Morris/mississippi/pull/803#discussion_r4114302241) reproduced configuration-selected publication of an unrequested branch.
- Agents SHOULD disable configured mirror mode for pushes with enumerated destinations. Why: The PR #803 bare-remote mirror control deleted an unrelated branch and published private refs despite a successful exit.
- Agents SHOULD disable implicit tag expansion for pushes with enumerated destinations. Why: The PR #803 follow-tags control published an unrequested annotated tag from configuration.
- Agents SHOULD reject or clear configured push options before publication. Why: The [PR #803 push-option controls](https://github.com/Gibbs-Morris/mississippi/pull/803#discussion_r4118048211) transmitted `ci.skip` and another server option despite pinned refs; command-local `push.pushOption=` cleared both without changing configuration.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Git push documentation](https://git-scm.com/docs/git-push)
