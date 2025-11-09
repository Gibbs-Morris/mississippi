---
applyTo: "**/*.{csproj,props,targets}"
---

# Project File Standards

## Scope
MSBuild files, CPM. Minimal project files only. No version duplication.

## Quick-Start
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Package" />
  </ItemGroup>
</Project>
```

## Core Principles
Minimal content only. No versions in `<PackageReference>` (managed in `Directory.Packages.props`). No duplicate props from `Directory.Build.props`. Auto naming: `Company.$(MSBuildProjectName)`. Test projects auto-configured.

## Anti-Patterns
❌ Version attributes. ❌ Duplicate properties. ❌ Assembly naming overrides. ❌ Test package references (auto for `*Tests` projects).

## Enforcement
Builds fail on CPM violations. Code reviews verify: minimal content, no versions, no duplicates.
