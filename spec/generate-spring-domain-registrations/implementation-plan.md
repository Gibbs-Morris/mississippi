# Implementation Plan

## Outline
1. Locate current Spring manual registration wrappers and Program.cs usage.
2. Inspect Inlet client/server/silo generators for extension points to emit domain wrappers.
3. Decide scoping mechanism to generate only for Spring sample projects.
4. Implement generator outputs to emit AddSpringDomain wrappers per SDK type.
5. Remove manual SpringDomain*Registrations classes.
6. Confirm Program.cs references remain unchanged.
7. Update docs/instructions if needed.
