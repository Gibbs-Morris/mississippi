using System.Collections.Generic;


namespace Mississippi.DocumentationGenerator.Configuration;

/// <summary>
///     Main configuration options for the documentation generator.
/// </summary>
public sealed class DocGenOptions
{
    /// <summary>
    ///     Gets or sets the repository root path. If not set, autodiscovery is used.
    /// </summary>
    public string? RepoRoot { get; set; }

    /// <summary>
    ///     Gets or sets the Docusaurus root path. If not set, autodiscovery is used.
    /// </summary>
    public string? DocusaurusRoot { get; set; }

    /// <summary>
    ///     Gets or sets the output directory. Defaults to {DocusaurusDocsRoot}/generated.
    /// </summary>
    public string? OutputDir { get; set; }

    /// <summary>
    ///     Gets or sets the list of solution files to process.
    ///     Defaults to mississippi.slnx and samples.slnx at repo root.
    /// </summary>
    public List<string> Solutions { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of reports to run. Empty means run all.
    /// </summary>
    public List<string> Reports { get; set; } = new();

    /// <summary>
    ///     Gets or sets the configuration file path.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    ///     Gets or sets the list of enabled reports.
    /// </summary>
    public List<string> EnabledReports { get; set; } = new();
}

/// <summary>
///     Configuration for class diagram generation.
/// </summary>
public sealed class ClassDiagramOptions
{
    /// <summary>
    ///     Gets or sets project patterns to include.
    /// </summary>
    public List<string> IncludeProjects { get; set; } = new();

    /// <summary>
    ///     Gets or sets project patterns to exclude.
    /// </summary>
    public List<string> ExcludeProjects { get; set; } = new();

    /// <summary>
    ///     Gets or sets namespace patterns to include.
    /// </summary>
    public List<string> IncludeNamespaces { get; set; } = new();

    /// <summary>
    ///     Gets or sets namespace patterns to exclude.
    /// </summary>
    public List<string> ExcludeNamespaces { get; set; } = new();

    /// <summary>
    ///     Gets or sets the maximum types per diagram.
    /// </summary>
    public int MaxTypesPerDiagram { get; set; } = 30;

    /// <summary>
    ///     Gets or sets a value indicating whether to include only public types.
    /// </summary>
    public bool PublicOnly { get; set; } = false;
}

/// <summary>
///     Configuration for project dependency diagrams.
/// </summary>
public sealed class ProjectDependencyOptions
{
    /// <summary>
    ///     Gets or sets the solutions to include.
    /// </summary>
    public List<string> IncludeSolutions { get; set; } = new();

    /// <summary>
    ///     Gets or sets project patterns to include.
    /// </summary>
    public List<string> IncludeProjects { get; set; } = new();

    /// <summary>
    ///     Gets or sets project patterns to exclude.
    /// </summary>
    public List<string> ExcludeProjects { get; set; } = new();
}

/// <summary>
///     Configuration for Orleans grain call mapping.
/// </summary>
public sealed class OrleansOptions
{
    /// <summary>
    ///     Gets or sets project patterns to include.
    /// </summary>
    public List<string> IncludeProjects { get; set; } = new();

    /// <summary>
    ///     Gets or sets project patterns to exclude.
    /// </summary>
    public List<string> ExcludeProjects { get; set; } = new();

    /// <summary>
    ///     Gets or sets namespace patterns to include.
    /// </summary>
    public List<string> IncludeNamespaces { get; set; } = new();

    /// <summary>
    ///     Gets or sets namespace patterns to exclude.
    /// </summary>
    public List<string> ExcludeNamespaces { get; set; } = new();

    /// <summary>
    ///     Gets or sets the maximum edges to display.
    /// </summary>
    public int MaxEdges { get; set; } = 100;

    /// <summary>
    ///     Gets or sets a value indicating whether to include only public types.
    /// </summary>
    public bool PublicOnly { get; set; } = false;
}
