using System;
using System.Collections.Generic;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Context provided to reports during generation.
/// </summary>
public sealed class ReportContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportContext" /> class.
    /// </summary>
    /// <param name="repoRoot">The repository root path.</param>
    /// <param name="docusaurusRoot">The Docusaurus root path.</param>
    /// <param name="outputDirectory">The output directory path.</param>
    /// <param name="solutionFiles">The list of solution files to analyze.</param>
    public ReportContext(
        string repoRoot,
        string docusaurusRoot,
        string outputDirectory,
        IReadOnlyList<string> solutionFiles
    )
    {
        RepoRoot = repoRoot ?? throw new ArgumentNullException(nameof(repoRoot));
        DocusaurusRoot = docusaurusRoot ?? throw new ArgumentNullException(nameof(docusaurusRoot));
        OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        SolutionFiles = solutionFiles ?? throw new ArgumentNullException(nameof(solutionFiles));
    }

    /// <summary>
    ///     Gets the repository root path.
    /// </summary>
    public string RepoRoot { get; }

    /// <summary>
    ///     Gets the Docusaurus root path.
    /// </summary>
    public string DocusaurusRoot { get; }

    /// <summary>
    ///     Gets the output directory path.
    /// </summary>
    public string OutputDirectory { get; }

    /// <summary>
    ///     Gets the list of solution files to analyze.
    /// </summary>
    public IReadOnlyList<string> SolutionFiles { get; }
}
