using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Registry and executor for documentation reports.
/// </summary>
public sealed class ReportRegistry
{
    private readonly List<IDocumentationReport> reports = new();

    private ILogger<ReportRegistry> Logger { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportRegistry" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ReportRegistry(
        ILogger<ReportRegistry> logger
    )
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets all registered reports sorted by execution order.
    /// </summary>
    public IReadOnlyList<IDocumentationReport> Reports => reports.OrderBy(r => r.Order).ThenBy(r => r.Name, StringComparer.Ordinal).ToList();

    /// <summary>
    ///     Registers a report with the registry.
    /// </summary>
    /// <param name="report">The report to register.</param>
    public void Register(
        IDocumentationReport report
    )
    {
        ArgumentNullException.ThrowIfNull(report);
        reports.Add(report);
        Logger.ReportRegistered(report.Name, report.Order);
    }

    /// <summary>
    ///     Executes all reports or a subset based on the filter.
    /// </summary>
    /// <param name="context">The report generation context.</param>
    /// <param name="reportNames">Optional filter for specific report names. If empty, all reports run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        ReportContext context,
        IReadOnlyList<string>? reportNames = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<IDocumentationReport> reportsToRun = Reports;

        if (reportNames is { Count: > 0 })
        {
            HashSet<string> filterSet = new(reportNames, StringComparer.OrdinalIgnoreCase);
            reportsToRun = reportsToRun.Where(r => filterSet.Contains(r.Name)).ToList();

            if (reportsToRun.Count == 0)
            {
                Logger.NoMatchingReports(string.Join(", ", reportNames));
                throw new InvalidOperationException(
                    $"No reports matched the specified filter: {string.Join(", ", reportNames)}. " +
                    $"Available reports: {string.Join(", ", Reports.Select(r => r.Name))}");
            }
        }

        Logger.ExecutingReports(reportsToRun.Count);

        foreach (IDocumentationReport report in reportsToRun)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.ExecutingReport(report.Name, report.Description);

            try
            {
                await report.GenerateAsync(context, cancellationToken);
                Logger.ReportCompleted(report.Name);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.ReportFailed(report.Name, ex);
                throw;
            }
        }

        Logger.AllReportsCompleted(reportsToRun.Count);
    }
}
