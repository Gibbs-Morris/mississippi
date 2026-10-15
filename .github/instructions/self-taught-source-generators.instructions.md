---
applyTo: '**/*Generator*.cs'
---

# Self-Taught Lessons: Source Generators

Governing thought: Compile and exercise generated collection mappings to preserve nullability.

> Drift check: Before adding a lesson, check overlapping instructions for conflicts and duplicates under `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Source generator collection tests **SHOULD** check null containers separately from null elements through compiled DTOs and executed mappers. Why: PR #829 dropped container nullability and threw `ArgumentNullException` from `Select` while nullable element tests passed.

- Immutable-array mapper tests **SHOULD** include default, empty and populated inputs. Why: PR #829 passed populated enum fixtures while default-array enumeration threw `NullReferenceException` or `InvalidOperationException`.

## Scope and Audience

Agents editing source generators or their collection mapping tests.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Testing policy](testing.instructions.md)
- [Observed correction](https://github.com/Gibbs-Morris/mississippi/pull/829)
