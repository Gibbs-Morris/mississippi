---
applyTo: '**'
---

# Project File Management Best Practices

This document defines the project file standards and best practices for .NET applications using centralized package management. All `.csproj` files must follow these guidelines to ensure consistency, maintainability, and proper utilization of centralized package management.

## At-a-Glance Compliance Checklist

- Use centralized package management via `Directory.Packages.props`; no version numbers in `PackageReference`.
- Keep `.csproj` minimal; do not duplicate properties from `Directory.Build.props`.
- Choose the correct SDK (`Microsoft.NET.Sdk` / `.Web`) for the project type.
- Include only project-specific properties (e.g., `OutputType`, `GeneratePackageOnBuild`).
- Test project settings and test packages are automatic for projects ending with `Tests`.
- Do not override automatic assembly/namespace naming.
- Only add necessary `ProjectReference`/`PackageReference` entries (without versions).
- Validate with a clean build; fix any warnings (warnings are treated as errors in CI per Build Rules).

### Quick Validation Commands

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/final-build-solutions.ps1
```

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

## Core Principles

### Keep Project Files Minimal

- **Project files should be as minimal as possible** - avoid duplicating information already defined in `Directory.Build.props`
- **Use centralized package management** - all package versions are controlled through `Directory.Packages.props`
- **Leverage Directory.Build.props** - common properties, settings, and configurations are inherited automatically
- **No version numbers in project files** - package versions are managed centrally to prevent version conflicts
- **Focus on project-specific concerns only** - only include settings that are unique to the specific project

### Central Package Management Strategy

- **Directory.Packages.props** controls all package versions across the solution
- **PackageReference elements** in project files should not include version numbers
- **Use `<PackageReference Include="PackageName" />` format** without version attributes
- **Version conflicts are prevented** by centralized version management
- **Transitive dependencies** are automatically managed

## Required Project File Structure

### Minimal Project File Template

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Only project-specific properties that differ from Directory.Build.props -->
    <OutputType>Exe</OutputType> <!-- Only if this is a console app -->
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Only if this project produces a NuGet package -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Package references WITHOUT version numbers -->
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
  </ItemGroup>

  <ItemGroup>
    <!-- Project references -->
    <ProjectReference Include="..\Other.Project\Other.Project.csproj" />
  </ItemGroup>

</Project>
```

### What NOT to Include

❌ **Avoid these common anti-patterns:**

```xml
<!-- DON'T: Version numbers in PackageReference (managed centrally) -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.1" />

<!-- DON'T: Properties already in Directory.Build.props (automatic) -->
<TargetFramework>net9.0</TargetFramework>
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<ImplicitUsings>true</ImplicitUsings>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<AnalysisMode>All</AnalysisMode>
<LangVersion>13.0</LangVersion>

<!-- DON'T: Test project settings (automatic for *Tests projects) -->
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>

<!-- DON'T: Test packages (automatic for *Tests projects) -->
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="coverlet.collector" />
<PackageReference Include="Moq" />
<PackageReference Include="Allure.Xunit" />

<!-- DON'T: Using statements for tests (automatic) -->
<Using Include="Xunit" />

<!-- DON'T: Repository info (automatic) -->
<RepositoryUrl>https://github.com/Organization/repository</RepositoryUrl>
<RepositoryType>git</RepositoryType>

<!-- DON'T: Assembly naming (automatic pattern follows project name) -->
<AssemblyName>Company.SomeName</AssemblyName>
<RootNamespace>Company.SomeName</RootNamespace>
```

### What TO Include

✅ **Only include project-specific settings:**

```xml
<!-- Project type (only if different from default library) -->
<OutputType>Exe</OutputType>

<!-- Package generation (only for packages) -->
<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
<PackageId>Company.SomeFeature</PackageId>
<Description>Specific description for this package</Description>
<PackageTags>tag1;tag2;company</PackageTags>

<!-- Project-specific compiler directives -->
<DefineConstants>SPECIFIC_FEATURE</DefineConstants>

<!-- Unique build configurations -->
<Configurations>Debug;Release;Integration</Configurations>
```

