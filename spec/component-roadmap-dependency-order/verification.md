# Verification

## Claim List

1. The roadmap file is docs/Docusaurus/docs/refraction/component-roadmap.md.
2. Phase tables do not currently encode dependencies.
3. MooringLine depends on AnchorPoint, so AnchorPoint must not appear after MooringLine.
4. Phase tables can be augmented with a Dependencies column without breaking the docs build.
5. A Dependency Rules section can capture build order and cross-phase dependencies.

## Verification Questions (TBD)

- Q1: Does component-roadmap.md exist at the documented path?
- Q2: Do phase tables currently lack a Dependencies column?
- Q3: Is AnchorPoint listed after MooringLine in the current Phase 1/2 ordering?
- Q4: Does Docusaurus build succeed after adding a Dependencies column to tables?
- Q5: Are any components in Phase 9 dependent on Inlet/Reservoir (external dependencies)?
- Q6: Do catalog entries omit dependency annotations today?
- Q7: Are there any cross-phase dependencies that would require reordering within phases?
