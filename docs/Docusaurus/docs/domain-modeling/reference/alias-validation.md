---
id: alias-validation
title: Alias Validation Helper
description: Reference Orleans type-alias scanning, exception matching, and architecture-test diagnostics.
sidebar_position: 15
sidebar_label: Alias Validation Helper
---

# Alias Validation Helper

## Overview

`AliasValidation.AnalyzeAssemblies` compares declared type-level Orleans aliases with current CLR type names. It returns diagnostics for a consumer architecture test to evaluate.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Architecture.AliasValidation`
- `AliasValidationSummary` and `AliasValidationExceptionRule`
- Consumer tests supplying their own assemblies and exception rules

## Scan Boundary

The [helper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Architecture/AliasValidation.cs) rejects null assembly and exception-rule collections. It filters null assembly items, deduplicates assemblies by ordinal `FullName`, and orders candidates by simple assembly name and type name. Distinct assemblies sharing the same simple name and candidate type name tie on those keys, so their relative order can depend on the supplied assembly order.

Candidate types must declare a type-level `AliasAttribute`; inherited aliases are not selected. Missing type aliases and method-level aliases are outside this scan. When `Assembly.GetTypes` throws `ReflectionTypeLoadException`, the helper uses its non-null loadable types rather than failing the entire scan.

Generated types are excluded by `CompilerGeneratedAttribute` and known name/namespace patterns, including `OrleansCodeGen`, `Codec_`, `Copier_`, `Activator_`, `Proxy_`, `Invokable_`, and names containing `AnonymousType`. These checks are conventions, not a complete generated-code detector.

## Expected Alias And Categories

`GetExpectedAlias(type)` uses `Type.FullName`, falling back to `Name`. CLR generic arity and nested-type separators are retained. Alias comparison is ordinal and case-sensitive.

Mismatch records contain assembly name, type name, actual and expected aliases, and category metadata. Categories are inferred from names/namespaces such as commands, events, projections, aggregates, and grain types; they do not establish the type's domain role.

## Exception Rules And Results

Rule identifiers are trimmed; whitespace-only identifiers become null. A rule matches when either its `TypeFullName` or `ExpectedAlias` equals the scanned type's corresponding value by ordinal comparison. Supplying both does not require both to match.

Configuration diagnostics cover missing identifiers, blank reasons, `*` in identifiers, duplicate combinations of normalized `TypeFullName`, normalized `ExpectedAlias`, and classification, and stale rules that match no scanned candidate. Classification and owner metadata do not add conditions to the identifier match.

The [summary](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Architecture/AliasValidationSummary.cs) separates:

- `Mismatches`: alias differences not suppressed by matching rules.
- `ConfigurationErrors`: invalid or stale exception-rule diagnostics.
- `ActiveExceptions`: rules matching at least one scanned candidate.

Configuration errors do not themselves prevent a matching rule from suppressing a mismatch. A consumer deciding whether a test passes should inspect both mismatches and configuration errors. `FormatReport` formats the ordered results, subject to the scan's ordering ties above. `ActiveExceptions` sort only by normalized `TypeFullName` and `ExpectedAlias`. Rules with equal identifiers but different classifications tie on both keys and retain their preceding sequence, so their report order can also depend on exception-rule input order.

## Optional Report Output

When `MISSISSIPPI_ALIAS_VALIDATION_REPORT_PATH` is non-whitespace and the scan contains mismatches or configuration errors, analysis creates the parent directory as needed and writes `FormatReport()` to that path.

A clean scan does not write or delete that file, even when active exceptions exist. Path and filesystem errors propagate; the method named `TryWriteReport` does not catch them.

The [helper tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Architecture.L0Tests/AliasValidationHelperTests.cs) cover trimmed rules, method-alias exclusion, stale/wildcard diagnostics, deterministic ordering, generic/nested names, and generated-name exclusions.

## Summary

This helper checks declared type aliases within supplied assemblies and reports exceptions separately from mismatches. It does not check every missing alias, method contract, or unloadable type.

## Next Steps

- Read [Domain Modeling Reference](./reference.md) for the package's other consumer contracts.
- Read [Generated Application Contracts](../../inlet/reference/generated-contracts.md) for Inlet's generated surface.
