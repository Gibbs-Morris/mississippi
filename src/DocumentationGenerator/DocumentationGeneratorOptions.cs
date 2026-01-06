using System;
using System.Collections.Generic;


namespace Mississippi.DocumentationGenerator;

/// <summary>
///     Configuration options for the documentation generator.
/// </summary>
public sealed class DocumentationGeneratorOptions
{
    /// <summary>
    ///     Gets or sets the repository root path.
    ///     When null or empty, autodiscovery is used.
    /// </summary>
    public string? RepoRoot { get; set; }

    /// <summary>
    ///     Gets or sets the Docusaurus root path.
    ///     When null or empty, autodiscovery is used.
    /// </summary>
    public string? DocusaurusRoot { get; set; }

    /// <summary>
    ///     Gets or sets the output directory path.
    ///     When null or empty, defaults to {DocusaurusDocsRoot}/generated.
    /// </summary>
    public string? OutputDir { get; set; }

    /// <summary>
    ///     Gets or sets the list of solution files to analyze.
    ///     When empty, defaults to mississippi.slnx and samples.slnx.
    /// </summary>
    public List<string> SolutionFiles { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of reports to run.
    ///     When empty, all reports are run.
    /// </summary>
    public List<string> Reports { get; set; } = new();

    /// <summary>
    ///     Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public void Validate()
    {
        // Validation will be performed after discovery has completed.
        // No required options at this stage.
    }
}
