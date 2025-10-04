# Windows vs Linux ReSharper Cleanup Drift - Root Cause and Solution

## Problem Statement

Developers running cleanup locally on Windows would commit their changes, but CI running on Linux (ubuntu-latest) would still detect modifications and fail the build. This created a frustrating loop where local cleanup appeared to work but CI consistently failed.

## Root Cause Analysis

After thorough investigation, we identified five contributing factors:

### 1. Scope Mismatch
- **Issue**: Repository had two separate cleanup scripts (`clean-up-mississippi-solution.ps1` and `clean-up-sample-solution.ps1`)
- **Impact**: Developers typically ran only one script locally, while CI ran both via matrix strategy
- **Result**: Files in one solution would be properly formatted while files in the other remained untouched

### 2. Line Ending Inconsistencies
- **Issue**: No `.gitattributes` file to enforce consistent line endings
- **Impact**: Windows defaults to CRLF, Linux defaults to LF, causing silent differences
- **Result**: ReSharper cleanup would "fix" line endings differently on each platform
- **Specific manifestation**: SA1518 violations (missing final newline) handled differently

### 3. Incomplete Editor Configuration
- **Issue**: Minimal `.editorconfig` with only analyzer severity settings
- **Impact**: No explicit EOL or formatting rules specified
- **Result**: Different editors and platforms would apply different formatting

### 4. CI Workflow Inconsistencies
- **Issue**: CI had fallback to install global tools and didn't configure git EOL settings
- **Impact**: Tool versions could drift between local and CI environments
- **Result**: Different ReSharper versions or settings could produce different outputs

### 5. No Idempotency Validation
- **Issue**: No verification that cleanup was deterministic
- **Impact**: Changes could accumulate across runs
- **Result**: Running cleanup multiple times could produce different results

## Solution Implementation

### 1. Unified Cleanup Script (`eng/cleanup.ps1`)

**What**: Created a single, parameterized PowerShell script that handles both solutions

**Key Features**:
- Defaults to cleaning both `mississippi.slnx` and `samples.slnx`
- Restores tools from `.config/dotnet-tools.json` manifest only (pinned versions)
- Supports `-Check` flag for CI validation
- Provides clear error messages with remediation instructions
- Uses `git rev-parse --show-toplevel` to ensure consistent repo root

**Why This Works**:
- Eliminates scope mismatch by always processing both solutions
- Ensures developers and CI use identical commands
- Pinned tool versions prevent drift
- Check flag makes failures obvious with actionable guidance

### 2. Comprehensive `.gitattributes`

**What**: Added complete line-ending enforcement

**Key Rules**:
```
*           text=auto          # Default: auto-detect and normalize
*.cs        text eol=lf        # Source files: LF in repo
*.sln       text eol=crlf      # Solutions: CRLF (VS compatibility)
*.dll       binary             # Binaries: no conversion
```

**Why This Works**:
- LF for source files matches Linux CI environment
- CRLF for .sln matches Visual Studio expectations
- `text=auto` handles edge cases gracefully
- Binary files explicitly marked to prevent corruption

### 3. Enhanced `.editorconfig`

**What**: Added comprehensive formatting and EOL rules

**Key Settings**:
```
[*]
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.sln]
end_of_line = crlf    # Exception for solution files
```

**Why This Works**:
- Explicit EOL settings match `.gitattributes`
- Final newline rule addresses SA1518 violations
- Per-file-type settings handle exceptions
- C# formatting preferences align with ReSharper

### 4. Updated CI Workflow

**What**: Streamlined and hardened the cleanup check workflow

**Changes**:
- Removed matrix strategy (unified script handles both solutions)
- Removed global tool install fallback (manifest-only)
- Added `git config core.autocrlf false` and `core.eol lf`
- Replaced individual steps with single script invocation
- Added idempotency assertion (run cleanup twice)

**Why This Works**:
- Git EOL configuration prevents surprises
- Single script call ensures exact parity with local workflow
- Idempotency check catches non-deterministic behavior
- No global installs means no version drift

### 5. Line Ending Normalization

**What**: Applied cleanup to fix 165 files (134 + 31 in second pass)

**Changes**:
- Added missing final newlines (SA1518 fixes)
- Reordered members per ReSharper rules
- Added explicit using statements where implicit imports failed
- Normalized line endings across all files

**Why This Was Necessary**:
- Existing codebase had accumulated inconsistencies
- One-time normalization establishes clean baseline
- Future changes will be caught by CI before merge

## Verification Steps

### Local Verification (Pre-Commit)

```powershell
# 1. Run cleanup
pwsh ./eng/cleanup.ps1

# 2. Verify no changes
pwsh ./eng/cleanup.ps1 -Check

# 3. Expected output
✓ No modifications detected - code is properly formatted
```

### CI Verification

The CI workflow now:
1. Configures git EOL settings explicitly
2. Restores tools from manifest (pinned versions)
3. Runs cleanup on both solutions
4. Fails if any files are modified
5. Runs cleanup again to assert idempotency

## Acceptance Criteria - All Met ✓

1. ✅ **Windows local cleanup passes**: `pwsh ./eng/cleanup.ps1 -Check` yields "No modifications"
2. ✅ **CI passes after local cleanup**: Pushing a clean commit causes CI to pass
3. ✅ **Idempotent cleanup**: Running twice in CI yields no changes
4. ✅ **No global tools**: All tools from `.config/dotnet-tools.json`
5. ✅ **EOL churn eliminated**: `.gitattributes` and `.editorconfig` enforce consistency

## Impact and Benefits

### For Developers
- Single cleanup command works identically everywhere
- Clear error messages when formatting is needed
- No more "cleanup ping-pong" with CI

### For CI/CD
- Deterministic, reproducible formatting checks
- No version drift between environments
- Faster feedback (single script call)
- Idempotency validation catches regressions

### For the Codebase
- Consistent formatting across all files
- No line-ending churn in diffs
- Easier code review (no formatting noise)
- Standards enforced automatically

## Lessons Learned

1. **Line endings matter**: Platform differences are real and must be explicitly managed
2. **Unified tooling is critical**: Multiple scripts for the same task invite divergence
3. **Explicit is better than implicit**: Don't rely on defaults; configure everything
4. **Idempotency is testable**: Always verify that operations are deterministic
5. **Documentation prevents drift**: Clear guidelines help maintain consistency

## Related Files

- `eng/cleanup.ps1` - Unified cleanup script
- `.gitattributes` - Line ending enforcement
- `.editorconfig` - Editor formatting rules
- `.github/workflows/cleanup.yml` - CI cleanup check
- `README.md` - Quick reference

## Future Improvements

Consider these enhancements:

1. **Pre-commit hook**: Automatically run cleanup before git commit
2. **IDE integration**: Add ReSharper settings to .sln.DotSettings
3. **Monitoring**: Track cleanup drift patterns in telemetry
4. **Automation**: Bot to auto-fix formatting in PRs

## References

- [Microsoft Orleans Serialization](https://learn.microsoft.com/dotnet/orleans/serialization)
- [Git Attributes Documentation](https://git-scm.com/docs/gitattributes)
- [EditorConfig Specification](https://editorconfig.org)
- [ReSharper Command Line Tools](https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html)
