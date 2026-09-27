---
applyTo: '**'
---

# Self-Taught Lessons: Git Publication

Governing thought: Keep remote updates within the authorized destinations.

> Drift check: Check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD constrain each authorized push to verified explicit source/destination refspecs with mirror mode and implicit tag expansion disabled. Why: The [PR #803 finding](https://github.com/Gibbs-Morris/mississippi/pull/803#discussion_r4114302241) and local bare-remote controls reproduced unrelated branch deletion, private-ref publication and implicit tag expansion from configuration.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Git push documentation](https://git-scm.com/docs/git-push)
