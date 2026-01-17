using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Registry of all available reports.
/// </summary>
public sealed class ReportRegistry
{
    private ILogger<ReportRegistry> Logger { get; }

    private IReadOnlyList<IReport> AllReports { get; }

    private DocGenOptions Options { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportRegistry" /> class.
    /// </summary>
    /// <param name="reports">All registered reports.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public ReportRegistry(
        IEnumerable<IReport> reports,
        IOptions<DocGenOptions> options,
        ILogger<ReportRegistry> logger
    )
    {
        // Sort reports by name for deterministic execution order
        AllReports = reports.OrderBy(r => r.Name, StringComparer.Ordinal).ToList();
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the reports to execute based on configuration and CLI options.
    /// </summary>
    /// <returns>The ordered list of reports to execute.</returns>
    public IReadOnlyList<IReport> GetReportsToExecute()
    {
        // CLI --reports takes precedence
        List<string> reportFilter = Options.Reports.Count > 0
            ? Options.Reports
            : Options.EnabledReports;

        if (reportFilter.Count == 0)
        {
            Logger.LogInformation("No report filter specified, running all {Count} reports", AllReports.Count);
            return AllReports;
        }

        HashSet<string> filterSet = new(reportFilter, StringComparer.OrdinalIgnoreCase);
        List<IReport> filtered = AllReports.Where(r => filterSet.Contains(r.Name)).ToList();

        Logger.LogInformation(
            "Running {FilteredCount} of {TotalCount} reports based on filter: {Filter}",
            filtered.Count,
            AllReports.Count,
            string.Join(", ", reportFilter)
        );

        return filtered;
    }

    /// <summary>
    ///     Gets all registered report names.
    /// </summary>
    /// <returns>List of report names.</returns>
    public IReadOnlyList<string> GetAllReportNames()
    {
        return AllReports.Select(r => r.Name).ToList();
    }
}
