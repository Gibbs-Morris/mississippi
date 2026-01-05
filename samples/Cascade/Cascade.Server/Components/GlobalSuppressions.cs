using System.Diagnostics.CodeAnalysis;


// Blazor component classes must be public because the Razor compiler generates public partial classes.
// CA1515 is a false positive for Blazor components - suppress for all components.
[assembly:
    SuppressMessage(
        "Performance",
        "CA1515:Consider making public types internal",
        Justification = "Blazor components must be public - Razor codegen produces public partial classes.",
        Scope = "namespaceanddescendants",
        Target = "~N:Cascade.Server.Components")]

// The Shared folder follows Blazor default template conventions.
// Renaming would break discoverability for developers familiar with Blazor patterns.
[assembly:
    SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Shared is a Blazor convention for shared components.",
        Scope = "namespace",
        Target = "~N:Cascade.Server.Components.Shared")]

// Error.razor is the standard Blazor error page. Renaming would break conventions.
[assembly:
    SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Error is the standard Blazor error page name.",
        Scope = "type",
        Target = "~T:Cascade.Server.Components.Pages.Error")]

// Channels is a domain-specific page. The conflict with System.Runtime.Remoting.Channels
// is irrelevant for a Blazor Server app that does not use .NET Remoting.
[assembly:
    SuppressMessage(
        "Naming",
        "CA1724:Type names should not match namespaces",
        Justification = ".NET Remoting is obsolete and not used in Blazor Server apps.",
        Scope = "type",
        Target = "~T:Cascade.Server.Components.Pages.Channels")]