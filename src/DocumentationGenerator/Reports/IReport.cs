using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Interface for documentation reports.
/// </summary>
public interface IReport
{
    /// <summary>
    ///     Gets the unique name of this report.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the description of this report.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Executes the report and generates documentation files.
    /// </summary>
    /// <param name="context">The report execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(
        ReportContext context,
        CancellationToken cancellationToken
    );
}
