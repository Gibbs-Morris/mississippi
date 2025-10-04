# Contributing to Mississippi Framework

Thank you for your interest in contributing to the Mississippi Framework! This document provides guidelines and workflows for contributors.

## Code of Conduct

Please be respectful and constructive in all interactions. We aim to foster an inclusive and welcoming environment for all contributors.

## Development Setup

### Prerequisites

- .NET 9.0 SDK or later
- PowerShell 7.0 or later
- Git
- JetBrains Rider or Visual Studio 2022+ (recommended)

### Getting Started

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/mississippi.git
   cd mississippi
   ```
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/Gibbs-Morris/mississippi.git
   ```
4. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Building and Testing

### Build Scripts

The repository includes PowerShell scripts for common tasks:

- **Build Mississippi solution**: `pwsh ./scripts/build-mississippi-solution.ps1`
- **Build sample solution**: `pwsh ./scripts/build-sample-solution.ps1`
- **Run tests**: `pwsh ./scripts/unit-test-mississippi-solution.ps1`
- **Run mutation tests**: `pwsh ./scripts/mutation-test-mississippi-solution.ps1`
- **Full orchestration**: `pwsh ./go.ps1`

See `scripts/README.md` for detailed documentation on all available scripts.

### Code Quality Standards

This repository enforces strict code quality standards:

- **Zero warnings policy**: All compiler warnings are treated as errors
- **Code analysis**: StyleCop, SonarAnalyzer, and other analyzers are enabled
- **Code coverage**: Aim for high test coverage on new code
- **Mutation testing**: Core library changes should maintain or improve mutation scores

## Code Formatting

### Running Code Cleanup

Before committing your changes, **you must run the code cleanup script**:

```powershell
pwsh ./eng/cleanup.ps1
```

This script:
- Runs ReSharper CleanupCode on both solutions (mississippi and samples)
- Applies the "Built-in: Full Cleanup" profile
- Ensures consistent formatting across Windows and Linux platforms

### Verifying Cleanup

To verify that your code is properly formatted, run:

```powershell
pwsh ./eng/cleanup.ps1 -Check
```

This will fail if cleanup would modify any files. The expectation is:
- After cleanup, `git status` should show no modifications
- CI will enforce the same check and fail if cleanup produces changes

### Why This Matters

The cleanup process ensures:
- Consistent code style across all contributors
- No formatting-related merge conflicts
- Clean git history without formatting noise
- CI builds pass without formatting issues

## Workflow

1. **Make your changes** in your feature branch
2. **Write tests** for new functionality
3. **Run cleanup**: `pwsh ./eng/cleanup.ps1`
4. **Build and test**: `pwsh ./go.ps1`
5. **Verify formatting**: `pwsh ./eng/cleanup.ps1 -Check`
6. **Commit your changes** with descriptive messages
7. **Push to your fork**
8. **Create a pull request** to the main repository

## Pull Request Guidelines

### Before Submitting

- [ ] Code cleanup has been run (`./eng/cleanup.ps1`)
- [ ] All tests pass (`./scripts/unit-test-mississippi-solution.ps1`)
- [ ] No compiler warnings (`./scripts/final-build-solutions.ps1`)
- [ ] Code coverage is maintained or improved
- [ ] Commit messages are clear and descriptive

### PR Description

Include:
- **What**: Brief description of changes
- **Why**: Motivation and context
- **How**: Technical approach (if complex)
- **Testing**: How you tested the changes
- **Related issues**: Link to any related issues

### Review Process

1. Automated checks must pass (builds, tests, cleanup, SonarCloud)
2. At least one maintainer review is required
3. Address review feedback
4. Maintainer will merge when approved

## Common Issues

### Cleanup Fails in CI

If CI reports that cleanup modified files:

1. Pull the latest changes from main
2. Run `pwsh ./eng/cleanup.ps1` locally
3. Review and commit the changes
4. Push and verify CI passes

### Line Ending Issues

The repository uses:
- **LF** for source files (.cs, .ps1, .json, etc.)
- **CRLF** for solution files (.sln)

This is enforced via `.gitattributes` and `.editorconfig`. If you encounter line ending issues:

1. Ensure your git is configured correctly:
   ```bash
   git config core.autocrlf false
   git config core.eol lf
   ```
2. Re-clone the repository if needed
3. Run cleanup script

## Questions?

- Check existing issues for similar questions
- Review the `scripts/README.md` for build script documentation
- Create a new issue with the question label

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).
