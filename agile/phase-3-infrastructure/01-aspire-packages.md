# Task 3.1: Add Aspire Packages

**Status**: ⬜ Not Started  
**Depends On**: None

## Goal

Add .NET Aspire 13.1.0 packages to `Directory.Packages.props` for central package management.

## Acceptance Criteria

- [ ] All Aspire hosting packages added at version 13.1.0
- [ ] All Aspire client/integration packages added at version 13.1.0
- [ ] `dotnet restore` succeeds for mississippi.sln and samples.sln
- [ ] No version conflicts

## Packages to Add

### Hosting Packages (for AppHost projects)

```xml
<!-- Aspire Hosting -->
<PackageVersion Include="Aspire.Hosting.AppHost" Version="13.1.0" />
<PackageVersion Include="Aspire.Hosting.Azure.CosmosDB" Version="13.1.0" />
<PackageVersion Include="Aspire.Hosting.Azure.Storage" Version="13.1.0" />
```

### Client/Integration Packages (for service projects)

```xml
<!-- Aspire Client Integration -->
<PackageVersion Include="Aspire.Azure.Storage.Blobs" Version="13.1.0" />
<PackageVersion Include="Aspire.Microsoft.Azure.Cosmos" Version="13.1.0" />
```

## TDD Steps

This task is infrastructure-only; no test code needed.

1. **Update**: Edit `Directory.Packages.props` to add packages
2. **Verify**: Run `dotnet restore` on both solutions
3. **Build**: Run `dotnet build` to confirm no conflicts

## File to Modify

- `Directory.Packages.props`

## Validation Commands

```powershell
# Restore both solutions
dotnet restore mississippi.sln
dotnet restore samples.sln

# Verify no package conflicts
dotnet build mississippi.sln --no-restore
dotnet build samples.sln --no-restore
```

## Notes

- Aspire 13.1.0 is the latest stable as of December 2025
- All Aspire packages should use the same version for compatibility
- These packages only add to `Directory.Packages.props`; actual usage comes in subsequent tasks
