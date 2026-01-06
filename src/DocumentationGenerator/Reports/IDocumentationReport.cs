using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Represents a documentation report that can be generated.
/// </summary>
public interface IDocumentationReport
{
    /// <summary>
    ///     Gets the unique name of the report.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets a brief description of what the report generates.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Gets the execution order priority. Lower values execute first.
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Generates the report and writes output to the specified directory.
    /// </summary>
    /// <param name="context">The report generation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GenerateAsync(
        ReportContext context,
        CancellationToken cancellationToken = default
    );
}
