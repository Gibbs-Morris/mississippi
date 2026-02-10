# Verification

## Claim list
1. Builder APIs with terminal methods like `Done()` are inconsistent with Microsoft .NET builder conventions.
2. The refactor can remove terminal methods without losing expressiveness.
3. All existing builder usage sites can be migrated with minimal behavioral changes.
4. Tests and samples can be updated to the new pattern without weakening coverage.
5. The new pattern will remain consistent across client/server/silo/feature builder types.
