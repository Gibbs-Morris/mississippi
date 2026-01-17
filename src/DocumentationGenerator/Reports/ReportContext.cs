using System.Collections.Generic;

using Mississippi.DocumentationGenerator.Infrastructure;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Context provided to reports during execution.
/// </summary>
public sealed class ReportContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportContext" /> class.
    /// </summary>
    /// <param name="repoRoot">The repository root path.</param>
    /// <param name="outputDir">The output directory path.</param>
    /// <param name="solutions">The list of solution file paths.</param>
    /// <param name="writer">The deterministic writer.</param>
    public ReportContext(
        string repoRoot,
        string outputDir,
        IReadOnlyList<string> solutions,
        DeterministicWriter writer
    )
    {
        RepoRoot = repoRoot;
        OutputDir = outputDir;
        Solutions = solutions;
        Writer = writer;
    }

    /// <summary>
    ///     Gets the repository root path.
    /// </summary>
    public string RepoRoot { get; }

    /// <summary>
    ///     Gets the output directory path.
    /// </summary>
    public string OutputDir { get; }

    /// <summary>
    ///     Gets the list of solution file paths to process.
    /// </summary>
    public IReadOnlyList<string> Solutions { get; }

    /// <summary>
    ///     Gets the deterministic writer.
    /// </summary>
    public DeterministicWriter Writer { get; }
}
