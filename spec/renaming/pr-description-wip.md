# PR Description WIP Notes (Renaming)

Use this file as the running source of truth for the final PR description.

## How to use

- After each task is completed, append a new section using the template below.
- Keep entries factual and scoped to that single task/commit.
- Capture enough detail to assemble PR sections later (business value, implementation, files changed, verification, risks).

## Task Entry Template

```markdown
## Task <N>: <OldProject> -> <NewProject>

- Task type: `ProjectRename` | `ProjectMerge` | `ProjectExtract` | `TestRename` | `TestMerge` | `TestExtract`
- Linked pair: `<P## or T##>`

### Summary
- What was renamed and why.

### Changes made
- Folder rename:
- `.csproj` rename:
- `.slnx` updates:
- `.csproj` `ProjectReference` updates:

### Files touched
- `path/to/file-or-folder`

### Verification run
- Build Mississippi: pass/fail
- Build Samples: pass/fail
- Cleanup Mississippi: pass/fail
- Cleanup Samples: pass/fail
- Tests Mississippi: pass/fail
- Tests Samples: pass/fail

### Commit
- SHA:
- Message:

### PR-ready notes
- Business value:
- How it works (high level):
- Risks / breaking surface:
- Follow-up items (if any):
```

## Entries

Add one section per completed task.