## Assembly and Namespace Naming Convention

The build system automatically generates assembly names and root namespaces following this pattern:

- **AssemblyName**: `Company.$(MSBuildProjectName)`
- **RootNamespace**: `Company.$(MSBuildProjectName.Replace(" ", "_"))`

**Examples:**

- Project: `EventSourcing.csproj` → Assembly: `Company.EventSourcing` → Namespace: `Company.EventSourcing`
- Project: `Event Sourcing.csproj` → Assembly: `Company.Event Sourcing` → Namespace: `Company.Event_Sourcing`

**⚠️ Do NOT override these automatic naming patterns** unless there's a
specific business requirement. The automatic pattern ensures consistency across
the entire solution.

```xml
<!-- Example: These names are automatically generated -->
<AssemblyName>Company.EventSourcing</AssemblyName>
<RootNamespace>Company.EventSourcing</RootNamespace>
```

## What's Automatically Configured

### Directory.Build.props - Automatic Settings

The following properties are **automatically configured** for ALL projects and
should **NEVER** be included in individual `.csproj` files:

#### Universal Settings Applied to All Projects

```xml
<!-- Build Configuration -->
<TargetFramework>net9.0</TargetFramework>
<LangVersion>13.0</LangVersion>
<ImplicitUsings>true</ImplicitUsings>
<Nullable>enable</Nullable>
<Deterministic>true</Deterministic>
<AnalysisMode>All</AnalysisMode>

<!-- Performance & Optimization -->
<ServerGarbageCollection>true</ServerGarbageCollection>

<!-- Documentation & Source Linking -->
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<IncludeSymbols>true</IncludeSymbols>
<IncludeSource>true</IncludeSource>

<!-- Package Management -->
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>

<!-- Repository Information -->
<RepositoryType>git</RepositoryType>
<RepositoryUrl>https://github.com/Organization/repository</RepositoryUrl>

<!-- Naming Conventions -->
<AssemblyName>Company.$(MSBuildProjectName)</AssemblyName>
<RootNamespace>Company.$(MSBuildProjectName.Replace(" ", "_"))</RootNamespace>
```

#### Automatic Code Analysis & Quality Tools

All projects automatically include these analyzers:

- **Microsoft.VisualStudio.Threading.Analyzers** - Threading best practices
- **SonarAnalyzer.CSharp** - Code quality and security
- **IDisposableAnalyzers** - Proper disposal patterns
- **Microsoft.CodeAnalysis.NetAnalyzers** - .NET API guidelines
- **StyleCop.Analyzers.Unstable** - Code style enforcement
- **Microsoft.SourceLink.GitHub** - Source linking for debugging

#### Test Projects Get Extra Configuration

Projects ending with `Tests` **automatically receive**:

**Test-Specific Properties:**

```xml
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>
```

**Test Framework Packages (automatically included):**

- **Microsoft.NET.Test.Sdk** - Test SDK
- **xunit** - Test framework
- **xunit.runner.visualstudio** - Visual Studio test runner
- **coverlet.collector** - Code coverage collection
- **Moq** - Mocking framework
- **Allure.Xunit** - Test reporting

**Automatic Using Statements:**

```xml
<Using Include="Xunit" />
```

**Automatic InternalsVisibleTo:**

```xml
<InternalsVisibleTo Include="$(AssemblyName).Tests" />
<InternalsVisibleTo Include="$(AssemblyName).UnitTests" />
<InternalsVisibleTo Include="$(AssemblyName).IntegrationTests" />
<InternalsVisibleTo Include="$(AssemblyName).L0Tests" />
<InternalsVisibleTo Include="$(AssemblyName).L1Tests" />
<InternalsVisibleTo Include="$(AssemblyName).L2Tests" />
<InternalsVisibleTo Include="$(AssemblyName).L3Tests" />
<InternalsVisibleTo Include="$(AssemblyName).L4Tests" />
<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
```

