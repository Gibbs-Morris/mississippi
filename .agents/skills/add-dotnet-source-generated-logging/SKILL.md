---
name: add-dotnet-source-generated-logging
description: Add or convert C# Microsoft.Extensions.Logging calls to LoggerMessage source-generated methods. Use when implementing structured logging or replacing direct ILogger.Log calls in a specified component. Not for reading incident logs, configuring logging providers, or general code review.
---

# Add .NET source-generated logging

Implement the requested logging change using the target repository's policies and existing conventions. Keep the change scoped to the selected component; this skill does not authorize a repository-wide conversion, package upgrade, provider change, or publication.

## Establish the logging contract

1. Identify the component and requested outcome. If no target can be inferred from the request or supplied code, ask for the missing scope before editing.
2. Read applicable repository instructions, the project configuration, nearby logging helpers, their callers, and relevant tests. Determine the supported generator, helper naming/accessibility, dependency-injection convention, required logging points, redaction rules, and verification commands from that repository. Do not assume a particular folder layout, shell, or test framework.
3. For a conversion, record the existing logger/category, level, event ID **and name**, message template, structured property names/types/values/order, exception argument, scopes, and surrounding control flow. For new logs, choose these deliberately using local conventions; reuse an existing helper when its contract matches.

## Implement the selected calls

- Follow the repository's valid partial-method pattern and accessibility. Static helpers use `static partial void` with `[LoggerMessage]` and an `ILogger` parameter (`this ILogger logger` for extension methods). Where instance logging methods are established, keep the supported logger field or primary-constructor pattern instead. Keep the caller's logger category; check generator-version support before choosing a signature.
- Use constant message templates and typed parameters. Templates bind parameter names case-insensitively, not by declaration order; explicitly check each mapping. Avoid interpolation, preformatted strings, and accidental changes to structured property names.
- Named-field binding does not guarantee the same enumeration order: generated state can follow parameter declaration order. Preserve the original field order in the generated method. If a required signature conflicts, identify the mismatch and offer a scoped alternative rather than claiming equivalence; a forwarding method is justified only when that signature needs preserving.
- Preserve existing event identity explicitly when converting. Generated default IDs/names can differ from direct calls; do not assume omitting `EventId` or `EventName` preserves the old event. If a contract cannot be represented, report the mismatch and a scoped alternative before replacing that call.
- Pass the logging exception as an `Exception` parameter rather than inserting its text into the message. The first exception parameter is special and should not appear as a template placeholder. Check additional exception parameters separately.
- Preserve a dynamic level with a `LogLevel` parameter and no fixed attribute level. Respect existing scopes and keep calls at the same success/failure boundaries. Do not change exception handling, retries, return values, or business-side effects to introduce logging.
- Keep the generator's enabled check by default. It cannot prevent argument expressions being evaluated at the call site: guard expensive **logging-only** preparation with the matching `ILogger.IsEnabled(level)` check. Do not move required business work inside that guard. Use `SkipEnabledCheck` only when every caller demonstrably supplies the matching check.
- Follow local logging coverage and redaction policy. Do not copy sensitive payloads or credentials into a new log to preserve a flawed old message; identify the necessary contract change. Follow the repository's remediation tracking rules for direct calls left outside scope.

## Verify and report

1. Build the affected project using its supported SDK/generator and repository commands. Resolve generator/analyzer diagnostics; do not suppress them or edit generated output. Check version-dependent features against the actual compiler rather than assuming all SDK versions behave alike.
2. Use a capturing logger or the existing logging test infrastructure to check the enabled event: category, level, event ID/name, original template, structured fields, and exception. Compare before/after for conversions, including formatted output and field order. Report differences and establish consumer impact before calling a conversion equivalent. A `NullLogger` invocation alone does not establish equivalence.
3. Check disabled levels, dynamic levels when present, and expensive logging-only argument evaluation. Verify success and failure control flow remains unchanged. Run the remaining applicable repository quality gates.
4. Report the changed calls/helpers, preserved contract or explicit intentional differences, commands and actual results, and unresolved cases. If tools are missing or checks fail, state what remains unverified; do not claim a successful build or completed conversion.

## Technical references

Consult these when a signature, template mapping, or generator behavior needs verification; repository policy still determines local conventions.

- [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation)
- [Logging guidance for library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/library-guidance)
