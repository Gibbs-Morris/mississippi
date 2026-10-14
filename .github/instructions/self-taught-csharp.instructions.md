---
applyTo: '**/*.cs'
---

# Self-Taught Lessons: C# Source Edits

Governing thought: Record validated source-editing failures without weakening compiler checks.

> Drift check: Read overlapping instructions and existing lessons before adding guidance under `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Agents making bulk XML documentation edits **SHOULD** inspect the source newline format and replacement API overload before applying them. Why: PR #1108 produced `SA1625` duplicate remarks when an LF removal assumed Windows CRLF and static `Regex.Replace(..., 1)` selected `RegexOptions` rather than a replacement count.

- Generator authors **MUST** qualify emitted consumer type references when introducing helper types that can shadow them. Why: A03's private `RegistrationMarker` captured valid projection, event, and reducer names and caused `CS0311` in compiled generated output.

- Generator authors **MUST** qualify emitted framework calls that can bind to consumer extensions. Why: Imported consumer namespaces can make an extension call ambiguous or select a different overload; A03's unqualified `Any` caused both a compiler failure and duplicate reducer registration.

- Generator authors **MUST** account for same-compilation generated types without assuming sibling generator output is present in the input compilation, and validate the combined outputs in one generator invocation. Why: A03's saga-status projection produced `CS1061` for a missing registration helper without authored reducers and silently omitted seven generated reducers when an authored reducer was present.

## Scope and Audience

Agents editing C# source documentation or source generator emission.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [C# guidance](csharp.instructions.md)
- [Metadata and host composition](metadata-and-composition.instructions.md), within its activation scope