### Directory.Packages.props - Centralized Package Management

- **`ManagePackageVersionsCentrally` is enabled** - all versions controlled centrally
- **66+ packages pre-configured** with specific versions
- **Automatic version resolution** prevents conflicts
- **Single source updates** - change one file to update all projects
- **Security vulnerability management** - update vulnerable packages in one location

### What This Means for Your Project Files

❌ **NEVER include these in your `.csproj` files** (automatically handled):

```xml
<!-- Build settings (all automatic) -->
<TargetFramework>net9.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>true</ImplicitUsings>
<GenerateDocumentationFile>true</GenerateDocumentationFile>

<!-- Test project settings (automatic for *Tests projects) -->
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>

<!-- Test packages (automatic for *Tests projects) -->
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="Moq" />

<!-- Any package with a version number -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.1" />
```

✅ **Only include project-specific settings:**

```xml
<!-- Different output type -->
<OutputType>Exe</OutputType>

<!-- Package publishing settings -->
<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
<PackageId>Company.CustomName</PackageId>

<!-- Project-specific configurations -->
<Configurations>Debug;Release;Integration</Configurations>
```

## Common Project Types

### Library Project (Default)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

### Console Application

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
</Project>
```

### Web Application

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  </ItemGroup>
</Project>
```

### Test Project (Automatic Configuration)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- 
  Everything below is AUTOMATIC for projects ending with 'Tests':
  - IsPackable=false
  - IsTestProject=true  
  - Microsoft.NET.Test.Sdk, xunit, Moq, Allure.Xunit packages
  - coverlet.collector, xunit.runner.visualstudio
  - Using Include="Xunit"
  - InternalsVisibleTo for test assemblies
  -->

  <ItemGroup>
    <!-- Only add project references - test packages are automatic -->
    <ProjectReference Include="..\ProjectUnderTest\ProjectUnderTest.csproj" />
  </ItemGroup>
</Project>
```

### NuGet Package Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Company.EventSourcing</PackageId>
    <Description>Event sourcing capabilities for distributed systems</Description>
    <PackageTags>event-sourcing;cqrs;domain-driven-design</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

**Note**: PackageId should typically match the automatic assembly name (`Company.ProjectName`) unless there's a specific business reason to use a different name. This ensures consistency between assembly names and NuGet package names.

## Validation Rules

### Required Checks

1. **No version numbers** in PackageReference elements
2. **No duplicate properties** already defined in Directory.Build.props
3. **Minimal content** - only project-specific configurations
4. **Proper SDK selection** - use appropriate SDK for project type
5. **Consistent formatting** - proper indentation and element ordering

### Build Validation

The build system will fail if:

- Version numbers are found in PackageReference elements
- Properties conflict with Directory.Build.props settings
- Required project-specific properties are missing
- Package references use incorrect format

When drift is detected (e.g., version attributes in `PackageReference`, duplicated props from `Directory.Build.props`), create small, discrete tasks in `.scratchpad/tasks/pending` (one per project or file) so agents can normalize in parallel. See `.github/instructions/agent-scratchpad.instructions.md`.

## Migration Guidelines

### Converting Existing Projects

1. **Remove all version numbers** from PackageReference elements
2. **Remove properties** already defined in Directory.Build.props
3. **Keep only project-specific** OutputType, GeneratePackageOnBuild, etc.
4. **Verify build still works** after cleanup
5. **Run quality scripts** to ensure compliance

### Adding New Projects

1. **Start with minimal template** for the project type
2. **Add PackageReference** elements without versions
3. **Include only necessary** project-specific properties
4. **Follow naming conventions** for project files and IDs
5. **Validate against** Directory.Build.props inheritance

This approach ensures that the solution remains maintainable, prevents version conflicts, and follows modern .NET project management best practices while keeping individual project files clean and focused.

## Related Guidelines

This document should be read in conjunction with:

- **Build Rules** (`.github/instructions/build-rules.instructions.md`) - For build pipeline requirements and quality standards that ensure project file compliance
