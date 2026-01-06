// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// API contracts must be public for OpenAPI/Swagger documentation and client generation
[assembly: SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "API contracts must be public for OpenAPI spec generation",
    Scope = "namespaceanddescendants",
    Target = "~N:Cascade.WebApi.Controllers.Contracts")]
